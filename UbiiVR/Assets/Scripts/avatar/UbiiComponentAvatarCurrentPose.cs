using System.Collections.Generic;
using UnityEngine;


//[RequireComponent(typeof(BoneMeshContainer))]
public class UbiiComponentAvatarCurrentPose : MonoBehaviour
{
    static string TOPIC_SUFFIX_CURRENT_POSE_LIST = "/avatar/current_pose/list";

    public int publishFrequency = 30;
    public AvatarPhysicsManager avatarPhysicsManager = null;

    private UbiiNode ubiiNode = null;
    private bool initialized;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;
    private bool ubiiReady = false, physicsReady = false;

    void Start()
    {
    }

    void OnEnable()
    {
        ubiiNode = FindObjectOfType<UbiiNode>();

        UbiiNode.OnInitialized += OnUbiiInitialized;
        AvatarPhysicsManager.OnInitialized += OnPhysicsInitialized;
    }

    void OnDisable()
    {
        UbiiNode.OnInitialized -= OnUbiiInitialized;
        AvatarPhysicsManager.OnInitialized -= OnPhysicsInitialized;
        physicsReady = false;
        ubiiReady = false;
    }

    void OnPhysicsInitialized()
    {
        physicsReady = true;
    }

    void OnUbiiInitialized()
    {
        secondsBetweenPublish = 1f / (float)publishFrequency;
        tLastPublish = Time.time;
        ubiiReady = true;
    }

    void Update()
    {
        if (physicsReady && ubiiReady)
        {
            float tNow = Time.time;
            if (tNow >= tLastPublish + secondsBetweenPublish)
            {
                PublishCurrentPosesList();
                tLastPublish = tNow;
            }
        }
    }

    private void PublishCurrentPosesList()
    {
        Ubii.TopicData.TopicDataRecord record = new Ubii.TopicData.TopicDataRecord
        {
            Topic = GetTopicCurrentPoseList(),
            Object3DList = new Ubii.DataStructure.Object3DList()
        };
        Dictionary<HumanBodyBones, Rigidbody> mapBone2Rigidbody = avatarPhysicsManager.GetMapBone2Rigidbody();
        foreach (KeyValuePair<HumanBodyBones, Rigidbody> entry in mapBone2Rigidbody)
        {
            Transform currentTransform = entry.Value.transform;

            record.Object3DList.Elements.Add(new Ubii.DataStructure.Object3D
            {
                Id = entry.Key.ToString(),
                Pose = new Ubii.DataStructure.Pose3D
                {
                    Position = new Ubii.DataStructure.Vector3
                    {
                        X = currentTransform.position.x,
                        Y = currentTransform.position.y,
                        Z = currentTransform.position.z
                    },
                    Quaternion = new Ubii.DataStructure.Quaternion
                    {
                        X = currentTransform.rotation.x,
                        Y = currentTransform.rotation.y,
                        Z = currentTransform.rotation.z,
                        W = currentTransform.rotation.w
                    }
                }
            });
        }

        ubiiNode.Publish(record);
    }

    public string GetTopicCurrentPoseList()
    {
        return "/" + ubiiNode.GetID() + TOPIC_SUFFIX_CURRENT_POSE_LIST;
    }
}
