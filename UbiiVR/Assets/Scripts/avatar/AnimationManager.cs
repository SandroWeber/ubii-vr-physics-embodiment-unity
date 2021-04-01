using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    public Animator animator;
    public bool playAnimationLoop = false;

    private bool lastStatePlayAnimationLoop = false;
    private Dictionary<IK_TARGET, GameObject> emulatedIKTargets = new Dictionary<IK_TARGET, GameObject>();

    void Start()
    {
        if (animator == null)
        {
            animator = this.GetComponent<Animator>();
        }

        if (animator != null)
        {
            SetupEmulatedIKTargets();
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

    public Transform GetEmulatedIKTargetTransform(IK_TARGET bodyPart)
    {
        if (emulatedIKTargets.ContainsKey(bodyPart))
        {
            return emulatedIKTargets[bodyPart].transform;
        }
        else
        {
            return null;
        }
    }

    private void SetupEmulatedIKTargets()
    {
        Transform transformHead = animator.GetBoneTransform(HumanBodyBones.Head);
        emulatedIKTargets.Add(IK_TARGET.HEAD,
            VRTrackingManager.GenerateIKTarget("Animation Emulated IK Target Head", transformHead, IK_TARGET.HEAD));
        emulatedIKTargets.Add(IK_TARGET.VIEWING_DIRECTION,
            VRTrackingManager.GenerateIKTarget(
                "Animation Emulated IK Target Look At", 
                transformHead, 
                IK_TARGET.VIEWING_DIRECTION,
                Vector3.forward));

        Transform transformHips = animator.GetBoneTransform(HumanBodyBones.Hips);
        emulatedIKTargets.Add(IK_TARGET.HIP,
            VRTrackingManager.GenerateIKTarget("Animation Emulated IK Target Hips", transformHips, IK_TARGET.HIP));

        Transform transformLeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        emulatedIKTargets.Add(IK_TARGET.HAND_LEFT,
            VRTrackingManager.GenerateIKTarget("Animation Emulated IK Target Left Hand", transformLeftHand, IK_TARGET.HAND_LEFT));

        Transform transformRightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        emulatedIKTargets.Add(IK_TARGET.HAND_RIGHT,
            VRTrackingManager.GenerateIKTarget("Animation Emulated IK Target Right Hand", transformRightHand, IK_TARGET.HAND_RIGHT));

        Transform transformLeftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        emulatedIKTargets.Add(IK_TARGET.FOOT_LEFT,
            VRTrackingManager.GenerateIKTarget("Animation Emulated IK Target Left Foot", transformLeftFoot, IK_TARGET.FOOT_LEFT));

        Transform transformRightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        emulatedIKTargets.Add(IK_TARGET.FOOT_RIGHT,
            VRTrackingManager.GenerateIKTarget("Animation Emulated IK Target Right Foot", transformRightFoot, IK_TARGET.FOOT_RIGHT));
    }
}