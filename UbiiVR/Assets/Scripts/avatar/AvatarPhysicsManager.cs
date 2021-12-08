﻿using System.Collections.Generic;
using UnityEngine;

public enum JOINT_TYPE
{
    CHARACTER_JOINT,
    CONFIGURABLE_JOINT
};

public struct MIXAMO_RIG
{
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
    //public int publishFrequency = 30;

    //private UbiiNode ubiiNode = null;
    private bool initialized;
    //private float tLastPublish = 0;
    //private float secondsBetweenPublish = 0;
    //private bool ubiiReady = false;

    private Animator animator;

    private Dictionary<HumanBodyBones, Rigidbody> mapBone2Rigidbody = new Dictionary<HumanBodyBones, Rigidbody>();
    private Dictionary<HumanBodyBones, Transform> mapBone2TargetTransform = new Dictionary<HumanBodyBones, Transform>();

    void Start()
    {
        InitializeBodyStructures();
        OnInitialized?.Invoke();
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
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                //LastBone is not mapped to a bodypart, we need to skip it.
                if (bone != HumanBodyBones.LastBone)
                {
                    Transform armatureBoneTransform = animator.GetBoneTransform(bone);
                    if (armatureBoneTransform != null)
                    {
                        string boneName = armatureBoneTransform.name.Substring(MIXAMO_RIG.PREFIX_ARMATURE.Length);
                        GameObject boneGeometryGameObject = GameObject.Find(MIXAMO_RIG.PREFIX_GEOMETRY_SURFACES + boneName);
                        Rigidbody rigidbody = AddPhysicsComponents(armatureBoneTransform.gameObject, boneGeometryGameObject);
                        mapBone2Rigidbody.Add(bone, rigidbody);
                    }
                }
            }

            initialized = true;
        }
    }

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
            if (jointType == JOINT_TYPE.CONFIGURABLE_JOINT)
            {
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

    #region getters

    public Dictionary<HumanBodyBones, Rigidbody> GetMapBone2Rigidbody()
    {
        return mapBone2Rigidbody;
    }

    #endregion
}
