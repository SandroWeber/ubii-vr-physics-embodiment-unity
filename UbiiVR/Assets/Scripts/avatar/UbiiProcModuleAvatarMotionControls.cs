using UnityEngine;


public class UbiiProcModuleAvatarMotionControls : ProcessingModule
{
    private bool ubiiReady = false;
    private AvatarPoseEstimator avatarPoseEstimator = null;
    private AvatarPhysicsEstimator avatarPhysicsEstimator = null;

    public UbiiProcModuleAvatarMotionControls(Ubii.Processing.ProcessingModule specs, AvatarPoseEstimator avatarPoseEstimator, AvatarPhysicsEstimator avatarPhysicsEstimator)
      : base(specs)
    {
        this.avatarPoseEstimator = avatarPoseEstimator;
        this.avatarPhysicsEstimator = avatarPhysicsEstimator;
    }

    protected override void StartProcessingByFree()
    {
        Debug.Log("### UbiiProcModuleAvatarMotionControls.Start()");
        this.avatarPoseEstimator.StartProcessing(this.dictInputGetters["ikTargets"]);
        this.avatarPhysicsEstimator.StartProcessing(this.dictInputGetters["avatarCurrentPoses"], this.dictOutputSetters["avatarTargetVelocities"]);
    }

    public override void OnHalted()
    {
        this.avatarPhysicsEstimator.StopProcessing();
    }

    public static Ubii.Processing.ProcessingModule GetSpecifications()
    {
        Ubii.Processing.ProcessingModule specs = new Ubii.Processing.ProcessingModule
        {
            Name = "Unity Physical Avatar - Motion Controls PM",
            Description = "Input require IK Targets and current pose of avatar. Output are velocities to be applied to the avatar.",
            ProcessingMode = new Ubii.Processing.ProcessingMode
            {
                Free = new Ubii.Processing.ProcessingMode.Types.Free { }
            }
        };
        specs.Authors.AddRange(new string[] { "Sandro Weber (webers@in.tum.de)" });
        specs.Tags.AddRange(new string[] { "avatar", "motion control", "inverse kinematics", "velocity" });
        specs.Language = Ubii.Processing.ProcessingModule.Types.Language.Cs;

        specs.Inputs.AddRange(new Ubii.Processing.ModuleIO[] {
            new Ubii.Processing.ModuleIO { InternalName = "ikTargets", MessageFormat = "ubii.dataStructure.Object3DList" },
            new Ubii.Processing.ModuleIO { InternalName = "avatarCurrentPoses", MessageFormat = "ubii.dataStructure.Object3DList" }
        });
        specs.Outputs.AddRange(new Ubii.Processing.ModuleIO[] {
            new Ubii.Processing.ModuleIO { InternalName = "avatarTargetVelocities", MessageFormat = "ubii.dataStructure.Object3DList" }
        });

        return specs;
    }
}

public class UbiiProcModuleAvatarMotionControlsDBEntry : IProcessingModuleDatabaseEntry
{
    private AvatarPoseEstimator avatarPoseEstimator = null;
    private AvatarPhysicsEstimator avatarPhysicsEstimator = null;

    public UbiiProcModuleAvatarMotionControlsDBEntry(AvatarPoseEstimator avatarPoseEstimator, AvatarPhysicsEstimator avatarPhysicsEstimator)
    {
        this.avatarPoseEstimator = avatarPoseEstimator;
        this.avatarPhysicsEstimator = avatarPhysicsEstimator;
    }

    public Ubii.Processing.ProcessingModule GetSpecifications()
    {
        return UbiiProcModuleAvatarMotionControls.GetSpecifications();
    }

    public ProcessingModule CreateInstance(Ubii.Processing.ProcessingModule specs)
    {
        return new UbiiProcModuleAvatarMotionControls(specs, this.avatarPoseEstimator, this.avatarPhysicsEstimator);
    }
}
