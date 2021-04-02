using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public enum JOINT_TYPE {
    CHARACTER_JOINT,
    CONFIGURABLE_JOINT
};

public struct MIXAMO_RIG {
    public static string PREFIX_GEOMETRY_SURFACES = "Alpha_Surface_";
    public static string PREFIX_GEOMETRY_JOINTS = "Alpha_Joints_";
    public static string PREFIX_ARMATURE = "mixamorig_";
}

//[RequireComponent(typeof(BoneMeshContainer))]
[RequireComponent(typeof(Animator))]
public class AvatarPhysicsManager : MonoBehaviour
{
    public delegate void OnInitializedAction();
    public static event OnInitializedAction OnInitialized;
    
    static string TOPIC_PREFIX_CURRENT_POSE = "/avatar/current_pose";

    //public GameObject joints, surface;
    public float height = 1.7855f;
    public bool useGravity = false;
    public bool useJoints = true;
    public JOINT_TYPE jointType;

    public bool resetVelocityCalculation = true;
    public int publishFrequency = 30;

    private UbiiClient ubiiClient = null;
    private bool initialized;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;
    private bool ubiiReady = false;

    private Animator animator;

    //The bones of the character that physiscs should be applied to
    //Dictionary<HumanBodyBones, GameObject> mapBone2GameObject = new Dictionary<HumanBodyBones, GameObject>();
    private Dictionary<HumanBodyBones, Rigidbody> mapBone2Rigidbody = new Dictionary<HumanBodyBones, Rigidbody>();
    private Dictionary<HumanBodyBones, Transform> mapBone2TargetTransform = new Dictionary<HumanBodyBones, Transform>();
    //Dictionary<GameObject, HumanBodyBones> bonesPerGameObjectRemoteAvatar = new Dictionary<GameObject, HumanBodyBones>();

    //private List<HumanBodyBones> bonesInOrder = new List<HumanBodyBones>();

    // Use this for initialization
    void Start()
    {
        ubiiClient = FindObjectOfType<UbiiClient>();
        InitializeBodyStructures();
        OnInitialized();
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
                //PublishCurrentPosesPerBone();
                PublishCurrentPosesList();
                tLastPublish = tNow;
            }
        }
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
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                //LastBone is not mapped to a bodypart, we need to skip it.
                if (bone != HumanBodyBones.LastBone)
                {
                    Transform armatureBoneTransform = animator.GetBoneTransform(bone);
                    //Debug.Log(bone);
                    if (armatureBoneTransform != null)
                    {
                        string boneName = armatureBoneTransform.name.Substring(MIXAMO_RIG.PREFIX_ARMATURE.Length);
                        //Debug.Log(boneName);
                        GameObject boneGeometryGameObject = GameObject.Find(MIXAMO_RIG.PREFIX_GEOMETRY_SURFACES + boneName);
                        Rigidbody rigidbody = AddPhysicsComponents(armatureBoneTransform.gameObject, boneGeometryGameObject);
                        //mapBone2GameObject.Add(bone, armatureBoneTransform.gameObject);
                        mapBone2Rigidbody.Add(bone, rigidbody);
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
    /*public Rigidbody GetRigidbodyFromBone(HumanBodyBones boneID)
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
                Debug.Log("No rigidbody is assigned to the bone " + boneID + "\nMake sure to initialize AvatarPhysicsManager first.");
                return null;
            }
        }
        else
        {
            Debug.Log("No object is assigned to the bone " + boneID);
            return null;
        }
    }*/
    public Rigidbody GetRigidbodyFromBone(HumanBodyBones boneID)
    {
        return mapBone2Rigidbody[boneID];
    }

    private Rigidbody AddPhysicsComponents(GameObject armatureBone, GameObject geometry)
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

        return rigidbody;
    }

    private void PublishCurrentPosesPerBone()
    {
        Ubii.TopicData.TopicData topicData = new Ubii.TopicData.TopicData { TopicDataRecordList = new Ubii.TopicData.TopicDataRecordList() };
        foreach (KeyValuePair<HumanBodyBones, Rigidbody> entry in mapBone2Rigidbody)
        {
            Transform currentTransform = entry.Value.transform;
            string topic = GetTopicCurrentPose(entry.Key);
            
            topicData.TopicDataRecordList.Elements.Add(new Ubii.TopicData.TopicDataRecord
            {
                Topic = topic,
                Pose3D = new Ubii.DataStructure.Pose3D
                {
                    Position = new Ubii.DataStructure.Vector3 { 
                        X = currentTransform.position.x,
                        Y = currentTransform.position.y,
                        Z = currentTransform.position.z },
                    Quaternion = new Ubii.DataStructure.Quaternion {
                        X = currentTransform.rotation.x,
                        Y = currentTransform.rotation.y,
                        Z = currentTransform.rotation.z,
                        W = currentTransform.rotation.w }
                }
            });
        }

        ubiiClient.Publish(topicData);
    }

    private void PublishCurrentPosesList()
    {
        Ubii.TopicData.TopicData topicData = new Ubii.TopicData.TopicData { 
            TopicDataRecord = new Ubii.TopicData.TopicDataRecord {
                Topic = GetTopicCurrentPoseList(),
                Object3DList = new Ubii.DataStructure.Object3DList()
            } 
        };
        foreach (KeyValuePair<HumanBodyBones, Rigidbody> entry in mapBone2Rigidbody)
        {
            Transform currentTransform = entry.Value.transform;
            
            topicData.TopicDataRecord.Object3DList.Elements.Add(new Ubii.DataStructure.Object3D
            {
                Id = entry.Key.ToString(),
                Pose = new Ubii.DataStructure.Pose3D
                {
                    Position = new Ubii.DataStructure.Vector3 { 
                        X = currentTransform.position.x,
                        Y = currentTransform.position.y,
                        Z = currentTransform.position.z },
                    Quaternion = new Ubii.DataStructure.Quaternion {
                        X = currentTransform.rotation.x,
                        Y = currentTransform.rotation.y,
                        Z = currentTransform.rotation.z,
                        W = currentTransform.rotation.w }
                }
            });
        }

        ubiiClient.Publish(topicData);
    }

    public string GetTopicCurrentPose(HumanBodyBones bone)
    {
        return "/" + ubiiClient.GetID() + TOPIC_PREFIX_CURRENT_POSE + "/" + bone.ToString();
    }

    public string GetTopicCurrentPoseList()
    {
        return "/" + ubiiClient.GetID() + TOPIC_PREFIX_CURRENT_POSE + "/list";
    }

    #region getters

    public Dictionary<HumanBodyBones, Rigidbody> GetMapBone2Rigidbody()
    {
        return mapBone2Rigidbody;
    }

    #endregion
}
