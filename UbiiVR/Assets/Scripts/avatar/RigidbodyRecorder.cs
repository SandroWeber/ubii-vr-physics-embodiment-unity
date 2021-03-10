using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RigidbodyRecorder : MonoBehaviour {

    struct DatasetKey
    {
        public float timestep;
        public string gameObjectName;

        public DatasetKey(float time, string name)
        {
            this.timestep = time;
            this.gameObjectName = name;
        }

    }

    public string fileDestinationX = "";
    public string fileDestinationY = "";

    public bool doRecord = false;
    public bool recordVelocities = false;

    string animationName = "";

    Rigidbody[] rigidbodies;
    string[] rigidbodiesOrder;
    string labelsDomain = ""; //X
    string labelsCodomain = ""; //Y

    Dictionary<DatasetKey, List<float>> domainMap;
    Dictionary<DatasetKey, List<float>> codomainMap;
    List<float> timesteps;

    Transform thisTransform;
    public UnityAction PrepareRecorder;

	// Use this for initialization
	void Start () {

        if(fileDestinationX == string.Empty || fileDestinationY == string.Empty)
        {
            Debug.LogWarning("Recorder has no filename and wont record into a file!");
        }

        domainMap = new Dictionary<DatasetKey, List<float>>();
        codomainMap = new Dictionary<DatasetKey, List<float>>();
        timesteps = new List<float>();

        thisTransform = this.transform;
        PrepareRecorder += GatherRigidbodiesAndLabels;
	}


    public void GatherRigidbodiesAndLabels()
    {
        rigidbodies = thisTransform.GetComponentsInChildren<Rigidbody>();
        rigidbodiesOrder = new string[rigidbodies.Length];

        string formatDomain = "";

        //Domain X
        labelsDomain = "Timestep";
        formatDomain += "{0}";
        for (int idx = 0; idx < rigidbodies.Length; idx += 1)
        {
            labelsDomain += ",";
            labelsDomain += rigidbodies[idx].gameObject.name + "ErrorPos_x,";
            labelsDomain += rigidbodies[idx].gameObject.name + "ErrorPos_y,";
            labelsDomain += rigidbodies[idx].gameObject.name + "ErrorPos_z,";

            labelsDomain += rigidbodies[idx].gameObject.name + "ErrorAng_x,";
            labelsDomain += rigidbodies[idx].gameObject.name + "ErrorAng_y,";
            labelsDomain += rigidbodies[idx].gameObject.name + "ErrorAng_z";

            if(recordVelocities)
            {
                labelsDomain += ",";
                labelsDomain += rigidbodies[idx].gameObject.name + "velocity_x,";
                labelsDomain += rigidbodies[idx].gameObject.name + "velocity_y,";
                labelsDomain += rigidbodies[idx].gameObject.name + "velocity_z,";

                labelsDomain += rigidbodies[idx].gameObject.name + "angularVelo_x,";
                labelsDomain += rigidbodies[idx].gameObject.name + "angularVelo_y,";
                labelsDomain += rigidbodies[idx].gameObject.name + "angularVelo_z";

            }

            rigidbodiesOrder[idx] = rigidbodies[idx].gameObject.name;
        }
        formatDomain += "\n";

        string formatCodomain = "";

        //Codomain Y
        labelsCodomain = "Timestep, ";
        formatCodomain += "{0}";
        for (int idx = 0; idx < rigidbodies.Length; idx += 1)
        {
            labelsCodomain += rigidbodies[idx].gameObject.name + "Force_x,";
            labelsCodomain += rigidbodies[idx].gameObject.name + "Force_y,";
            labelsCodomain += rigidbodies[idx].gameObject.name + "Force_z,";

            labelsCodomain += rigidbodies[idx].gameObject.name + "Torque_x,";
            labelsCodomain += rigidbodies[idx].gameObject.name + "Torque_y,";
            labelsCodomain += rigidbodies[idx].gameObject.name + "Torque_z,";
        }
        formatCodomain += "\n";
    }

    public void OnApplicationQuit()
    {
        if(doRecord)
            SerialzeToCSVFile();
    }

    public void AddTimeStep(float t) {
        if(timesteps.Count == 0 || timesteps[timesteps.Count -1] != t)
            timesteps.Add(t);
    }

    public void RecordDomain(float time, Vector3 errorPos, Vector3 errorAngle, Vector3 velocity, Vector3 angularVelo, string objname)
    {
        DatasetKey key = new DatasetKey(time, objname);
        if(domainMap.ContainsKey(key))
        {
            Debug.LogError("Error! set contains key already, missing one insert");
        }
        
        if(!recordVelocities)
        {
            domainMap.Add(key, new List<float> { errorPos.x, errorPos.y, errorPos.z, errorAngle.x, errorAngle.y, errorAngle.z });
        }
        else
        {
            domainMap.Add(key, new List<float> { errorPos.x, errorPos.y, errorPos.z, errorAngle.x, errorAngle.y, errorAngle.z, velocity.x, velocity.y, velocity.z, angularVelo.x, angularVelo.y, angularVelo.z });
        }
    }

    public void RecordCodomain(float time, Vector3 appliedForce, Vector3 appliedTorque, string objname)
    {
        DatasetKey key = new DatasetKey(time, objname);
        codomainMap.Add(key, new List<float> { appliedForce.x, appliedForce.y, appliedForce.z, appliedTorque.x, appliedTorque.y, appliedTorque.z });
    }

    public void SerialzeToCSVFile()
    {
        var csvX = new StringBuilder();
        var csvY = new StringBuilder();
        csvX.AppendLine(labelsDomain);
        csvY.AppendLine(labelsCodomain);

        foreach (float t in timesteps)
        {
            List<float> valuesX = getDomainByTime(t);
            List<float> valuesY = getCodomainByTime(t);
            csvX.AppendFormat("{0}", t);
            csvY.AppendFormat("{0}", t);

            foreach (float f in valuesX)
            {
                string format = ",{0}";
                csvX.AppendFormat(format, f.ToString());
            }

            foreach (float f in valuesY)
            {
                string format = ",{0}";
                csvY.AppendFormat(format, f.ToString());
            }

            csvX.Append("\n");
            csvY.Append("\n");
        }

        File.WriteAllText(fileDestinationX, csvX.ToString());
        File.WriteAllText(fileDestinationY, csvY.ToString());
        
    }

    List<string> testOrderConsistency(float time)
    {
        List<string> data = new List<string>();
        foreach(string name in rigidbodiesOrder)
        {
            DatasetKey key = new DatasetKey(time, name);
            data.Add(name);
            data.Add(name);
            data.Add(name);

            data.Add(name);
            data.Add(name);
            data.Add(name);
        }
        return data;
    }

    List<float> getDomainByTime(float time)
    {
        List<float> data = new List<float>();
        foreach (string name in rigidbodiesOrder)
        {
            DatasetKey key = new DatasetKey(time, name);
            List<float> values = new List<float>();
            if(domainMap.TryGetValue(key,out values))
            {
                data.AddRange(values);
            }
            else
            {
                Debug.LogError("key not found: " + key.timestep + key.gameObjectName);
            }
            
            
        }       
        return data;
    }

    List<float> getCodomainByTime(float time)
    {
        List<float> data = new List<float>();
        foreach (string name in rigidbodiesOrder)
        {
            DatasetKey key = new DatasetKey(time, name);
            List<float> values = new List<float>();
            if (codomainMap.TryGetValue(key, out values))
            {
                data.AddRange(values);
            }
            else
            {
                Debug.LogError("key not found: " + key.timestep + key.gameObjectName);
            }
        }
        return data;
    }
}
