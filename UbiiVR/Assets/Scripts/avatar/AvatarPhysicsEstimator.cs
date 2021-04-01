using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarPhysicsEstimator : MonoBehaviour
{
    public GameObject avatarPoseEstimation = null;
    public AvatarPhysicsManager avatarPhysicsManager = null;
    public int publishFrequency = 15;

    private UbiiClient ubiiClient = null;
    private bool running = false;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;

    private Dictionary<HumanBodyBones, Transform> mapBone2TargetTransform = new Dictionary<HumanBodyBones, Transform>();

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
        running = false;
    }

    void OnUbiiClientInitialized()
    {
        running = true;
        secondsBetweenPublish = 1f / (float)publishFrequency;
        tLastPublish = Time.time;
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

    void Update()
    {
        if (running)
        {
            //TODO: make ubii client networking threaded & thread-safe
            float tNow = Time.time;
            if (tNow >= tLastPublish + secondsBetweenPublish)
            {
                //PublishTopicDataIKTargets();
                tLastPublish = tNow;
            }
        }
    }

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
        Animator animatorPoseEstimation = avatarPoseEstimation.GetComponent<Animator>();

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
}
