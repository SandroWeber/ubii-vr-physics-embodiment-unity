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

    void Start()
    {
        if (animator != null)
        {
            animator = this.GetComponent<Animator>();
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
}