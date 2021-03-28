using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Collections;

using static TrackingIKTargetManager;

public class TopicDataCommunicator : MonoBehaviour
{
    static string TOPIC_PREFIX_IK_TARGET_POSITION = "/avatar/ik_target/pos";
    static string TOPIC_PREFIX_IK_TARGET_ROTATION = "/avatar/ik_target/rot";
    static string TOPIC_PREFIX_IK_TARGET_POSE = "/avatar/ik_target/pose";

    public bool usePseudoTopicData = false;
    public int publishFrequency = 10;
    public TrackingIKTargetManager ikTargetManager = null;
    public AnimationManager animationManager = null;

    private PseudoTopicData pseudoTopicdata = null;
    private UbiiClient ubiiClient = null;
    private bool running = false;
    private IEnumerator publishCoroutine = null;
    private float tLastPublish = 0;
    private float secondsBetweenPublish = 0;

    // Start is called before the first frame update
    void Start()
    {
        ubiiClient = FindObjectOfType<UbiiClient>();
        pseudoTopicdata = PseudoTopicData.Instance;
    }

    void OnEnable()
    {
        UbiiClient.OnInitialized += OnUbiiClientInitialized;
    }

    void OnDisable()
    {
        UbiiClient.OnInitialized -= OnUbiiClientInitialized;
        if (publishCoroutine != null)
        {
            StopCoroutine(publishCoroutine);
        }
        running = false;
    }

    void OnUbiiClientInitialized()
    {
        running = true;
        secondsBetweenPublish = 1f / (float)publishFrequency;

        //TODO: make ubii client networking threaded & thread-safe
        //publishCoroutine = PublishIKTargetsCoroutine(waitTimeSeconds);
        //StartCoroutine(publishCoroutine);
        /*Task.Run(() => {
            while (running)
            {
                PublishTopicData();
    
                Thread.Sleep((int)(waitTimeSeconds * 1000));
                Debug.Log("running: " + running);
            }
        });*/
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
                PublishTopicData();
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
        return "/" + ubiiClient.GetID() + TOPIC_PREFIX_IK_TARGET_POSE + "/" + GetIKTargetBodyPartString(ikTarget);
    }

    private IEnumerator PublishIKTargetsCoroutine(float waitTime)
    {
        while (running)
        {
            PublishTopicData();

            yield return new WaitForSeconds(waitTime);
            Debug.Log("running: " + running);
        }
    }

    private void PublishTopicData()
    {
        Ubii.TopicData.TopicData topicData = new Ubii.TopicData.TopicData { TopicDataRecordList = new Ubii.TopicData.TopicDataRecordList() };

        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            string topic = GetTopicIKTargetPose(ikTarget);

            Transform ikTargetTransform = null;
            if (ikTargetManager.IsReady())
            {
                ikTargetTransform = ikTargetManager.GetIKTargetTransform(ikTarget);
            }
            else if (animationManager != null)
            {
                ikTargetTransform = animationManager.GetEmulatedIKTargetTransform(ikTarget);
            }
            
            topicData.TopicDataRecordList.Elements.Add(new Ubii.TopicData.TopicDataRecord
            {
                Topic = topic,
                Pose3D = new Ubii.DataStructure.Pose3D
                {
                    Position = new Ubii.DataStructure.Vector3 { 
                        X = ikTargetTransform.position.x,
                        Y = ikTargetTransform.position.y,
                        Z = ikTargetTransform.position.z },
                    Quaternion = new Ubii.DataStructure.Quaternion {
                        X = ikTargetTransform.rotation.x,
                        Y = ikTargetTransform.rotation.y,
                        Z = ikTargetTransform.rotation.z,
                        W = ikTargetTransform.rotation.w }
                }
            });
        }

        ubiiClient.Publish(topicData);
    }

    private void PublishPseudoTopicData()
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
                Transform ikTargetTransform = animationManager.GetEmulatedIKTargetTransform(ikTarget);
                pseudoTopicdata.SetVector3(topicPos, ikTargetTransform.position);
                pseudoTopicdata.SetQuaternion(topicRot, ikTargetTransform.rotation);
            }
        }
    }
}
