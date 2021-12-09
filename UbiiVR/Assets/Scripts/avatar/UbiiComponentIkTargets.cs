using UnityEngine;
using System;

public class UbiiComponentIkTargets : MonoBehaviour
{
    static string TOPIC_PREFIX_IK_TARGET_POSE = "/avatar/ik_target/pose";

    public int publishFrequency = 15;
    public IKTargetsManager ikTargetsManager = null;
    //public VRTrackingManager vrTrackingManager = null;
    //public AnimationManager animationManager = null;

    private UbiiNode ubiiNode = null;
    private bool ubiiReady = false;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;

    // Start is called before the first frame update
    void Start()
    {
    }

    void OnEnable()
    {
        ubiiNode = FindObjectOfType<UbiiNode>();
        UbiiNode.OnInitialized += OnUbiiNodeInitialized;
    }

    void OnDisable()
    {
        UbiiNode.OnInitialized -= OnUbiiNodeInitialized;
        ubiiReady = false;
    }

    void OnUbiiNodeInitialized()
    {
        ubiiReady = true;
        secondsBetweenPublish = 1f / (float)publishFrequency;
        tLastPublish = Time.time;
    }

    void Update()
    {
        if (ubiiReady)
        {
            float tNow = Time.time;
            if (tNow >= tLastPublish + secondsBetweenPublish)
            {
                PublishTopicDataIKTargets();
                tLastPublish = tNow;
            }
        }
    }

    public static string GetIKTargetBodyPartString(IK_TARGET ikTarget)
    {
        return ikTarget.ToString().ToLower();
    }

    public string GetTopicIKTargetPose(IK_TARGET ikTarget)
    {
        return "/" + ubiiNode.GetID() + TOPIC_PREFIX_IK_TARGET_POSE + "/" + GetIKTargetBodyPartString(ikTarget);
    }

    private void PublishTopicDataIKTargets()
    {
        Ubii.TopicData.TopicDataRecordList recordList = new Ubii.TopicData.TopicDataRecordList();

        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            string topic = GetTopicIKTargetPose(ikTarget);

            Vector3 ikTargetPosition = ikTargetsManager.GetIkTargetPosition(ikTarget);
            Quaternion ikTargetRotation = ikTargetsManager.GetIkTargetRotation(ikTarget);
            recordList.Elements.Add(new Ubii.TopicData.TopicDataRecord
            {
                Topic = topic,
                Pose3D = new Ubii.DataStructure.Pose3D
                {
                    Position = new Ubii.DataStructure.Vector3
                    {
                        X = ikTargetPosition.x,
                        Y = ikTargetPosition.y,
                        Z = ikTargetPosition.z
                    },
                    Quaternion = new Ubii.DataStructure.Quaternion
                    {
                        X = ikTargetRotation.x,
                        Y = ikTargetRotation.y,
                        Z = ikTargetRotation.z,
                        W = ikTargetRotation.w
                    }
                }
            });
        }

        ubiiNode.Publish(recordList);
    }
}
