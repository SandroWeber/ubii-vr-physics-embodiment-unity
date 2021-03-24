using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserAvatarForceControl : MonoBehaviour
{
    public static void AddForce(Rigidbody rigidbody, Vector3 additiveForce, bool reset = false)
    {
        if (reset)
        {
            rigidbody.velocity = Vector3.zero;
        }
        rigidbody.AddForce(additiveForce, ForceMode.Acceleration);
    }

    public static void AddForceFromTargetLinearVelocity(Rigidbody rigidbody, Vector3 targetVelocity, bool reset = false)
    {
        Vector3 newVelocity = targetVelocity - rigidbody.velocity;
        UserAvatarForceControl.AddForce(rigidbody, newVelocity / Time.deltaTime, reset);
    }

    public static void AddTorque(Rigidbody rigidbody, Vector3 additiveTorque, bool reset = false)
    {
        if(reset)
        {
            rigidbody.angularVelocity = Vector3.zero;
        }
        rigidbody.AddTorque(additiveTorque, ForceMode.Acceleration);
    }

    public static void AddTorqueFromTargetAngularVelocity(Rigidbody rigidbody, Vector3 targetVelocity, bool reset = false)
    {
        Vector3 newAngularVelocity = targetVelocity - rigidbody.angularVelocity;
        UserAvatarForceControl.AddTorque(rigidbody, newAngularVelocity / Time.deltaTime, reset);
    }
}
