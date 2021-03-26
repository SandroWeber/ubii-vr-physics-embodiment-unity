using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using static TrackingIKTargetManager;

public class PseudoTopicDataCommunicator : MonoBehaviour
{
    static string TOPIC_PREFIX_IK_TARGET_POSITION = "/topic/avatar/ik_target/pos";
    static string TOPIC_PREFIX_IK_TARGET_ROTATION = "/topic/avatar/ik_target/rot";

    public TrackingIKTargetManager ikTargetManager = null;
    public AnimationManager animationManager = null;

    private PseudoTopicData topicdata = null;

    // Start is called before the first frame update
    void Start()
    {
        topicdata = PseudoTopicData.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("PseudoTopicDataCommunicator.Update()");
        if (ikTargetManager.IsReady())
        {
            foreach(IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                string partName = GetIKTargetBodyPartString(ikTarget);
                string topicPos = TOPIC_PREFIX_IK_TARGET_POSITION + "/" + partName;
                //Debug.Log(topicPos);
                string topicRot = TOPIC_PREFIX_IK_TARGET_ROTATION + "/" + partName;
                //Debug.Log(topicRot);
                Transform ikTargetTransform = ikTargetManager.GetIKTargetTransform(ikTarget);
                topicdata.SetVector3(topicPos, ikTargetTransform.position);
                topicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
            }
        }
        else if (animationManager != null)
        {
            foreach(IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                string topicPos = GetTopicIKTargetPosition(ikTarget);
                //Debug.Log(topicPos);
                string topicRot = GetTopicIKTargetRotation(ikTarget);
                //Debug.Log(topicRot);
                Transform ikTargetTransform = animationManager.GetPseudoIKTargetTransform(ikTarget);
                topicdata.SetVector3(topicPos, ikTargetTransform.position);
                topicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
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
}
