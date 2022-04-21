using System;
using UnityEngine;


//[RequireComponent(typeof(BoneMeshContainer))]
public class UbiiProcModuleAvatarMotionControls : MonoBehaviour
{

    private UbiiNode ubiiNode = null;
    private bool ubiiReady = false;
    private Ubii.Processing.ProcessingModule ubiiSpecs = null;

    void Start()
    {
    }

    void OnEnable()
    {
        ubiiSpecs = new Ubii.Processing.ProcessingModule
        {
            Name = "Unity Physical Avatar - Motion Controls PM",
            Description = "Input require IK Targets and current pose of avatar. Output are velocities to be applied to the avatar.",
            ProcessingMode = new Ubii.Processing.ProcessingMode {
                Frequency = new Ubii.Processing.ProcessingMode.Types.Frequency {
                    Hertz = (int) Math.Floor(1f / Time.fixedDeltaTime)
                }
            }
        };
        ubiiSpecs.Authors.AddRange(new string[] { "Sandro Weber (webers@in.tum.de)" });
        ubiiSpecs.Tags.AddRange(new string[] { "avatar", "motion control", "inverse kinematics", "velocity" });

        ubiiSpecs.Inputs.AddRange(new Ubii.Processing.ModuleIO[] {
            new Ubii.Processing.ModuleIO { InternalName = "ikTargets", MessageFormat = "ubii.dataStructure.Object3DList" },
            new Ubii.Processing.ModuleIO { InternalName = "avatarCurrentPoses", MessageFormat = "ubii.dataStructure.Object3DList" }
        });
        ubiiSpecs.Outputs.AddRange(new Ubii.Processing.ModuleIO[] {
            new Ubii.Processing.ModuleIO { InternalName = "avatarTargetVelocities", MessageFormat = "ubii.dataStructure.Object3DList" }
        });

        ubiiNode = FindObjectOfType<UbiiNode>();
        UbiiNode.OnInitialized += OnUbiiInitialized;
    }

    void OnDisable()
    {
        UbiiNode.OnInitialized -= OnUbiiInitialized;
        ubiiReady = false;
    }

    void OnUbiiInitialized()
    {
        ubiiReady = true;
        /*ubiiNode.CallService(new Ubii.Services.ServiceRequest {
            Topic = UbiiConstants.DefaultTopics.SERVICES.
        })*/
    }

    void Update()
    {
        if (ubiiReady)
        {
        }
    }
}
