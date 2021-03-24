using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PseudoTopicData : Singleton<PseudoTopicData>
{
    private Dictionary<string, Vector3> dataVector3 = new Dictionary<string, Vector3>();
    private Dictionary<string, Quaternion> dataQuaternion = new Dictionary<string, Quaternion>();

    public Vector3 GetVector3(string topic)
    {
        return dataVector3[topic];
    }

    public void SetVector3(string topic, Vector3 vec)
    {
        if (dataVector3.ContainsKey(topic))
        {
            dataVector3[topic] = vec;
        }
        else
        {
            dataVector3.Add(topic, vec);
        }
    }
    public Quaternion GetQuaternion(string topic)
    {
        return dataQuaternion[topic];
    }

    public void SetQuaternion(string topic, Quaternion quat)
    {
        if (dataQuaternion.ContainsKey(topic))
        {
            dataQuaternion[topic] = quat;
        }
        else
        {
            dataQuaternion.Add(topic, quat);
        }
    }
}
