using UnityEngine;

public class UbiiProcModuleSetupManager : MonoBehaviour
{
    public AvatarPoseEstimator avatarPoseEstimator = null;
    public AvatarPhysicsEstimator avatarPhysicsEstimator = null;

    private UbiiNode ubiiNode = null;

    private UbiiProcModuleAvatarMotionControlsDBEntry procModuleAvatarMotionControlsDBEntry = null;

    // Start is called before the first frame update
    void OnEnable()
    {
        ubiiNode = FindObjectOfType<UbiiNode>();
        if (ubiiNode != null && avatarPoseEstimator != null && avatarPhysicsEstimator != null)
        {
            procModuleAvatarMotionControlsDBEntry = new UbiiProcModuleAvatarMotionControlsDBEntry(avatarPoseEstimator, avatarPhysicsEstimator);
            ubiiNode.processingModuleDatabase.AddEntry(procModuleAvatarMotionControlsDBEntry);
        }
    }
}
