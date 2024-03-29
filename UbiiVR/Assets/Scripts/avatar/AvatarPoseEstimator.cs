﻿using System;
using System.Collections.Generic;
using UnityEngine;

using Ubii.TopicData;

public struct UbiiPose3D
{
    public Vector3 position;
    public Quaternion rotation;
}

[RequireComponent(typeof(Animator))]
public class AvatarPoseEstimator : MonoBehaviour
{
    public bool ikActive = true;
    public bool useTopicData = true;
    public UbiiComponentIkTargets ubiiComponentIkTargets = null;
    public VRTrackingManager vrTrackingManager;
    public TrackingHandManager trackingHandManager;
    public Pose manualBodyOffset;
    [Tooltip("If we need to infer hip position from head and feet targets, adjust the ratio to place it along distance from feet to head.")]
    public float inferredHipPosFeet2HeadDistanceRatio = 0.575f;

    protected Animator animator;

    private Dictionary<IK_TARGET, Transform> mapIKTargetTransforms = new Dictionary<IK_TARGET, Transform>();
    //TODO: pull directly from client side topicdata during update once implemented?
    private Dictionary<IK_TARGET, UbiiPose3D> mapIKTarget2UbiiPose = new Dictionary<IK_TARGET, UbiiPose3D>();
    private Queue<Vector3> groundCenterTrajectory = new Queue<Vector3>();
    private int groundCenterTrajectorySize = 20;
    private bool initialized = false;
    private bool running = false;
    private Func<TopicDataRecord> GetIkTargets = null;

    void Start()
    {
        animator = GetComponent<Animator>();
        this.Initialize();
    }

    void OnEnable()
    {
        VRTrackingManager.OnInitialized += InitInternalIKTargets;
    }

    void OnDisable()
    {
        VRTrackingManager.OnInitialized -= InitInternalIKTargets;
    }

    public void StartProcessing(Func<TopicDataRecord> GetIkTargets)
    {
        this.GetIkTargets = GetIkTargets;
        running = true;
    }

    public void StopProcessing()
    {
        running = false;
    }

    void Initialize()
    {
        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            GameObject ikTargetObject = new GameObject("IK-Target PoseEstimator " + ikTarget.ToString());
            mapIKTargetTransforms.Add(ikTarget, ikTargetObject.transform);
            mapIKTarget2UbiiPose.Add(ikTarget, new UbiiPose3D
            {
                position = new Vector3(),
                rotation = new Quaternion()
            });
        }

