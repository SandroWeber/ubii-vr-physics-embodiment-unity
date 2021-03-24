using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PseudoTopicDataCommunicator : MonoBehaviour
{
    static string TOPIC_PREFIX_IK_TARGET_POSITION = "/topic/avatar/ik_target/pos";
    static string TOPIC_PREFIX_IK_TARGET_ROTATION = "/topic/avatar/ik_target/rot";

    public TrackingIKTargetManager ikTargetManager = null;

    private PseudoTopicData topicdata = null;

    // Start is called before the first frame update
    void Start()
    {
        topicdata = PseudoTopicData.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("PseudoTopicDataCommunicator.Update()");
        if (ikTargetManager.IsReady())
        {
            foreach(TrackingIKTargetManager.BODY_PART part in Enum.GetValues(typeof(TrackingIKTargetManager.BODY_PART)))
            {
                string topicPos = TOPIC_PREFIX_IK_TARGET_POSITION + "/" + part.ToString();
                Debug.Log(topicPos);
                string topicRot = TOPIC_PREFIX_IK_TARGET_ROTATION + "/" + part.ToString();
                Debug.Log(topicRot);
                Transform ikTarget = ikTargetManager.GetIKTargetTransform(part);
                topicdata.SetVector3(topicPos, ikTarget.position);
                topicdata.SetQuaternion(topicRot, ikTarget.rotation);
            }

            /*Transform ikTargetHead = ikTargetManager.GetIKTargetTransform(TrackingIKTargetManager.BODY_PART.HEAD);
            topicdata.SetVector3(TOPIC_IK_TARGET_HIP_POSITION, ikTargetHead.position);
            topicdata.SetQuaternion(TOPIC_IK_TARGET_HIP_POSITION, ikTargetHead.rotation);

            Transform ikTargetLookAt = ikTargetManager.GetIKTargetTransform(TrackingIKTargetManager.BODY_PART.VIEWING_DIRECTION);
            topicdata.SetVector3(TOPIC_IK_TARGET_HIP_POSITION, ikTargetHead.position);
            topicdata.SetQuaternion(TOPIC_IK_TARGET_HIP_POSITION, ikTargetHead.rotation);
            ikTargetHip = ikTargetManager.GetIKTargetTransform(TrackingIKTargetManager.BODY_PART.HIP);
            ikTargetLeftHand = ikTargetManager.GetIKTargetTransform(TrackingIKTargetManager.BODY_PART.HAND_LEFT);
            ikTargetRightHand = ikTargetManager.GetIKTargetTransform(TrackingIKTargetManager.BODY_PART.HAND_RIGHT);
            ikTargetLeftFoot = ikTargetManager.GetIKTargetTransform(TrackingIKTargetManager.BODY_PART.FOOT_LEFT);
            ikTargetRightFoot = ikTargetManager.GetIKTargetTransform(TrackingIKTargetManager.BODY_PART.FOOT_RIGHT);*/
        }
    }
}
