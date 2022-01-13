using UnityEngine;
using System;

public class UbiiComponentIkTargets : MonoBehaviour
{
    static string TOPIC_SUFFIX_IK_TARGETS = "/avatar/ik_targets";
    static string NAME = "Unity Physical Avatar - User IK Targets";
    static string DESCRIPTION = "Publishes IK Target Positions as Pose3D on individual topics for each target.";
    static string MESSAGE_FORMAT = "ubii.dataStructure.Pose3D";
    static string[] TAGS = new string[] { "avatar", "user tracking", "IK targets" };

    public int publishFrequency = 15;
    public IKTargetsManager ikTargetsManager = null;

    private UbiiNode ubiiNode = null;
    private bool ubiiReady = false;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;

    private Ubii.Devices.Component ubiiSpecs = null;

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

    public string GetTopicIKTargets()
    {
        return "/" + ubiiNode.GetID() + TOPIC_SUFFIX_IK_TARGETS;
    }

    private void PublishTopicDataIKTargets()
    {
        Ubii.TopicData.TopicDataRecord record = new Ubii.TopicData.TopicDataRecord {
            Topic = GetTopicIKTargets(),
            Object3DList = new Ubii.DataStructure.Object3DList()
        };

        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            Vector3 ikTargetPosition = ikTargetsManager.GetIkTargetPosition(ikTarget);
            Quaternion ikTargetRotation = ikTargetsManager.GetIkTargetRotation(ikTarget);

            record.Object3DList.Elements.Add(new Ubii.DataStructure.Object3D
            {
                Id = ikTarget.ToString(),
                Pose = new Ubii.DataStructure.Pose3D
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

        ubiiNode.Publish(record);
    }
}
