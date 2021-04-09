using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct UbiiRigidbodyForces {
    public Vector3 linear;
    public Vector3 angular;
}

public class AvatarForceControl : MonoBehaviour
{
    public AvatarPhysicsManager avatarPhysicsManager = null;
    public AvatarPhysicsEstimator avatarPhysicsEstimator = null;

    private Dictionary<HumanBodyBones, UbiiRigidbodyForces> mapBone2TargetVelocities = new Dictionary<HumanBodyBones, UbiiRigidbodyForces>();
    private UbiiClient ubiiClient = null;
    private bool ubiiReady = false, physicsReady = false;

    void Start()
    {
        ubiiClient = FindObjectOfType<UbiiClient>();
    }

    void OnEnable()
    {
        UbiiClient.OnInitialized += OnUbiiClientInitialized;
        AvatarPhysicsManager.OnInitialized += OnPhysicsManagerInitialized;
    }

    void OnDisable()
    {
        UbiiClient.OnInitialized -= OnUbiiClientInitialized;
        AvatarPhysicsManager.OnInitialized -= OnPhysicsManagerInitialized;

        ubiiReady = false;
        physicsReady = false;
    }

    async void OnUbiiClientInitialized()
    {
        await ubiiClient.Subscribe(avatarPhysicsEstimator.GetTopicTargetVelocities(), (Ubii.TopicData.TopicDataRecord record) => {
            for (int i=0; i < record.Object3DList.Elements.Count; i++)
            {
                string boneString = record.Object3DList.Elements[i].Id;
                HumanBodyBones bone;
                if (HumanBodyBones.TryParse(boneString, out bone)) {
                    Ubii.DataStructure.Pose3D pose = record.Object3DList.Elements[i].Pose;
                    Vector3 linear = new Vector3((float)pose.Position.X, (float)pose.Position.Y, (float)pose.Position.Z);
                    Vector3 angular = new Vector3((float)pose.Euler.X, (float)pose.Euler.Y, (float)pose.Euler.Z);
                    SetTargetVelocity(bone, linear, angular);
                }
            }
        });

        ubiiReady = true;
    }

    public void SetTargetVelocity(HumanBodyBones bone, Vector3 linear, Vector3 angular)
    {
        UbiiRigidbodyForces targetVelocities = new UbiiRigidbodyForces {
            linear = linear,
            angular = angular
        };
        if (mapBone2TargetVelocities.ContainsKey(bone))
        {
            mapBone2TargetVelocities[bone] = targetVelocities;
        }
        else
        {
            mapBone2TargetVelocities.Add(bone, targetVelocities);
        }
    }

    void OnPhysicsManagerInitialized()
    {
        physicsReady = true;
    }
   
    // Update is called once per frame
    void FixedUpdate()
    {
        if (physicsReady)
        {
            foreach(KeyValuePair<HumanBodyBones, Rigidbody> entry in avatarPhysicsManager.GetMapBone2Rigidbody())
            {
                Rigidbody rigidbody = avatarPhysicsManager.GetRigidbodyFromBone(entry.Key);
                if (rigidbody != null)
                {
                    ActuateRigidbodyFromTargetVelocities(entry.Key, rigidbody);
                }
            }
        }
    }

    private void ActuateRigidbodyFromTargetVelocities(HumanBodyBones bone, Rigidbody rigidbody)
    {
        UbiiRigidbodyForces targetVelocities;
        if (mapBone2TargetVelocities.TryGetValue(bone, out targetVelocities))
        {
            AvatarForceControl.AddForceFromTargetLinearVelocity(rigidbody, targetVelocities.linear);
            AvatarForceControl.AddTorqueFromTargetAngularVelocity(rigidbody, targetVelocities.angular);
        }

    }

    public static void AddForce(Rigidbody rigidbody, Vector3 additiveForce, bool reset = false)
    {
        if (reset)
        {
            rigidbody.velocity = Vector3.zero;
        }
        rigidbody.AddForce(additiveForce, ForceMode.Acceleration);
    }

    public static void AddForceFromTargetLinearVelocity(Rigidbody rigidbody, Vector3 targetVelocity, bool reset = false)
    {
        Vector3 newVelocity = targetVelocity - rigidbody.velocity;
        AvatarForceControl.AddForce(rigidbody, newVelocity / Time.deltaTime, reset);
    }

    public static void AddTorque(Rigidbody rigidbody, Vector3 additiveTorque, bool reset = false)
    {
        if(reset)
        {
            rigidbody.angularVelocity = Vector3.zero;
        }
        rigidbody.AddTorque(additiveTorque, ForceMode.Acceleration);
    }

    public static void AddTorqueFromTargetAngularVelocity(Rigidbody rigidbody, Vector3 targetVelocity, bool reset = false)
    {
        Vector3 newAngularVelocity = targetVelocity - rigidbody.angularVelocity;
        AvatarForceControl.AddTorque(rigidbody, newAngularVelocity / Time.deltaTime, reset);
    }
}
