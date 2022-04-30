using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarPhysicsEstimator : MonoBehaviour
{
    static string TOPIC_SUFFIX_TARGET_LINEAR_VELOCITIES = "/avatar/target_linear_velocities";
    static string TOPIC_SUFFIX_TARGET_ANGULAR_VELOCITIES = "/avatar/target_angular_velocities";

    public GameObject avatarPoseEstimation = null;
    public UbiiComponentAvatarCurrentPose ubiiComponentAvatarCurrentPose = null;
    public UbiiComponentAvatarForceControl ubiiComponentAvatarForceControl = null;
    public int publishFrequency = 15;

    public bool publishLinearVelocity = true;
    public bool publishAngularVelocity = true;

    [Tooltip("For target velocities of AvatarForceControl, don't use topics but directly set them")]
    public bool setVelocitiesDirectly = true;

    [Tooltip("Scaling factors for linear (X) and angular (Y) output velocities")]
    public Vector2 scalingFactorsVelocities = new Vector2(1, 1);
    public Vector3 manualPositionOffset = new Vector3();

    private UbiiNode ubiiNode = null;
    private bool running = false;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;

    private Dictionary<HumanBodyBones, Transform> mapBone2TargetTransform = new Dictionary<HumanBodyBones, Transform>();
    private Dictionary<HumanBodyBones, UbiiPose3D> mapBone2CurrentPose = new Dictionary<HumanBodyBones, UbiiPose3D>();

    private SubscriptionToken tokenCurrentPoseList;

    void OnEnable()
    {
        ubiiNode = FindObjectOfType<UbiiNode>();
        UbiiNode.OnInitialized += OnUbiiNodeInitialized;

        InitBoneTargetTransforms();
    }

    void OnDisable()
    {
        UbiiNode.OnInitialized -= OnUbiiNodeInitialized;
        running = false;
    }

    void Update()
    {
        if (running)
        {
            if (setVelocitiesDirectly)
            {
                SetTargetVelocitiesDirectly();
            }
            else
            {
                float tNow = Time.time;
                if (tNow >= tLastPublish + secondsBetweenPublish)
                {
                    PublishIdealVelocities();
                    tLastPublish = tNow;
                }
            }
        }
    }

    public void StartProcessing(/*Func<Ubii.TopicData.TopicDataRecord> GetCurrentPoses, Action<Ubii.TopicData.TopicDataRecord> */)
    {
        this.running = true;
    }

    public void StopProcessing()
    {
        this.running = false;
    }

    void OnUbiiNodeInitialized()
    {
        secondsBetweenPublish = 1f / (float)publishFrequency;
        tLastPublish = Time.time;

        InitCurrentPoseListTopic();
    }

    void InitBoneTargetTransforms()
    {
        Animator animatorPoseEstimation = avatarPoseEstimation.GetComponent<Animator>();

        foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
        {
            //LastBone is not mapped to a bodypart, we need to skip it.
            if (bone != HumanBodyBones.LastBone)
            {
                Transform armatureJointTransform = animatorPoseEstimation.GetBoneTransform(bone);
                if (armatureJointTransform != null)
                {
                    GameObject boneTarget = new GameObject("PhysicsEstimator BoneTarget " + bone.ToString());

                    Vector3 armaturePosition = armatureJointTransform.position;
                    Vector3 targetPosition = new Vector3();
                    // since we receive the position of the armature joint and not the position of the bone in between those joints
                    // which is the geometry carrying a rigidbody, colliders and mass
                    // we need to interpolate the true target position between it and all its child joint positions
                    // to arrive at an estimate center position where the bone would be
                    targetPosition.Set(armaturePosition.x, armaturePosition.y, armaturePosition.z);
                    if (bone != HumanBodyBones.UpperChest)
                    {
                        int armatureChildCount = 0;
                        foreach (Transform child in armatureJointTransform)
                        {
                            if (child.name.Contains(MIXAMO_RIG.PREFIX_ARMATURE))
                            {
                                targetPosition += child.position;
                                armatureChildCount++;
                            }
                        }
                        targetPosition /= armatureChildCount + 1;
                    }

                    boneTarget.transform.position = targetPosition;
                    boneTarget.transform.parent = armatureJointTransform;
                    mapBone2TargetTransform.Add(bone, boneTarget.transform);
                }
            }
        }
    }

    async void InitCurrentPoseListTopic()
    {
        tokenCurrentPoseList = await ubiiNode.SubscribeTopic(ubiiComponentAvatarCurrentPose.GetTopicCurrentPoseList(), (Ubii.TopicData.TopicDataRecord record) =>
        {
            Google.Protobuf.Collections.RepeatedField<Ubii.DataStructure.Object3D> objects = record.Object3DList.Elements;
            for (int i = 0; i < record.Object3DList.Elements.Count; i++)
            {
                string boneString = record.Object3DList.Elements[i].Id;
                HumanBodyBones bone;
                if (HumanBodyBones.TryParse(boneString, out bone))
                {
                    Ubii.DataStructure.Pose3D pose = record.Object3DList.Elements[i].Pose;
                    UbiiPose3D newMapPose = new UbiiPose3D
                    {
                        position = new Vector3((float)pose.Position.X, (float)pose.Position.Y, (float)pose.Position.Z),
                        rotation = new Quaternion((float)pose.Quaternion.X, (float)pose.Quaternion.Y, (float)pose.Quaternion.Z, (float)pose.Quaternion.W)
                    };
                    if (mapBone2CurrentPose.ContainsKey(bone))
                    {
                        mapBone2CurrentPose[bone] = newMapPose;
                    }
                    else
                    {
                        mapBone2CurrentPose.Add(bone, newMapPose);
                    }
                }
            }
        });
    }

    private void PublishIdealVelocities()
    {
        /*Ubii.TopicData.TopicData topicData = new Ubii.TopicData.TopicData { TopicDataRecord = new Ubii.TopicData.TopicDataRecord {
            Topic = GetTopicTargetVelocities(),
            Object3DList = new Ubii.DataStructure.Object3DList()
        } };*/

        Ubii.TopicData.TopicDataRecord record = new Ubii.TopicData.TopicDataRecord
        {
            Topic = ubiiComponentAvatarForceControl.GetTopicTargetVelocities(),
            Object3DList = new Ubii.DataStructure.Object3DList()
        };

        foreach (KeyValuePair<HumanBodyBones, Transform> entry in mapBone2TargetTransform)
        {
            HumanBodyBones bone = entry.Key;
            Transform targetTransform = entry.Value;

            UbiiPose3D currentPose;
            if (mapBone2CurrentPose.TryGetValue(bone, out currentPose))
            {
                Vector3 linearVelocity = scalingFactorsVelocities.x * GetIdealLinearVelocity(currentPose.position, targetTransform.position + manualPositionOffset);
                Vector3 angularSpeed = scalingFactorsVelocities.y * GetIdealAngularVelocity(currentPose.rotation, targetTransform.rotation);

                Ubii.DataStructure.Object3D ubiiObject3D = new Ubii.DataStructure.Object3D
                {
                    Id = bone.ToString(),
                    Pose = new Ubii.DataStructure.Pose3D { }
                };
                if (this.publishLinearVelocity)
                {
                    ubiiObject3D.Pose.Position = new Ubii.DataStructure.Vector3
                    {
                        X = linearVelocity.x,
                        Y = linearVelocity.y,
                        Z = linearVelocity.z
                    };
                }
                if (this.publishAngularVelocity)
                {
                    ubiiObject3D.Pose.Euler = new Ubii.DataStructure.Vector3
                    {
                        X = angularSpeed.x,
                        Y = angularSpeed.y,
                        Z = angularSpeed.z
                    };
                }
                record.Object3DList.Elements.Add(ubiiObject3D);
            }
        }

        ubiiNode.Publish(record);
    }

    private void SetTargetVelocitiesDirectly()
    {
        foreach (KeyValuePair<HumanBodyBones, Transform> entry in mapBone2TargetTransform)
        {
            HumanBodyBones bone = entry.Key;
            Transform targetTransform = entry.Value;

            UbiiPose3D currentPose;
            if (mapBone2CurrentPose.TryGetValue(bone, out currentPose))
            {
                Vector3 linearVelocity = scalingFactorsVelocities.x * GetIdealLinearVelocity(currentPose.position, targetTransform.position + manualPositionOffset);
                Vector3 angularSpeed = scalingFactorsVelocities.y * GetIdealAngularVelocity(currentPose.rotation, targetTransform.rotation);

                ubiiComponentAvatarForceControl.SetTargetVelocity(bone, linearVelocity, angularSpeed);
            }
        }
    }

    private static Vector3 GetIdealLinearVelocity(Vector3 currentPos, Vector3 targetPos)
    {
        Vector3 deltaPosition = GetPositionError(currentPos, targetPos);
        Vector3 targetVelocity = deltaPosition / Time.deltaTime;
        return targetVelocity;
    }

    private static Vector3 GetIdealAngularVelocity(Quaternion currentRot, Quaternion targetRot)
    {
        Quaternion rotationDir = targetRot * Quaternion.Inverse(currentRot);
        float angleInDegrees;
        Vector3 rotationAxis;
        rotationDir.ToAngleAxis(out angleInDegrees, out rotationAxis);
        rotationAxis.Normalize();
        Vector3 angularDifference = rotationAxis * angleInDegrees * Mathf.Deg2Rad;
        Vector3 angularSpeed = angularDifference / Time.deltaTime;
        return angularSpeed;
    }

    public static Vector3 GetPositionError(Vector3 current, Vector3 target)
    {
        return target - current;
    }

    public static Vector3 GetAngularErrorEuler(Quaternion current, Quaternion target)
    {
        Vector3 a = current.eulerAngles;
        Vector3 b = target.eulerAngles;
        return new Vector3(Mathf.DeltaAngle(a.x, b.x), Mathf.DeltaAngle(a.y, b.y), Mathf.DeltaAngle(a.z, b.z));
    }

    public string GetTopicTargetLinearVelocities()
    {
        return "/" + ubiiNode.Id + TOPIC_SUFFIX_TARGET_LINEAR_VELOCITIES;
    }

    public string GetTopicTargetAngularVelocities()
    {
        return "/" + ubiiNode.Id + TOPIC_SUFFIX_TARGET_ANGULAR_VELOCITIES;
    }
}
