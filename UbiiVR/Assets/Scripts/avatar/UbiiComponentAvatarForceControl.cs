using System.Collections.Generic;
using UnityEngine;

struct UbiiRigidbodyForces
{
    public Vector3 linear;
    public Vector3 angular;
}

public class UbiiComponentAvatarForceControl : MonoBehaviour
{
    static string TOPIC_SUFFIX_TARGET_VELOCITIES = "/avatar/target_velocities";

    public AvatarPhysicsManager avatarPhysicsManager = null;
    public AvatarPhysicsEstimator avatarPhysicsEstimator = null;

    private Dictionary<HumanBodyBones, UbiiRigidbodyForces> mapBone2TargetVelocities = new Dictionary<HumanBodyBones, UbiiRigidbodyForces>();
    private UbiiNode ubiiNode = null;
    private bool ubiiReady = false, physicsReady = false;
    private SubscriptionToken tokenTargetVelocities;

    private Ubii.Devices.Component ubiiSpecs = null;

    void Start()
    {
    }

    void OnEnable()
    {
        ubiiNode = FindObjectOfType<UbiiNode>();
        UbiiNode.OnInitialized += OnUbiiNodeInitialized;
        AvatarPhysicsManager.OnInitialized += OnPhysicsManagerInitialized;
    }

    void OnDisable()
    {
        UbiiNode.OnInitialized -= OnUbiiNodeInitialized;
        AvatarPhysicsManager.OnInitialized -= OnPhysicsManagerInitialized;

        ubiiReady = false;
        physicsReady = false;
    }

    void OnPhysicsManagerInitialized()
    {
        physicsReady = true;
    }

    async void OnUbiiNodeInitialized()
    {
        ubiiSpecs = new Ubii.Devices.Component
        {
            Name = "Unity Physical Avatar - Apply Velocities",
            Description = "Allows to apply linear and angular velocities by subscribing to a Object3DList. Object3D elements field 'Id' should be bone string equaling one of UnityEngine.HumanBodyBones (to be change to .json config). Object3D.Pose.Position equals linear velocity and Object3D.Pose.Euler equals angular velocity to be applied.",
            MessageFormat = "ubii.dataStructure.Object3DList",
            IoType = Ubii.Devices.Component.Types.IOType.Subscriber,
            Topic = GetTopicTargetVelocities()
        };
        ubiiSpecs.Tags.AddRange(new string[] { "avatar", "bones", "control", "velocity", "linear", "angular" });

        tokenTargetVelocities = await ubiiNode.SubscribeTopic(this.ubiiSpecs.Topic, (Ubii.TopicData.TopicDataRecord record) =>
        {
            for (int i = 0; i < record.Object3DList.Elements.Count; i++)
            {
                string boneString = record.Object3DList.Elements[i].Id;
                HumanBodyBones bone;
                if (HumanBodyBones.TryParse(boneString, out bone))
                {
                    Ubii.DataStructure.Pose3D pose = record.Object3DList.Elements[i].Pose;
                    Vector3 linear = new Vector3((float)pose.Position.X, (float)pose.Position.Y, (float)pose.Position.Z);
                    Vector3 angular = new Vector3((float)pose.Euler.X, (float)pose.Euler.Y, (float)pose.Euler.Z);
                    SetTargetVelocity(bone, linear, angular);
                }
            }
        });

        ubiiReady = true;
    }

    public string GetTopicTargetVelocities()
    {
        return "/" + ubiiNode.GetID() + TOPIC_SUFFIX_TARGET_VELOCITIES;
    }

    public void SetTargetVelocity(HumanBodyBones bone, Vector3 linear, Vector3 angular)
    {
        UbiiRigidbodyForces targetVelocities = new UbiiRigidbodyForces
        {
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

    // Update is called once per frame
    void FixedUpdate()
    {
        if (physicsReady && ubiiReady)
        {
            foreach (KeyValuePair<HumanBodyBones, Rigidbody> entry in avatarPhysicsManager.GetMapBone2Rigidbody())
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
            UbiiComponentAvatarForceControl.AddForceFromTargetLinearVelocity(rigidbody, targetVelocities.linear);
            UbiiComponentAvatarForceControl.AddTorqueFromTargetAngularVelocity(rigidbody, targetVelocities.angular);
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
        UbiiComponentAvatarForceControl.AddForce(rigidbody, newVelocity / Time.deltaTime, reset);
    }

    public static void AddTorque(Rigidbody rigidbody, Vector3 additiveTorque, bool reset = false)
    {
        if (reset)
        {
            rigidbody.angularVelocity = Vector3.zero;
        }
        rigidbody.AddTorque(additiveTorque, ForceMode.Acceleration);
    }

    public static void AddTorqueFromTargetAngularVelocity(Rigidbody rigidbody, Vector3 targetVelocity, bool reset = false)
    {
        Vector3 newAngularVelocity = targetVelocity - rigidbody.angularVelocity;
        UbiiComponentAvatarForceControl.AddTorque(rigidbody, newAngularVelocity / Time.deltaTime, reset);
    }
}
