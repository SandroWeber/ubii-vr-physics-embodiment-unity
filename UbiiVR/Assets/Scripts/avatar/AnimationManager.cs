using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using static TrackingIKTargetManager;

public class AnimationManager : MonoBehaviour
{
    public Animator animator;
    public bool playAnimationLoop = false;

    private bool lastStatePlayAnimationLoop = false;
    private Dictionary<IK_TARGET, GameObject> pseudoIKTargets = new Dictionary<IK_TARGET, GameObject>();

    void Start()
    {
        if (animator == null)
        {
            animator = this.GetComponent<Animator>();
        }

        if (animator != null)
        {
            SetupPseudoIKTargets();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            playAnimationLoop = !playAnimationLoop;
        }
        
        if (animator != null && lastStatePlayAnimationLoop != playAnimationLoop)
        {
            if (playAnimationLoop) {
                animator.Play("Base Layer.Walk", 0);
            }
            else{
                animator.Play("Base Layer.idle", 0);
            }
        }
        lastStatePlayAnimationLoop = playAnimationLoop;
    }

    public Transform GetPseudoIKTargetTransform(IK_TARGET bodyPart)
    {
        if (pseudoIKTargets.ContainsKey(bodyPart))
        {
            return pseudoIKTargets[bodyPart].transform;
        }
        else
        {
            return null;
        }
    }

    private void SetupPseudoIKTargets()
    {
        Transform transformHead = animator.GetBoneTransform(HumanBodyBones.Head);
        pseudoIKTargets.Add(IK_TARGET.HEAD,
            TrackingIKTargetManager.GenerateIKTarget("Animation Pseudo IK Target Head", transformHead, IK_TARGET.HEAD));
        pseudoIKTargets.Add(IK_TARGET.VIEWING_DIRECTION,
            TrackingIKTargetManager.GenerateIKTarget(
                "Animation Pseudo IK Target Look At", 
                transformHead, 
                IK_TARGET.VIEWING_DIRECTION,
                Vector3.forward));

        Transform transformHips = animator.GetBoneTransform(HumanBodyBones.Hips);
        pseudoIKTargets.Add(IK_TARGET.HIP,
            TrackingIKTargetManager.GenerateIKTarget("Animation Pseudo IK Target Hips", transformHips, IK_TARGET.HIP));

        Transform transformLeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        pseudoIKTargets.Add(IK_TARGET.HAND_LEFT,
            TrackingIKTargetManager.GenerateIKTarget("Animation Pseudo IK Target Left Hand", transformLeftHand, IK_TARGET.HAND_LEFT));

        Transform transformRightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        pseudoIKTargets.Add(IK_TARGET.HAND_RIGHT,
            TrackingIKTargetManager.GenerateIKTarget("Animation Pseudo IK Target Right Hand", transformRightHand, IK_TARGET.HAND_RIGHT));

        Transform transformLeftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        pseudoIKTargets.Add(IK_TARGET.FOOT_LEFT,
            TrackingIKTargetManager.GenerateIKTarget("Animation Pseudo IK Target Left Foot", transformLeftFoot, IK_TARGET.FOOT_LEFT));

        Transform transformRightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        pseudoIKTargets.Add(IK_TARGET.FOOT_RIGHT,
            TrackingIKTargetManager.GenerateIKTarget("Animation Pseudo IK Target Right Foot", transformRightFoot, IK_TARGET.FOOT_RIGHT));
    }
}