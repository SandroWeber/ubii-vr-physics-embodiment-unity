using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using static TrackingIKTargetManager;

public class TopicDataCommunicator : MonoBehaviour
{
    static string TOPIC_PREFIX_IK_TARGET_POSITION = "/topic/avatar/ik_target/pos";
    static string TOPIC_PREFIX_IK_TARGET_ROTATION = "/topic/avatar/ik_target/rot";
    static string TOPIC_PREFIX_IK_TARGET_POSE = "/topic/avatar/ik_target/pose";

    public bool usePseudoTopicData = false;
    public TrackingIKTargetManager ikTargetManager = null;
    public AnimationManager animationManager = null;

    private PseudoTopicData pseudoTopicdata = null;
    private UbiiClient ubiiClient = null;

    // Start is called before the first frame update
    void Start()
    {
        ubiiClient = FindObjectOfType<UbiiClient>();
        pseudoTopicdata = PseudoTopicData.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("PseudoTopicDataCommunicator.Update()");
        if (ikTargetManager.IsReady())
        {
            foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                string partName = GetIKTargetBodyPartString(ikTarget);
                string topicPos = TOPIC_PREFIX_IK_TARGET_POSITION + "/" + partName;
                //Debug.Log(topicPos);
                string topicRot = TOPIC_PREFIX_IK_TARGET_ROTATION + "/" + partName;
                //Debug.Log(topicRot);
                Transform ikTargetTransform = ikTargetManager.GetIKTargetTransform(ikTarget);
                pseudoTopicdata.SetVector3(topicPos, ikTargetTransform.position);
                pseudoTopicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
            }
        }
        else if (animationManager != null)
        {
            foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                string topicPos = GetTopicIKTargetPosition(ikTarget);
                //Debug.Log(topicPos);
                string topicRot = GetTopicIKTargetRotation(ikTarget);
                //Debug.Log(topicRot);
                Transform ikTargetTransform = animationManager.GetPseudoIKTargetTransform(ikTarget);
                pseudoTopicdata.SetVector3(topicPos, ikTargetTransform.position);
                pseudoTopicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
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

    private void PublishIKTargets()
    {
        List<Ubii.TopicData.TopicDataRecord> recordList = new List<Ubii.TopicData.TopicDataRecord>();

        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            string partName = GetIKTargetBodyPartString(ikTarget);
            string topic = TOPIC_PREFIX_IK_TARGET_POSE + "/" + partName;
            //Debug.Log(topic);

            Transform ikTargetTransform = null;
            if (ikTargetManager.IsReady())
            { }
            else if (animationManager != null)
            {
                Transform ikTargetTransform = animationManager.GetPseudoIKTargetTransform(ikTarget);
            }

            recordList.Add(new Ubii.TopicData.TopicDataRecord
            {
                Topic = topicTestPublishSubscribe,
                Pose3D = new Ubii.DataStructure.Pose3D
                {
                    Position = { X = ikTargetTransform.position.x, Y = ikTargetTransform.position.y, Z = ikTargetTransform.position.z },
                    Quaternion = { 
                        X = ikTargetTransform.rotation.x, 
                        Y = ikTargetTransform.rotation.y, 
                        Z = ikTargetTransform.rotation.z, 
                        W = ikTargetTransform.rotation.w }
                }
            });

        }

        if (ikTargetManager.IsReady())
        {
            foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                string partName = GetIKTargetBodyPartString(ikTarget);
                string topicPos = TOPIC_PREFIX_IK_TARGET_POSITION + "/" + partName;
                //Debug.Log(topicPos);
                string topicRot = TOPIC_PREFIX_IK_TARGET_ROTATION + "/" + partName;
                //Debug.Log(topicRot);
                Transform ikTargetTransform = ikTargetManager.GetIKTargetTransform(ikTarget);
                pseudoTopicdata.SetVector3(topicPos, ikTargetTransform.position);
                pseudoTopicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
            }
        }
        else if (animationManager != null)
        {
            foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                string topicPos = GetTopicIKTargetPosition(ikTarget);
                //Debug.Log(topicPos);
                string topicRot = GetTopicIKTargetRotation(ikTarget);
                //Debug.Log(topicRot);
                Transform ikTargetTransform = animationManager.GetPseudoIKTargetTransform(ikTarget);
                pseudoTopicdata.SetVector3(topicPos, ikTargetTransform.position);
                pseudoTopicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
            }
        }

        ubiiClient.Publish(new Ubii.TopicData.TopicData { TopicDataRecordList = recordList });
    }
}
