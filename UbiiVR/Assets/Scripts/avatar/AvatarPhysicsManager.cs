using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public enum JOINT_TYPE {
    CHARACTER_JOINT,
    CONFIGURABLE_JOINT
};

//[RequireComponent(typeof(ConfigJointManager))]
//[RequireComponent(typeof(BoneMeshContainer))]
public class AvatarPhysicsManager : MonoBehaviour
{
    public delegate void OnInitializedAction();
    public static event OnInitializedAction OnInitialized;

    const string PREFIX_GEOMETRY_SURFACES = "Alpha_Surface_";
    const string PREFIX_GEOMETRY_JOINTS = "Alpha_Joints_";
    const string PREFIX_ARMATURE = "mixamorig_";

    //public GameObject joints, surface;
    public float height = 1.7855f;
    public bool useGravity = false;
    public bool useJoints = true;
    public JOINT_TYPE jointType;
    public string prefixGeometrySurfaces = PREFIX_GEOMETRY_SURFACES;
    public string prefixGeometryJoints = PREFIX_GEOMETRY_JOINTS;
    public string prefixArmature = PREFIX_ARMATURE;

    public GameObject avatarPoseTracking = null;
    public bool resetVelocityCalculation = true;
    public bool testActuateRigidBody = true;
    public bool testActuateApplyLinearForce = true;
    public bool testActuateApplyAngularForce = true;

    bool initialized;

    Animator animator;

    //The bones of the character that physiscs should be applied to
    Dictionary<HumanBodyBones, GameObject> mapBone2GameObject = new Dictionary<HumanBodyBones, GameObject>();
    Dictionary<HumanBodyBones, Transform> mapBone2TargetTransform = new Dictionary<HumanBodyBones, Transform>();
    Dictionary<HumanBodyBones, Vector3> mapBone2TargetPosition = new Dictionary<HumanBodyBones, Vector3>();
    Dictionary<HumanBodyBones, Quaternion> mapBone2TargetRotation = new Dictionary<HumanBodyBones, Quaternion>();
    Dictionary<GameObject, HumanBodyBones> bonesPerGameObjectRemoteAvatar = new Dictionary<GameObject, HumanBodyBones>();

    List<HumanBodyBones> bonesInOrder = new List<HumanBodyBones>();

    // Use this for initialization
    void Start()
    {
    }

