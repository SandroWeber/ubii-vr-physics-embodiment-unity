using UnityEngine;

public enum IK_TARGET
{
    HEAD = 0,
    VIEWING_DIRECTION,
    HIP,
    HAND_LEFT,
    HAND_RIGHT,
    FOOT_LEFT,
    FOOT_RIGHT
}

public class IKTargetsManager : MonoBehaviour
{
    public bool useAnimation = true;
    public VRTrackingManager vrTrackingManager = null;
    public AnimationManager animationManager = null;
    public Vector3 manualPositionOffset = new Vector3();

    void Start()
    {
    }

    void OnEnable()
    {
    }

    void OnDisable()
    {
    }

    void Update()
    {
    }

    public static string GetIKTargetBodyPartString(IK_TARGET ikTarget)
    {
        return ikTarget.ToString().ToLower();
    }

    public Vector3 GetIkTargetPosition(IK_TARGET ikTarget)
    {
        Transform ikTargetTransform = null;
        if (useAnimation)
        {
            ikTargetTransform = animationManager.GetEmulatedIKTargetTransform(ikTarget);
        }
        else
        {
            ikTargetTransform = vrTrackingManager.GetIKTargetTransform(ikTarget);
        }

        return ikTargetTransform.position + manualPositionOffset;
    }

    public Quaternion GetIkTargetRotation(IK_TARGET ikTarget)
    {
        Transform ikTargetTransform = null;
        if (useAnimation)
        {
            ikTargetTransform = animationManager.GetEmulatedIKTargetTransform(ikTarget);
        }
        else
        {
            ikTargetTransform = vrTrackingManager.GetIKTargetTransform(ikTarget);
        }

        return ikTargetTransform.rotation;
    }
}
