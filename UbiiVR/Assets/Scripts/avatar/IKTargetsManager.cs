using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Collections;

struct UbiiPose3D
{
    public Vector3 position;
    public Quaternion rotation;
}

public enum IK_TARGET
{
    HEAD = 0,
    VIEWING_DIRECTION,
    HIP,
    HAND_LEFT,
    HAND_RIGHT,
    FOOT_LEFT,
    FOOT_RIGHT
}

public class IKTargetsManager : MonoBehaviour
{
    static string TOPIC_PREFIX_IK_TARGET_POSITION = "/avatar/ik_target/pos";
    static string TOPIC_PREFIX_IK_TARGET_ROTATION = "/avatar/ik_target/rot";
    static string TOPIC_PREFIX_IK_TARGET_POSE = "/avatar/ik_target/pose";

    public bool usePseudoTopicData = false;
    public int publishFrequency = 15;
    public VRTrackingManager vrTrackingManager = null;
    public AnimationManager animationManager = null;
    public Vector3 manualPositionOffset = new Vector3();

    private PseudoTopicData pseudoTopicdata = null;
    private UbiiNode ubiiNode = null;
    private bool running = false;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;

    // Start is called before the first frame update
    void Start()
    {
        pseudoTopicdata = PseudoTopicData.Instance;
    }

    void OnEnable()
    {
        ubiiNode = FindObjectOfType<UbiiNode>();
        UbiiNode.OnInitialized += OnUbiiNodeInitialized;
    }

    void OnDisable()
    {
        UbiiNode.OnInitialized -= OnUbiiNodeInitialized;
        running = false;
    }

    void OnUbiiNodeInitialized()
    {
        running = true;
        secondsBetweenPublish = 1f / (float)publishFrequency;
        tLastPublish = Time.time;
    }

    void Update()
    {
        if (running)
        {
            //TODO: make ubii client networking threaded & thread-safe
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

    public static string GetTopicIKTargetPosition(IK_TARGET ikTarget)
    {
        return TOPIC_PREFIX_IK_TARGET_POSITION + "/" + GetIKTargetBodyPartString(ikTarget);
    }

    public static string GetTopicIKTargetRotation(IK_TARGET ikTarget)
    {
        return TOPIC_PREFIX_IK_TARGET_ROTATION + "/" + GetIKTargetBodyPartString(ikTarget);
    }

    public string GetTopicIKTargetPose(IK_TARGET ikTarget)
    {
        return "/" + ubiiNode.GetID() + TOPIC_PREFIX_IK_TARGET_POSE + "/" + GetIKTargetBodyPartString(ikTarget);
    }

    private void PublishTopicDataIKTargets()
    {
        //Ubii.TopicData.TopicData topicData = new Ubii.TopicData.TopicData { TopicDataRecordList = new Ubii.TopicData.TopicDataRecordList() };

        Ubii.TopicData.TopicDataRecordList recordList = new Ubii.TopicData.TopicDataRecordList();

        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            string topic = GetTopicIKTargetPose(ikTarget);

            Transform ikTargetTransform = null;
            if (vrTrackingManager.IsReady())
            {
                ikTargetTransform = vrTrackingManager.GetIKTargetTransform(ikTarget);
            }
            else if (animationManager != null)
            {
                ikTargetTransform = animationManager.GetEmulatedIKTargetTransform(ikTarget);
            }

            recordList.Elements.Add(new Ubii.TopicData.TopicDataRecord
            {
                Topic = topic,
                Pose3D = new Ubii.DataStructure.Pose3D
                {
                    Position = new Ubii.DataStructure.Vector3
                    {
                        X = ikTargetTransform.position.x + manualPositionOffset.x,
                        Y = ikTargetTransform.position.y + manualPositionOffset.y,
                        Z = ikTargetTransform.position.z + manualPositionOffset.z
                    },
                    Quaternion = new Ubii.DataStructure.Quaternion
                    {
                        X = ikTargetTransform.rotation.x,
                        Y = ikTargetTransform.rotation.y,
                        Z = ikTargetTransform.rotation.z,
                        W = ikTargetTransform.rotation.w
                    }
                }
            });
        }

        ubiiNode.Publish(recordList);
    }

    private void PublishPseudoTopicData()
    {
        if (vrTrackingManager.IsReady())
        {
            foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                string partName = GetIKTargetBodyPartString(ikTarget);
                string topicPos = TOPIC_PREFIX_IK_TARGET_POSITION + "/" + partName;
                string topicRot = TOPIC_PREFIX_IK_TARGET_ROTATION + "/" + partName;
                Transform ikTargetTransform = vrTrackingManager.GetIKTargetTransform(ikTarget);
                pseudoTopicdata.SetVector3(topicPos, ikTargetTransform.position);
                pseudoTopicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
            }
        }
        else if (animationManager != null)
        {
            foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                string topicPos = GetTopicIKTargetPosition(ikTarget);
                string topicRot = GetTopicIKTargetRotation(ikTarget);
                Transform ikTargetTransform = animationManager.GetEmulatedIKTargetTransform(ikTarget);
                pseudoTopicdata.SetVector3(topicPos, ikTargetTransform.position);
                pseudoTopicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
            }
        }
    }
}