        initialized = true;
    }

    //TODO: intermediate map saving poses from topic data might not be necessary anymore, saves some complexity
    private void SaveIkTargetsToMap(TopicDataRecord record)
    {
        for (int i = 0; i < record.Object3DList.Elements.Count; i++)
        {
            string ikTargetString = record.Object3DList.Elements[i].Id;
            Ubii.DataStructure.Pose3D pose3D = record.Object3DList.Elements[i].Pose;
            IK_TARGET ikTarget;
            if (IK_TARGET.TryParse(ikTargetString, out ikTarget))
            {
                UbiiPose3D pose = mapIKTarget2UbiiPose[ikTarget];
                pose.position.Set(
                    (float)pose3D.Position.X,
                    (float)pose3D.Position.Y,
                    (float)pose3D.Position.Z);
                pose.rotation.Set(
                    (float)pose3D.Quaternion.X,
                    (float)pose3D.Quaternion.Y,
                    (float)pose3D.Quaternion.Z,
                    (float)pose3D.Quaternion.W);
                mapIKTarget2UbiiPose[ikTarget] = pose;
            }
        }
    }

    void InitInternalIKTargets()
    {
        if (useTopicData || !ikActive) return;

        foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
        {
            mapIKTargetTransforms.Add(ikTarget, vrTrackingManager.GetIKTargetTransform(ikTarget));
        }
        initialized = true;
    }

    void Update()
    {
        if (useTopicData && initialized && running)
        {
            this.SaveIkTargetsToMap(this.GetIkTargets());
            foreach (IK_TARGET ikTarget in Enum.GetValues(typeof(IK_TARGET)))
            {
                UbiiPose3D pose = mapIKTarget2UbiiPose[ikTarget];
                Transform ikTargetTransform = mapIKTargetTransforms[ikTarget];

                Vector3 pos = pose.position;
                Quaternion rot = pose.rotation;
                ikTargetTransform.position = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
                ikTargetTransform.rotation = new Quaternion((float)rot.x, (float)rot.y, (float)rot.z, (float)rot.w);
            }
        }
    }

    void OnAnimatorIK()
    {
        if (!running) return;

        Transform ikTargetLookAt = mapIKTargetTransforms[IK_TARGET.VIEWING_DIRECTION];
        Transform ikTargetLeftHand = mapIKTargetTransforms[IK_TARGET.HAND_LEFT];
        Transform ikTargetRightHand = mapIKTargetTransforms[IK_TARGET.HAND_RIGHT];
        Transform ikTargetLeftFoot = mapIKTargetTransforms[IK_TARGET.FOOT_LEFT];
        Transform ikTargetRightFoot = mapIKTargetTransforms[IK_TARGET.FOOT_RIGHT];

        // position body
        SetAnimatorBodyPose();

        if (ikTargetLookAt != null)
        {
            animator.SetLookAtWeight(1);
            animator.SetLookAtPosition(ikTargetLookAt.position);
        }
        if (ikTargetLeftHand != null) SetAnimatorIKPose(AvatarIKGoal.LeftHand, ikTargetLeftHand);
        if (ikTargetRightHand != null) SetAnimatorIKPose(AvatarIKGoal.RightHand, ikTargetRightHand);
        if (ikTargetLeftFoot != null) SetAnimatorIKPose(AvatarIKGoal.LeftFoot, ikTargetLeftFoot);
        if (ikTargetRightFoot != null) SetAnimatorIKPose(AvatarIKGoal.RightFoot, ikTargetRightFoot);
    }

    private void SetAnimatorBodyPose()
    {
        Transform ikTargetHip = mapIKTargetTransforms[IK_TARGET.HIP];
        Transform ikTargetHead = mapIKTargetTransforms[IK_TARGET.HEAD];
        Transform ikTargetLeftHand = mapIKTargetTransforms[IK_TARGET.HAND_LEFT];
        Transform ikTargetRightHand = mapIKTargetTransforms[IK_TARGET.HAND_RIGHT];
        Transform ikTargetLeftFoot = mapIKTargetTransforms[IK_TARGET.FOOT_LEFT];
        Transform ikTargetRightFoot = mapIKTargetTransforms[IK_TARGET.FOOT_RIGHT];

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
            float thresholdFootOffGround = 1.5f * vrTrackingManager.feetTargetOffsetAboveGround;
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

            animator.bodyPosition = bodyPosition;

            // set body rotation
            Quaternion bodyRotation = Quaternion.LookRotation(bodyForward, bodyUp);
            animator.bodyRotation = bodyRotation;

        }
        // no body target, only head target to interpolate from
        else if (ikTargetHead != null)
        {
            Vector3 inferredPos = ikTargetHead.position;
            inferredPos.y = inferredHipPosFeet2HeadDistanceRatio * inferredPos.y;
            animator.bodyPosition = new Vector3(inferredPos.x, inferredPos.y, inferredPos.z);  // + Quaternion.FromToRotation(Vector3.up, interpolatedUpVector) * headToBodyOffset;

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
            animator.bodyRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, Vector3.up), Vector3.up);
        }
    }

    private void SetAnimatorIKPose(AvatarIKGoal ikGoal, Transform targetTransform)
    {
        animator.SetIKPositionWeight(ikGoal, 1);
        animator.SetIKRotationWeight(ikGoal, 1);

        animator.SetIKPosition(ikGoal, targetTransform.position);
        animator.SetIKRotation(ikGoal, targetTransform.rotation);
    }

    private void LateUpdate()
    {
        if (vrTrackingManager.IsReady() && DetermineVRController.Instance.UseKnucklesControllers())
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
}