    void Update()
    {
    }
   
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.S))
        {
            InitializeBodyStructures();
        }

        if (initialized && testActuateRigidBody)
        {
            Animator poseTrackingAnimator = avatarPoseTracking.GetComponent<Animator>();

            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                //LastBone is not mapped to a bodypart, we need to skip it.
                if (bone != HumanBodyBones.LastBone)
                {
                    Transform armatureBoneTransform = animator.GetBoneTransform(bone);
                    if (armatureBoneTransform != null)
                    {
                        SetTargetTransformFromArmatureJoint(bone, poseTrackingAnimator.GetBoneTransform(bone));
                    }
                }
            }

            foreach(KeyValuePair<HumanBodyBones, GameObject> entry in mapBone2GameObject)
            {
                Rigidbody rigidbody = this.GetRigidbodyFromBone(entry.Key);
                if (rigidbody != null)
                {
                    ActuateRigidbodyFromBoneTargetPosRot(rigidbody, entry.Key);
                }
            }
            //mapBone2TargetTransform.Clear();
        }

        /*if (!initialized)
        {
            transform.localScale = UserAvatarService.Instance.transform.localScale = height / 1.7855f * Vector3.one;
        }*/
    }

    /// <summary>
    /// Hides the local avatar from camera view of the VR user. 
    /// </summary>
    /// <param name="toHide"></param>
    void SetInvisibleLocalAvatar(Transform toHide)
    {
        //layer 24 gets ignored by hmd camera
        toHide.gameObject.layer = 24;

        foreach (Transform child in toHide)
        {
            SetInvisibleLocalAvatar(child);
        }
    }

    /// <summary>
    ///     Maps all HumanBodyBones (assigned in the Animator) to their GameObjects in the scene in order to get access to all components.
    ///     Adds Rigidbody to both bodies, adds PDController to the avatar if useJoints is not chosen and initializes a ConfigJointManager otherwise.
    /// </summary>
    public void InitializeBodyStructures()
    {
        if (!initialized)
        {
            Debug.Log("InitializeBodyStructures() ...");
            //hide local avatar in camera
            if (/*gameObjectPerBoneLocalAvatar.Keys.Count == 0 &&*/ mapBone2GameObject.Keys.Count == 0)
            {
                animator = GetComponent<Animator>();
                if (animator == null) animator = GetComponentInChildren<Animator>();
                Animator poseTrackingAnimator = avatarPoseTracking.GetComponent<Animator>();

                foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
                {
                    //LastBone is not mapped to a bodypart, we need to skip it.
                    if (bone != HumanBodyBones.LastBone)
                    {
                        Transform armatureBoneTransform = animator.GetBoneTransform(bone);
                        //Debug.Log(bone);
                        if (armatureBoneTransform != null)
                        {
                            string boneName = armatureBoneTransform.name.Substring(prefixArmature.Length);
                            //Debug.Log(boneName);
                            GameObject boneGeometryGameObject = GameObject.Find(prefixGeometrySurfaces + boneName);
                            AddPhysicsComponents(armatureBoneTransform.gameObject, boneGeometryGameObject);
                            mapBone2GameObject.Add(bone, armatureBoneTransform.gameObject);
                        }
                    }
                }
            }
            initialized = true;
        }
    }

    /// <summary>
    ///     A method to return the Rigidbody of the GameObject that corresponds to a certain bodypart. 
    ///     Use this to gain access to the velocity of the bodypart.
    /// </summary>
    Rigidbody GetRigidbodyFromBone(HumanBodyBones boneID)
    {
        GameObject obj;
        if (this.mapBone2GameObject.TryGetValue(boneID, out obj))
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                return rb;
            }
            else
            {
                Debug.Log("No rigidbody is assigned to the bone " + boneID + "\nMake sure to run AvatarPhysicsManager.Initialize first.");
                return null;
            }
        }
        else
        {
            Debug.Log("No object is assigned to the bone " + boneID);
            return null;
        }
    }

    void AddPhysicsComponents(GameObject armatureBone, GameObject geometry)
    {
        if (geometry != null)
        {
            SkinnedMeshRenderer renderer = geometry.GetComponent<SkinnedMeshRenderer>();
            MeshCollider collider = armatureBone.AddComponent<MeshCollider>();
            collider.convex = true;
            collider.sharedMesh = renderer.sharedMesh;

            //geometry.transform.parent = armatureBone.transform;
            renderer.enabled = false;
        }

        Rigidbody rigidbody = armatureBone.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = armatureBone.AddComponent<Rigidbody>();
        }
        rigidbody.useGravity = this.useGravity;
        armatureBone.layer = 8;

        if (this.useJoints)
        {
            Joint joint = null;
            if (jointType == JOINT_TYPE.CONFIGURABLE_JOINT) {
                ConfigurableJoint configurableJoint = armatureBone.transform.parent.gameObject.AddComponent<ConfigurableJoint>();
                configurableJoint.xMotion = ConfigurableJointMotion.Locked;
                configurableJoint.yMotion = ConfigurableJointMotion.Locked;
                configurableJoint.zMotion = ConfigurableJointMotion.Locked;
                if (geometry == null)
                {
                    configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
                    configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
                    configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;
                }
                joint = configurableJoint;
            }
            else if (jointType == JOINT_TYPE.CHARACTER_JOINT)
            {
                CharacterJoint characterJoint = armatureBone.transform.parent.gameObject.AddComponent<CharacterJoint>();
                joint = characterJoint;
            }
            
            joint.connectedBody = rigidbody;
            //joint.enableCollision = true;
        }
    }

    /*void AddPhysicsComponents2(HumanBodyBones bone, GameObject boneObject, GameObject parentBoneObject)
    {
        if (boneObject == null) return;
        
        SkinnedMeshRenderer renderer = boneObject.GetComponent<SkinnedMeshRenderer>();
        MeshCollider collider = boneObject.AddComponent<MeshCollider>();
        collider.convex = true;
        collider.sharedMesh = renderer.sharedMesh;
        //boneObject.transform.parent = armatureBone.transform;
        renderer.enabled = false;

        Rigidbody rigidbody = boneObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = boneObject.AddComponent<Rigidbody>();
        }
        rigidbody.useGravity = this.useGravity;

        if (this.useJoints)
        {
            Joint joint = null;
            if (jointType == JOINT_TYPE.CONFIGURABLE_JOINT) {
                ConfigurableJoint configurableJoint = parentBoneObject.transform.parent.gameObject.AddComponent<ConfigurableJoint>();
                configurableJoint.xMotion = ConfigurableJointMotion.Locked;
                configurableJoint.yMotion = ConfigurableJointMotion.Locked;
                configurableJoint.zMotion = ConfigurableJointMotion.Locked;
                if (boneObject == null)
                {
                    configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
                    configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
                    configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;
                }
                joint = configurableJoint;
            }
            else if (jointType == JOINT_TYPE.CHARACTER_JOINT)
            {
                CharacterJoint characterJoint = parentBoneObject.transform.parent.gameObject.AddComponent<CharacterJoint>();
                joint = characterJoint;
            }
            
            joint.connectedBody = rigidbody;
            //joint.enableCollision = true;
        }
    }*/

    void SetTargetTransformFromArmatureJoint(HumanBodyBones bone, Transform armatureJointTransform)
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
                if (child.name.Contains(PREFIX_ARMATURE))
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

        /* target rotation */
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
    
    void ActuateRigidbodyFromBoneTargetPosRot(Rigidbody rigidbody, HumanBodyBones bone)
    {
        Transform currentTransform = rigidbody.transform;
        if (mapBone2TargetPosition.ContainsKey(bone))
        {
            Vector3 targetPosition = mapBone2TargetPosition[bone];
            Vector3 positionDelta = GetPositionError(currentTransform.position, targetPosition);
            Vector3 targetVelocity = positionDelta / Time.deltaTime;
            if (this.testActuateApplyLinearForce || bone == HumanBodyBones.Hips)
            {
                UserAvatarForceControl.AddForceFromTargetLinearVelocity(rigidbody, targetVelocity);
            }
        }

        if (mapBone2TargetRotation.ContainsKey(bone))
        {
            Quaternion targetRotation = mapBone2TargetRotation[bone];
            Quaternion currentRotation = rigidbody.rotation;
            Quaternion rotationDir = targetRotation * Quaternion.Inverse(currentRotation);
    
            float angleInDegrees;
            Vector3 rotationAxis;
            rotationDir.ToAngleAxis(out angleInDegrees, out rotationAxis);
            rotationAxis.Normalize();
    
            Vector3 angularDifference = rotationAxis * angleInDegrees * Mathf.Deg2Rad;
            Vector3 angularSpeed = angularDifference / Time.deltaTime;

            if(resetVelocityCalculation)
            {
                angularSpeed = new Vector3(angularSpeed.x, angularSpeed.y, angularSpeed.z);
            }
            if (this.testActuateApplyAngularForce) 
            {
                UserAvatarForceControl.AddTorqueFromTargetAngularVelocity(rigidbody, angularSpeed);
            }
        }
    }
}
