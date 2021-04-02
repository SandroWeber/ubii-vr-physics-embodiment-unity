using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarPhysicsEstimator : MonoBehaviour
{
    static string TOPIC_PREFIX_TARGET_VELOCITIES = "/avatar/target_velocities";
    static string TOPIC_PREFIX_TARGET_LINEAR_VELOCITIES = "/avatar/target_linear_velocities";
    static string TOPIC_PREFIX_TARGET_ANGULAR_VELOCITIES = "/avatar/target_angular_velocities";

    public GameObject avatarPoseEstimation = null;
    public AvatarPhysicsManager avatarPhysicsManager = null;
    public int publishFrequency = 15;

    private UbiiClient ubiiClient = null;
    private bool ubiiReady = false;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;

    private Dictionary<HumanBodyBones, Transform> mapBone2TargetTransform = new Dictionary<HumanBodyBones, Transform>();
    private Dictionary<HumanBodyBones, UbiiPose3D> mapBone2CurrentPose = new Dictionary<HumanBodyBones, UbiiPose3D>();

    void Start()
    {
        ubiiClient = FindObjectOfType<UbiiClient>();
        InitBoneTargetTransforms();
    }

    void OnEnable()
    {
        UbiiClient.OnInitialized += OnUbiiClientInitialized;
    }

    void OnDisable()
    {
        UbiiClient.OnInitialized -= OnUbiiClientInitialized;
        ubiiReady = false;
    }

    void OnUbiiClientInitialized()
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

                    //Debug.Log(targetPosition);
                    boneTarget.transform.position = targetPosition;
                    boneTarget.transform.parent = armatureJointTransform;
                    mapBone2TargetTransform.Add(bone, boneTarget.transform);
                }
            }
        }
    }

    async void InitCurrentPoseListTopic()
    {
        await ubiiClient.Subscribe(avatarPhysicsManager.GetTopicCurrentPoseList(), (Ubii.TopicData.TopicDataRecord record) => {
            Google.Protobuf.Collections.RepeatedField<Ubii.DataStructure.Object3D> objects = record.Object3DList.Elements;
            for (int i=0; i < record.Object3DList.Elements.Count; i++)
            {
                //Debug.Log(record.Object3DList.Elements[i]);
                string boneString = record.Object3DList.Elements[i].Id;
                HumanBodyBones bone;
                if (HumanBodyBones.TryParse(boneString, out bone)) {
                    //Debug.Log(bone);
                    Ubii.DataStructure.Pose3D pose = record.Object3DList.Elements[i].Pose;
                    UbiiPose3D newMapPose = new UbiiPose3D {
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

        ubiiReady = true;
    }

    void Update()
    {
        if (ubiiReady)
        {
            //TODO: make ubii client networking threaded & thread-safe
            float tNow = Time.time;
            if (tNow >= tLastPublish + secondsBetweenPublish)
            {
                //PublishTopicDataIKTargets();
                PublishTopicDataForces();
                tLastPublish = tNow;
            }
        }
    }

    /*void FixedUpdate()
    {
        if (ubiiReady)
        {
            // direct internal calculation for comparison
            //CalculateForcesInternally();
        }
    }*/

    /*void SetTargetTransformFromArmatureJoint(HumanBodyBones bone, Transform armatureJointTransform)
    {
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

        //Debug.Log(targetPosition);
        if (!this.mapBone2TargetPosition.ContainsKey(bone))
        {
            this.mapBone2TargetPosition.Add(bone, targetPosition);
        }
        else
        {
            this.mapBone2TargetPosition[bone] = targetPosition;
        }

        // target rotation
        Quaternion armatureRotation = armatureJointTransform.rotation;
        Quaternion targetRotation = new Quaternion();
        targetRotation.Set(armatureRotation.x, armatureRotation.y, armatureRotation.z, armatureRotation.w);
        if (!this.mapBone2TargetRotation.ContainsKey(bone))
        {
            this.mapBone2TargetRotation.Add(bone, targetRotation);
        }
        else
        {
            this.mapBone2TargetRotation[bone] = targetRotation;
        }
    }*/

    private void PublishTopicDataForces()
    {
        Ubii.TopicData.TopicData topicData = new Ubii.TopicData.TopicData { TopicDataRecord = new Ubii.TopicData.TopicDataRecord {
            Topic = GetTopicTargetVelocities(),
            Object3DList = new Ubii.DataStructure.Object3DList()
        } };

        foreach(KeyValuePair<HumanBodyBones, Transform> entry in mapBone2TargetTransform)
        {
            HumanBodyBones bone = entry.Key;
            Transform targetTransform = entry.Value;

            Rigidbody rigidbody = avatarPhysicsManager.GetRigidbodyFromBone(bone);
            UbiiPose3D currentPose;
            if (mapBone2CurrentPose.TryGetValue(bone, out currentPose))
            {
                // linear force
                Vector3 deltaPosition = GetPositionError(currentPose.position, targetTransform.position);
                Vector3 targetVelocity = deltaPosition / Time.deltaTime;
                //AvatarForceControl.AddForceFromTargetLinearVelocity(rigidbody, targetVelocity);
        
                // angular force
                Quaternion rotationDir = targetTransform.rotation * Quaternion.Inverse(currentPose.rotation);
        
                float angleInDegrees;
                Vector3 rotationAxis;
                rotationDir.ToAngleAxis(out angleInDegrees, out rotationAxis);
                rotationAxis.Normalize();
        
                Vector3 angularDifference = rotationAxis * angleInDegrees * Mathf.Deg2Rad;
                Vector3 angularSpeed = angularDifference / Time.deltaTime;
    
                //AvatarForceControl.AddTorqueFromTargetAngularVelocity(rigidbody, angularSpeed);
                

                topicData.TopicDataRecord.Object3DList.Elements.Add(new Ubii.DataStructure.Object3D { 
                    Id = bone.ToString(),
                    Pose = new Ubii.DataStructure.Pose3D
                    {
                        Position = new Ubii.DataStructure.Vector3 { 
                            X = targetVelocity.x,
                            Y = targetVelocity.y,
                            Z = targetVelocity.z },
                        Euler = new Ubii.DataStructure.Vector3 {
                            X = angularSpeed.x,
                            Y = angularSpeed.y,
                            Z = angularSpeed.z
                        }
                    }
                });
            }
        }

        ubiiClient.Publish(topicData);
    }

    private void CalculateForcesInternally()
    {
        foreach(KeyValuePair<HumanBodyBones, Transform> entry in mapBone2TargetTransform)
        {
            HumanBodyBones bone = entry.Key;
            Transform targetTransform = entry.Value;

            Rigidbody rigidbody = avatarPhysicsManager.GetRigidbodyFromBone(bone);
            Transform currentTransform = rigidbody.transform;

            // linear force
            Vector3 deltaPosition = GetPositionError(currentTransform.position, targetTransform.position);
            Vector3 targetVelocity = deltaPosition / Time.deltaTime;
            AvatarForceControl.AddForceFromTargetLinearVelocity(rigidbody, targetVelocity);

            // angular force
            Quaternion rotationDir = targetTransform.rotation * Quaternion.Inverse(currentTransform.rotation);
    
            float angleInDegrees;
            Vector3 rotationAxis;
            rotationDir.ToAngleAxis(out angleInDegrees, out rotationAxis);
            rotationAxis.Normalize();
    
            Vector3 angularDifference = rotationAxis * angleInDegrees * Mathf.Deg2Rad;
            Vector3 angularSpeed = angularDifference / Time.deltaTime;

            AvatarForceControl.AddTorqueFromTargetAngularVelocity(rigidbody, angularSpeed);
        }
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

    public string GetTopicTargetVelocities()
    {
        return "/" + ubiiClient.GetID() + TOPIC_PREFIX_TARGET_VELOCITIES;
    }

    public string GetTopicTargetLinearVelocities()
    {
        return "/" + ubiiClient.GetID() + TOPIC_PREFIX_TARGET_LINEAR_VELOCITIES;
    }

    public string GetTopicTargetAngularVelocities()
    {
        return "/" + ubiiClient.GetID() + TOPIC_PREFIX_TARGET_ANGULAR_VELOCITIES;
    }
}
