using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using static TrackingIKTargetManager;

[RequireComponent(typeof(Animator))]

public class UserAvatarIKControl : MonoBehaviour
{
    [SerializeField] private bool ikActive = true;
    [SerializeField] private bool usePseudoTopicData = true;
    [SerializeField] private TrackingIKTargetManager trackingIKTargetManager;
    [SerializeField] private TrackingHandManager trackingHandManager;
    [SerializeField] private Pose manualBodyOffset;
    [Tooltip("If we need to infer hip position from head and feet targets, adjust the ratio to place it along distance from feet to head.")]
    [SerializeField] private float inferredHipPosFeet2HeadDistanceRatio = 0.575f;

    protected Animator animator;

    private Dictionary<IK_TARGET, Transform> ikTargets = new Dictionary<IK_TARGET, Transform>();
    private Queue<Vector3> groundCenterTrajectory = new Queue<Vector3>();
    private int groundCenterTrajectorySize = 20;
    private bool initialized = false;
    private PseudoTopicData topicData = null;

    void Start()
    {
        animator = GetComponent<Animator>();
        topicData = PseudoTopicData.Instance;

        if (usePseudoTopicData)
        {
            foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                GameObject ikTargetObject = new GameObject("IK Target TopicData " + ikTarget.ToString());
                ikTargets.Add(ikTarget, ikTargetObject.transform);
            }
            initialized = true;
        }
    }

    void OnEnable()
    {
        TrackingIKTargetManager.OnInitialized += InitDirectIKTargets;
    }

    void OnDisable()
    {
        TrackingIKTargetManager.OnInitialized -= InitDirectIKTargets;
    }

    void InitDirectIKTargets()
    {
        if (usePseudoTopicData || !ikActive) return;

        /*ikTargetHead = trackingIKTargetManager.GetIKTargetTransform(IK_TARGET.HEAD);
        ikTargetLookAt = trackingIKTargetManager.GetIKTargetTransform(IK_TARGET.VIEWING_DIRECTION);
        ikTargetHip = trackingIKTargetManager.GetIKTargetTransform(IK_TARGET.HIP);
        ikTargetLeftHand = trackingIKTargetManager.GetIKTargetTransform(IK_TARGET.HAND_LEFT);
        ikTargetRightHand = trackingIKTargetManager.GetIKTargetTransform(IK_TARGET.HAND_RIGHT);
        ikTargetLeftFoot = trackingIKTargetManager.GetIKTargetTransform(IK_TARGET.FOOT_LEFT);
        ikTargetRightFoot = trackingIKTargetManager.GetIKTargetTransform(IK_TARGET.FOOT_RIGHT);*/
        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            ikTargets.Add(ikTarget, trackingIKTargetManager.GetIKTargetTransform(ikTarget));
        }
        initialized = true;
    }

    void Update()
    {
        if (usePseudoTopicData && initialized)
        {
            UpdateFromPseudoTopicData();
        }
    }

    void OnAnimatorIK()
    {
        if (!initialized)
        {
            return;
        }

        Transform ikTargetHip = ikTargets[IK_TARGET.HIP];
        Transform ikTargetLookAt = ikTargets[IK_TARGET.VIEWING_DIRECTION];
        Transform ikTargetHead = ikTargets[IK_TARGET.HEAD];
        Transform ikTargetLeftHand = ikTargets[IK_TARGET.HAND_LEFT];
        Transform ikTargetRightHand = ikTargets[IK_TARGET.HAND_RIGHT];
        Transform ikTargetLeftFoot = ikTargets[IK_TARGET.FOOT_LEFT];
        Transform ikTargetRightFoot = ikTargets[IK_TARGET.FOOT_RIGHT];

        // position body
        if (ikTargetHip != null)
        {
            animator.bodyPosition = ikTargetHip.position + manualBodyOffset.position;
            animator.bodyRotation = new Quaternion(
                ikTargetHip.rotation.x + manualBodyOffset.rotation.x,
                ikTargetHip.rotation.y + manualBodyOffset.rotation.y,
                ikTargetHip.rotation.z + manualBodyOffset.rotation.z,
                ikTargetHip.rotation.w);
        }
        // no body target, but head and feet targets to interpolate from
        else if (ikTargetHead != null && ikTargetLeftFoot != null && ikTargetRightFoot != null)
        {
            float leftFootHeight = ikTargetLeftFoot.position.y;
            float rightFootHeight = ikTargetRightFoot.position.y;

            // determine ground center of stability
            Vector3 groundCenter;
            float thresholdFootOffGround = 1.5f * trackingIKTargetManager.feetTargetOffsetAboveGround;
            // both feet on the ground
            if (leftFootHeight < thresholdFootOffGround && rightFootHeight < thresholdFootOffGround)
            {
                groundCenter = 0.5f * (ikTargetLeftFoot.position + ikTargetRightFoot.position);
            }
            // only left foot on the ground
            else if (leftFootHeight < thresholdFootOffGround)
            {
                groundCenter = ikTargetLeftFoot.position;
            }
            // only right foot on the ground
            else if (rightFootHeight < thresholdFootOffGround)
            {
                groundCenter = ikTargetRightFoot.position;
            }
            // both feet in the air
            else
            {
                groundCenter = 0.5f * (ikTargetLeftFoot.position + ikTargetRightFoot.position);
            }

            // smoooth out trajectory of ground center
            groundCenterTrajectory.Enqueue(groundCenter);
            while (groundCenterTrajectory.Count > groundCenterTrajectorySize)
            {
                groundCenterTrajectory.Dequeue();
            }
            groundCenter = new Vector3();
            foreach (Vector3 position in groundCenterTrajectory)
            {
                groundCenter += position;
            }
            groundCenter /= groundCenterTrajectory.Count;
            Vector3 bodyUp = (ikTargetHead.transform.position - this.transform.position).normalized;

            Vector3 bodyRight = (ikTargetHead.transform.right + ikTargetLeftFoot.transform.right + ikTargetRightFoot.transform.right).normalized;
            Vector3 bodyForward = Vector3.Cross(bodyRight, bodyUp).normalized;
            float distanceFeet2Head = Vector3.Distance(ikTargetHead.position, groundCenter);

            // set body position
            Vector3 bodyPosition = new Vector3();
            bodyPosition = new Vector3(groundCenter.x, groundCenter.y + inferredHipPosFeet2HeadDistanceRatio * distanceFeet2Head, groundCenter.z);
            bodyPosition.z -= 0.2f * bodyForward.z;

            this.transform.position = bodyPosition;
            //Debug.Log("head + feet inferred pos:");
            //Debug.Log(this.transform.position);

            // set body rotation
            Quaternion bodyRotation = Quaternion.LookRotation(bodyForward, bodyUp);
            this.transform.rotation = bodyRotation;

        }
        // no body target, only head target to interpolate from
        else if (ikTargetHead != null)
        {
            Vector3 inferredPos = ikTargetHead.position;
            inferredPos.y = inferredHipPosFeet2HeadDistanceRatio * inferredPos.y;
            this.transform.position = new Vector3(inferredPos.x, inferredPos.y, inferredPos.z);  // + Quaternion.FromToRotation(Vector3.up, interpolatedUpVector) * headToBodyOffset;

            Vector3 forward;
            if (ikTargetRightHand != null && ikTargetLeftHand != null)
            {
                Vector3 vec_controllers = ikTargetRightHand.position - ikTargetLeftHand.position;
                forward = Vector3.ProjectOnPlane(ikTargetHead.forward, vec_controllers);
            }
            else
            {
                forward = ikTargetHead.forward;
            }
            this.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, Vector3.up), Vector3.up);
        }

        if (ikTargetLookAt != null)
        {
            animator.SetLookAtWeight(1);
            animator.SetLookAtPosition(ikTargetLookAt.position);
        }

        if (ikTargetRightHand != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, ikTargetRightHand.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, ikTargetRightHand.rotation);
        }

        if (ikTargetLeftHand != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, ikTargetLeftHand.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, ikTargetLeftHand.rotation);
        }

        if (ikTargetRightFoot != null)
        {
            //rightFootTarget.up = Vector3.up;
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, ikTargetRightFoot.position);
            //Quaternion rightQuaternion = Quaternion.Euler(rightFootTarget.eulerAngles);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, /*rightQuaternion*/ikTargetRightFoot.rotation);
        }

        if (ikTargetLeftFoot != null)
        {
            //leftFootTarget.up = Vector3.up;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, ikTargetLeftFoot.position);
            //Quaternion leftQuaternion = Quaternion.Euler(leftFootTarget.eulerAngles);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, /*leftQuaternion*/ikTargetLeftFoot.rotation);
        }
    }

    private void LateUpdate()
    {
        if (trackingIKTargetManager.IsReady() && DetermineVRController.Instance.UseKnucklesControllers())
        {
            UpdateFingerTargets();
        }
    }

    private void UpdateFingerTargets()
    {
        // enum HumanBodyBones lowest finger index is LeftThumbProximal, highest finger index is RightLittleDistal
        for (int i = (int)HumanBodyBones.LeftThumbProximal; i <= (int)HumanBodyBones.RightLittleDistal; i++)
        {
            HumanBodyBones bone = (HumanBodyBones)i;
            this.animator.GetBoneTransform(bone).rotation = trackingHandManager.GetRemotePoseTarget(bone).rotation;
        }
    }

    private void UpdateFromPseudoTopicData()
    {
        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            //Debug.Log(ikTargets[ikTarget]);
            //Debug.Log(topicData.GetVector3(PseudoTopicDataCommunicator.GetTopicIKTargetPosition(ikTarget)));
            ikTargets[ikTarget].position = topicData.GetVector3(TopicDataCommunicator.GetTopicIKTargetPosition(ikTarget));
            ikTargets[ikTarget].rotation = topicData.GetQuaternion(TopicDataCommunicator.GetTopicIKTargetRotation(ikTarget));
        }
    }
}
