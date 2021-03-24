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
    //public bool useJoints = true;
    //[NonSerialized]
    //public bool tuningInProgress;
    //public bool showLocalAvatar;
    bool initialized;

    /*[Header("PD Control - Only used when useJoints is unchecked")]
    public float PDKp = 125;
    public float PDKd = 20;*/

    //could be used to initialize only for specific body parts, e.g. arm.
    //BodyGroups.BODYGROUP bodyGroup = BodyGroups.BODYGROUP.ALL_COMBINED;

    //BodyGroups bodyGroupsRemote;
    //BodyGroups bodyGroupsTarget;

    //ConfigJointManager configJointManager;
    //LatencyHandler latencyHandler;

    Animator animator;
    //Animator animatorLocalAvatar;

    //The bones of the character that physiscs should be applied to
    Dictionary<HumanBodyBones, GameObject> mapBone2GameObject = new Dictionary<HumanBodyBones, GameObject>();
    //Dictionary<HumanBodyBones, Transform> mapBone2TargetTransform = new Dictionary<HumanBodyBones, Transform>();
    Dictionary<HumanBodyBones, Vector3> mapBone2TargetPosition = new Dictionary<HumanBodyBones, Vector3>();
    Dictionary<HumanBodyBones, Quaternion> mapBone2TargetRotation = new Dictionary<HumanBodyBones, Quaternion>();
    //Dictionary<HumanBodyBones, Quaternion> orientationPerBoneRemoteAvatarAtStart = new Dictionary<HumanBodyBones, Quaternion>();
    Dictionary<GameObject, HumanBodyBones> bonesPerGameObjectRemoteAvatar = new Dictionary<GameObject, HumanBodyBones>();
    //The bones of the character that the remote avatar imitates
    //Dictionary<HumanBodyBones, GameObject> gameObjectPerBoneLocalAvatar = new Dictionary<HumanBodyBones, GameObject>();

    List<HumanBodyBones> bonesInOrder = new List<HumanBodyBones>();

    // Use this for initialization
    void Start()
    {
        //latencyHandler = GetComponent<LatencyHandler>();
        //InitializeBodyStructures();
    }

    void Update()
    {
    }
   
    // Update is called once per frame
    void FixedUpdate()
    {
        /*if (!useJoints)
        {
            UpdatePDControllers();
        }
        else
        {
            if (initialized)
            {
                if (!tuningInProgress && latencyHandler.latency_ms == 0)
                {
                    UpdateJoints();
                    //UpdateJointsRecursive(mapBone2GameObject[HumanBodyBones.Hips].transform);

                }
            }
        }*/

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
                    //rigidbody.AddForce(entry.Value);
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
                //animatorLocalAvatar = GameObject.FindGameObjectWithTag("Target").GetComponent<Animator>();

                /*if (!showLocalAvatar)
                {
                    SetInvisibleLocalAvatar(animatorLocalAvatar.gameObject.transform);
                }*/
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
                            /*string parentBoneName = armatureBoneTransform.parent.name.Substring(prefixArmature.Length);
                            GameObject parentBoneGameObject = GameObject.Find(prefixGeometrySurfaces + boneName);
                            if (boneGameObject == null)
                            {
                                boneGameObject = GameObject.Find(prefixGeometryJoints + boneName);
                            }*/
                            //Debug.Log(boneGameObject);
                            AddPhysicsComponents(armatureBoneTransform.gameObject, boneGeometryGameObject);
                            mapBone2GameObject.Add(bone, armatureBoneTransform.gameObject);
                            /*AddPhysicsComponents2(bone, boneGeometryGameObject, parentBoneGameObject);
                            mapBone2GameObject.Add(bone, boneGeometryGameObject);*/
                        }
                        //Transform armatureBoneTransformLocalAvatar = animatorLocalAvatar.GetarmatureBoneTransform(bone);
                        //We have to skip unassigned bodyparts.
                        /*if (armatureBoneTransform != null && armatureBoneTransformLocalAvatar != null)
                        {
                            //build Dictionaries
                            mapBone2GameObject.Add(bone, armatureBoneTransform.gameObject);
                            //gameObjectPerBoneLocalAvatar.Add(bone, armatureBoneTransformLocalAvatar.gameObject);

                            Quaternion tmp = new Quaternion();
                            tmp = armatureBoneTransform.localRotation;
                            orientationPerBoneRemoteAvatarAtStart.Add(bone, tmp);

                            bonesPerGameObjectRemoteAvatar.Add(armatureBoneTransform.gameObject, bone);

                            AssignRigidbodys(bone);

                            //if (!useJoints)
                            //{
                            //    AssignPDController(bone);
                            //}
                        }*/
                    }
                }

                //bodyGroupsRemote = new BodyGroups(mapBone2GameObject);
                //bodyGroupsTarget = new BodyGroups(gameObjectPerBoneLocalAvatar);

                /*if (useJoints)
                {
                    configJointManager = GetComponent<ConfigJointManager>();
                    configJointManager.SetupJoints();
                    SetupOrder(mapBone2GameObject[HumanBodyBones.Hips].transform);
                    bonesInOrder.Reverse();
                }*/
            }
            //animatorLocalAvatar.gameObject.GetComponent<UserAvatarIKControl>().coordStartAnchor = gameObjectPerBoneLocalAvatar[HumanBodyBones.Hips].transform.position.y;
            //joints.SetActive(true);
            //surface.SetActive(true);
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

    /*void AssignRigidbodys(HumanBodyBones bone)
    {
        mapBone2GameObject[bone].AddComponent<Rigidbody>();
        mapBone2GameObject[bone].GetComponent<Rigidbody>().useGravity = true;

        //gameObjectPerBoneLocalAvatar[bone].AddComponent<Rigidbody>();
        //gameObjectPerBoneLocalAvatar[bone].GetComponent<Rigidbody>().useGravity = false;
    }*/

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

    void AddPhysicsComponents2(HumanBodyBones bone, GameObject boneObject, GameObject parentBoneObject)
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
    }

    void SetTargetTransformFromArmatureJoint(HumanBodyBones bone, Transform armatureJointTransform)
    {
        //Debug.Log("#######");
        //Debug.Log(bone);
        /* target position */
        Vector3 armaturePosition = armatureJointTransform.position;
        Vector3 targetPosition = new Vector3();
        // since we receive the position of the armature joint and not the position of the bone in between those joints
        // which is the geometry carrying a rigidbody, colliders and mass
        // we need to interpolate the true target position between it and all its child joint positions
        // to arrive at an estimate center position where the bone would be
        targetPosition.Set(armaturePosition.x, armaturePosition.y, armaturePosition.z);
        if (bone != HumanBodyBones.UpperChest)
        {
            foreach (Transform child in armatureJointTransform)
            {
                targetPosition += child.position;
            }
            targetPosition /= armatureJointTransform.childCount + 1;
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

            /*string targetIndicatorName = "target-indicator_" + rigidbody.name;
            GameObject targetIndicator = GameObject.Find(targetIndicatorName);
            if (!targetIndicator)
            {
                targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                targetIndicator.name = targetIndicatorName;
                targetIndicator.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            }
            targetIndicator.transform.position = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);*/

            Vector3 positionDelta = GetPositionError(currentTransform.position, targetPosition);
            Vector3 targetVelocity = positionDelta / Time.deltaTime;
            if (this.testActuateApplyLinearForce || bone == HumanBodyBones.Hips) ApplyForce(targetVelocity, rigidbody, resetVelocityCalculation);
        }

        if (mapBone2TargetRotation.ContainsKey(bone))
        {
            Quaternion targetRotation = mapBone2TargetRotation[bone];
            /*Vector3 angularDelta = GetAngularErrorEuler(currentTransform.rotation, targetRotation);
            Vector3 angularSpeed = angularDelta * Mathf.Deg2Rad / Time.deltaTime;*/
            
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
            if (this.testActuateApplyAngularForce) ApplyTorque(angularSpeed, rigidbody, resetVelocityCalculation);
        }
        /*Debug.Log(rigidbody);
        Debug.Log(currentTransform.position);
        Debug.Log(targetTransform.position);*/
       
        //recorder.RecordDomain(time, positionDelta, angularDelta, rigidbody.velocity, rigidbody.angularVelocity, entry.Key.gameObject.name);

        //rigidbody.MovePosition(entry.Key.position + positionDelta);
        //rigidbody.MoveRotation(target.rotation);

        // angularDelta = OrientTorque(angularDelta);
       
        //Vector3 angu = (angularDelta * Mathf.Deg2Rad);
        //angu /= Time.deltaTime;

        //Vector3 newVelocity = 0.5f * (velo - prefVelo);
        //Vector3 newAngularAcce = 0.5f * (angu - prefAngl) / Time.deltaTime;

        //Vector3 newAccleration = 0.5f * positionDelta / Time.deltaTime / Time.deltaTime;
        //Vector3 newAngularAcce = 0.5f * angularDelta / Time.deltaTime / Time.deltaTime * Mathf.Deg2Rad ;

        // FORCE MODES:
        // IMPULSE mass * distance / time.
        // FORCE mass * distance / time^2.

        //rigidbody.AddTorque(newAngularAcce * rigidbody.mass);

        /* Vector3 x = Vector3.Cross(oldDir.normalized, newDir.normalized);
         float theta = Mathf.Asin(x.magnitude);
         Vector3 w = x.normalized * theta / Time.fixedDeltaTime;

         Quaternion q = transform.rotation * rigidbody.inertiaTensorRotation;
         Vector3 T = q * Vector3.Scale(rigidbody.inertiaTensor, (Quaternion.Inverse(q) * w));

         rigidbody.AddTorque(T, ForceMode.Impulse) */

        //recorder.RecordCodomain(time, velo, angularSpeed, entry.Key.gameObject.name);
    }
    
    public static void ApplyForce(Vector3 targetLinearVelocity, Rigidbody rigidbody, bool reset)
    {
        if (reset)
        {
            rigidbody.velocity = Vector3.zero;
        }
        Vector3 previousVelocity = rigidbody.velocity;
        Vector3 newVelocity = (targetLinearVelocity - previousVelocity);
        rigidbody.AddForce(newVelocity / Time.deltaTime, ForceMode.Acceleration);
    }

    public static void ApplyTorque(Vector3 targetAngularVelocity, Rigidbody rigidbody, bool reset)
    {
        if(reset)
        {
            rigidbody.angularVelocity = Vector3.zero;
        }
        Vector3 newAngularVelocity = targetAngularVelocity - rigidbody.angularVelocity;
        /*Vector3 prefAnguLocal = rigidbody.gameObject.transform.InverseTransformDirection(newAngularVelocity);
        Vector3 TorqueLocal;
        Vector3 prefAnguTemp = prefAnguLocal;
        prefAnguTemp = rigidbody.inertiaTensorRotation * prefAnguTemp;
        prefAnguTemp.Scale(rigidbody.inertiaTensor);
        TorqueLocal = Quaternion.Inverse(rigidbody.inertiaTensorRotation) * prefAnguTemp;

        Vector3 Torque = rigidbody.gameObject.transform.TransformDirection(TorqueLocal);
        */
        if (reset)
            rigidbody.AddTorque(newAngularVelocity / Time.deltaTime, ForceMode.Force);
        else
        {
            //rigidbody.angularVelocity = Vector3.zero;
            rigidbody.AddTorque(newAngularVelocity / Time.deltaTime, ForceMode.Acceleration);
        }
    }

    /*void AssignPDController(HumanBodyBones bone)
    {
        PDController pd = mapBone2GameObject[bone].AddComponent<PDController>();
        pd.rigidbody = mapBone2GameObject[bone].GetComponent<Rigidbody>();
        pd.oldVelocity = mapBone2GameObject[bone].GetComponent<PDController>().rigidbody.velocity;
        pd.proportionalGain = PDKp;
        pd.derivativeGain = PDKd;
    }*/

    /*void UpdatePDControllers()
    {
        foreach (HumanBodyBones bone in mapBone2GameObject.Keys)
        {
            UpdateSpecificPDController(bone);
        }
    }*/

    /*void UpdateSpecificPDController(HumanBodyBones bone)
    {
        Rigidbody targetRb = GetRigidbodyFromBone(false, bone);
        if (targetRb != null)
        {
            mapBone2GameObject[bone].GetComponent<PDController>().SetDestination(gameObjectPerBoneLocalAvatar[bone].transform, targetRb.velocity);
        }
    }*/

    /// <summary>
    /// Sends the target rotation of all joints to the ConfigJointManager
    /// </summary>
    /*void UpdateJoints()
    {
        foreach (HumanBodyBones bone in mapBone2GameObject.Keys)
        {
            configJointManager.SetTagetRotation(bone, gameObjectPerBoneLocalAvatar[bone].transform.localRotation);
        }
    }*/

    void SetupOrder(Transform armatureBoneTransform)
    {
        HumanBodyBones tmpBone;
        if (bonesPerGameObjectRemoteAvatar.TryGetValue(armatureBoneTransform.gameObject, out tmpBone))
        {

            GameObject tmp;
            if (mapBone2GameObject.TryGetValue(tmpBone, out tmp))
            {

                if (tmp.GetComponent<ConfigurableJoint>() != null)
                {
                    Rigidbody targetRb = GetRigidbodyFromBone(tmpBone);
                    if (targetRb != null)
                    {
                        bonesInOrder.Add(bonesPerGameObjectRemoteAvatar[armatureBoneTransform.gameObject]);
                    }
                }
            }

        }
        foreach (Transform child in armatureBoneTransform)
        {
            SetupOrder(child);
        }
    }

    #region getters
    public Dictionary<HumanBodyBones, GameObject> GetmapBone2GameObjectDictionary()
    {
        return mapBone2GameObject;
    }

    /*public Dictionary<HumanBodyBones, Quaternion> GetmapBone2GameObjectDictionaryAtStart()
    {
        return orientationPerBoneRemoteAvatarAtStart;
    }*/

    /*public Dictionary<HumanBodyBones, GameObject> GetGameObjectPerBoneLocalAvatarDictionary()
    {
        return gameObjectPerBoneLocalAvatar;
    }

    public List<HumanBodyBones> GetFixedJoints()
    {
        if (useJoints)
        {
            return configJointManager.GetFixedJoints();
        }
        else
        {
            throw new Exception("You are trying to access the ConfigurableJoints, but useJoints is set to false");
        }
    }

    public ConfigurableJoint GetJointInTemplate(HumanBodyBones bone, Vector3 axis)
    {
        return configJointManager.GetJointInTemplate(bone, axis);
    }

    public BodyGroups GetBodyGroupsRemote()
    {
        return bodyGroupsRemote;
    }

    public BodyGroups GetBodyGroupsTarget()
    {
        return bodyGroupsTarget;
    }

    public BodyGroups.BODYGROUP GetSelectedBodyGroup()
    {
        return bodyGroup;
    }*/

    #endregion

    #region tuning functionalities
    /*public void LockAvatarJointsExceptCurrent(ConfigurableJoint joint)
    {
        configJointManager.LockAvatarJointsExceptCurrent(joint);
    }*/

    /*public void UnlockAvatarJoints()
    {
        configJointManager.UnlockAvatarJoints();
    }*/

    /*public bool IsJointUnneeded(string joint)
    {
        ConfigurableJoint remoteJoint = LocalPhysicsToolkit.GetRemoteJointOfCorrectAxisFromString(joint, mapBone2GameObject);
        return remoteJoint.lowAngularXLimit.limit == 0 && remoteJoint.highAngularXLimit.limit == 0;
    }*/
    #endregion

    #region legacy

    public bool isInitialized()
    {
        return initialized;
    }

    Dictionary<HumanBodyBones, GameObject> SafeCopyOfRemoteAvatarDictionary()
    {
        //Dictionary<HumanBodyBones, GameObject> copy = mapBone2GameObject.ToDictionary(k => k.Key, k => k.Value);
        //return copy;
        return new Dictionary<HumanBodyBones, GameObject>(mapBone2GameObject);
        /*
        List<JointTransformContainer> jointTransformContainers = new List<JointTransformContainer>();
        JointTransformContainer container;
        foreach (HumanBodyBones bone in gameObjectPerBoneTarget.Keys)
        {
            container = new JointTransformContainer(bone, gameObjectPerBoneTarget[bone].transform);
            jointTransformContainers.Add(container);
        }
        return jointTransformContainers;  
        */

        /*
        Dictionary<HumanBodyBones, GameObject> tmp = new Dictionary<HumanBodyBones, GameObject>();
        foreach(HumanBodyBones bone in gameObjectPerBoneTarget.Keys)
        {
            tmp.Add(bone, gameObjectPerBoneTarget[bone]);
        }
        return tmp;
        */

    }

    /*void UpdateJointsRecursive(Transform armatureBoneTransform)
    {
        foreach (HumanBodyBones bone in bonesInOrder)
        {
            configJointManager.SetTagetRotation(bone, gameObjectPerBoneLocalAvatar[bone].transform.localRotation);
        }
    }*/
    /*
    void WaitUntilRotationComplete(HumanBodyBones bone, Quaternion rotation)
    {
        if (mapBone2GameObject[bone].transform.rotation.x != rotation.x && mapBone2GameObject[bone].transform.rotation.y != rotation.y && mapBone2GameObject[bone].transform.rotation.z != rotation.z)
        {
            WaitUntilRotationComplete(bone, rotation);
        }
        else
        {
            mapBone2GameObject[bone].GetComponent<ConfigurableJoint>().connectedBody.freezeRotation = false;
        }
    }
    */

    /// <summary>
    /// Sets start orientation when joints have been removed and then readded. Might need to be looked into again.
    /// </summary>
    /*public void RecalculateStartOrientations()
    {
        foreach (HumanBodyBones bone in mapBone2GameObject.Keys)
        {
            Transform armatureBoneTransformLocalAvatar = animatorLocalAvatar.GetarmatureBoneTransform(bone);
            Quaternion tmp = new Quaternion();
            tmp = armatureBoneTransformLocalAvatar.rotation;
            orientationPerBoneRemoteAvatarAtStart[bone] = tmp;
            configJointManager.SetStartOrientation();
        }
    }*/
    #endregion
}
