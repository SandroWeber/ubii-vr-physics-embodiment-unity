using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RigidBodyMoveAlong : MonoBehaviour {

    public bool useTensorflow = true;
    public bool sendVelocity = true;
    public int requiredTimeSteps = 1;
    Queue<List<float>> sequenceData;
    //TensorflowUbiiCommunication tensorflowUbiiComm;

    public bool usePredictionFile = false;
    public string predictionsFilename = "";
    StreamReader predictions;

    RigidbodyAvatar toFollowAvatar;
    Transform followTransform;
    Transform thisTransform;

    float lasttime = 0.0f;
    float lastTFProcessed = -1.0f;

    public bool resetVelocityCalculation = true;

    Dictionary<Transform, Transform> armatureMap;
    public UnityAction PrepareFollow;

    RigidbodyRecorder recorder;

    public string testName = "";
    bool doTest = false;
    Vector3 targetPos = Vector3.zero;
    Vector3 targetRot = Vector3.zero;

	// Use this for initialization
	void Start () {
        armatureMap = new Dictionary<Transform, Transform>();
        PrepareFollow += MapArmature;
        PrepareFollow += FetchRecorder;
        thisTransform = this.transform;
        //tensorflowUbiiComm = FindObjectOfType<TensorflowUbiiCommunication>();
        sequenceData = new Queue<List<float>>(requiredTimeSteps);

        if(usePredictionFile && predictionsFilename.EndsWith("csv"))
        {
            predictions = File.OpenText(predictionsFilename);
        }
    }

    private void OnApplicationQuit()
    {
        if(predictions != null)
            predictions.Close();
    }

    public void FetchRecorder()
    {
        recorder = ExperimentManager.Instance.Recorder;
    }

    public void MapArmature ()
    {
        toFollowAvatar = ExperimentManager.Instance.Avatar;
        Transform followAmature = toFollowAvatar.Armature;
        followTransform = followAmature;
        if(toFollowAvatar != null && followAmature != null)
        {
            Transform[] allChildrenTargets = followAmature.GetComponentsInChildren<Transform>();
            Transform[] allChildren = thisTransform.GetComponentsInChildren<Transform>();
            foreach (Transform bone in allChildren)
            {
                if (bone.GetComponent<Rigidbody>() != null)
                {
                    foreach (Transform targetBone in allChildrenTargets)
                    {
                        if(targetBone.name == bone.name && !armatureMap.ContainsKey(targetBone))
                        {
                            armatureMap.Add(bone, targetBone);
                            break;
                        }
                    }
                }
            }
        }
    }
	
	// Update is called once per frame
	void FixedUpdate () {

        if(Input.GetKey(KeyCode.T))
        {
            doTest = true;
        }

		if(armatureMap.Count > 0)
        {
            
            float time = Time.time;
            lasttime = time;
            recorder.AddTimeStep(time);

            if (usePredictionFile)
            {
                ProcessPredictionsFileData();
            }

            else if(!useTensorflow)
            {
                foreach (KeyValuePair<Transform, Transform> entry in armatureMap)
                {
                    actuateRigidbody(entry, time, Time.deltaTime);
                }
            }
            else
            {
                SendTFData();
                ProcessTFData();
            }
        }

       
        
	}

    void actuateRigidbody(KeyValuePair<Transform, Transform> entry, float time, float deltaTime)
    {
        Rigidbody source = entry.Key.GetComponent<Rigidbody>();
        Transform sourceB = entry.Key;
        Transform target = entry.Value;

        Vector3 positionDelta = RigidbodyErrorPos(source.transform, target);
        Vector3 angularDelta = RigidbodyErrorAngles(source.transform, target);
       
        recorder.RecordDomain(time, positionDelta, angularDelta, source.velocity, source.angularVelocity, entry.Key.gameObject.name);

        //source.MovePosition(entry.Key.position + positionDelta);
        //source.MoveRotation(target.rotation);

        

        // angularDelta = OrientTorque(angularDelta);

        
       
        //Vector3 angu = (angularDelta * Mathf.Deg2Rad);
        //angu /= deltaTime;


        //Vector3 newVelocity = 0.5f * (velo - prefVelo);
        //Vector3 newAngularAcce = 0.5f * (angu - prefAngl) / deltaTime;


        //Vector3 newAccleration = 0.5f * positionDelta / deltaTime / deltaTime;
        //Vector3 newAngularAcce = 0.5f * angularDelta / deltaTime / deltaTime * Mathf.Deg2Rad ;

        // FORCE MODES:
        // IMPULSE mass* distance/ time.
        // FORCE mass* distance/ time^2.

        Vector3 velo = positionDelta / Time.deltaTime;
        ApplyForce(velo, source, resetVelocityCalculation);
        //source.AddTorque(newAngularAcce * source.mass);

        Quaternion currentRotation = source.rotation;
        Quaternion rotationDir = target.rotation * Quaternion.Inverse(currentRotation);


        float angleInDegrees;
        Vector3 rotationAxis;
        rotationDir.ToAngleAxis(out angleInDegrees, out rotationAxis);
        rotationAxis.Normalize();

        Vector3 angularDifference = rotationAxis * angleInDegrees * Mathf.Deg2Rad;
        Vector3 angularSpeed = angularDifference / Time.deltaTime;

        if(resetVelocityCalculation)
        {
            angularSpeed = new Vector3(rotationDir.x, rotationDir.y, rotationDir.z);
            
        }
        ApplyTorque(angularSpeed, source, resetVelocityCalculation);

        //

        /* Vector3 x = Vector3.Cross(oldDir.normalized, newDir.normalized);
         float theta = Mathf.Asin(x.magnitude);
         Vector3 w = x.normalized * theta / Time.fixedDeltaTime;

         Quaternion q = transform.rotation * rigidbody.inertiaTensorRotation;
         Vector3 T = q * Vector3.Scale(rigidbody.inertiaTensor, (Quaternion.Inverse(q) * w));

         rigidbody.AddTorque(T, ForceMode.Impulse) */

        recorder.RecordCodomain(time, velo, angularSpeed, entry.Key.gameObject.name);

    }

    public static Vector3 RigidbodyErrorPos(Transform bone, Transform target)
    {
        return target.position - bone.position;
    }

    public static Vector3 RigidbodyErrorAngles(Transform bone, Transform target)
    {
        Vector3 a = bone.rotation.eulerAngles;
        Vector3 b = target.rotation.eulerAngles;
        return new Vector3(Mathf.DeltaAngle(a.x, b.x), Mathf.DeltaAngle(a.y, b.y), Mathf.DeltaAngle(a.z, b.z));
    }

    public static void ApplyForce(Vector3 velo, Rigidbody actuated, bool reset)
    {
        if(reset)
        {
            actuated.velocity = Vector3.zero;
        }
        Vector3 prefVelo = actuated.velocity;
        Vector3 newVelocity = (velo  - prefVelo);
        actuated.AddForce(newVelocity / Time.deltaTime, ForceMode.Acceleration);
    }

    public static void ApplyTorque(Vector3 anguV, Rigidbody actuator, bool reset)
    {
        if(reset)
        {
            actuator.angularVelocity = Vector3.zero;
        }
        Vector3 anguVelo = anguV - actuator.angularVelocity;
        /*Vector3 prefAnguLocal = actuator.gameObject.transform.InverseTransformDirection(anguVelo);
        Vector3 TorqueLocal;
        Vector3 prefAnguTemp = prefAnguLocal;
        prefAnguTemp = actuator.inertiaTensorRotation * prefAnguTemp;
        prefAnguTemp.Scale(actuator.inertiaTensor);
        TorqueLocal = Quaternion.Inverse(actuator.inertiaTensorRotation) * prefAnguTemp;

        Vector3 Torque = actuator.gameObject.transform.TransformDirection(TorqueLocal);
        */
        if (reset)
            actuator.AddTorque(anguVelo / Time.deltaTime, ForceMode.Force);
        else
        {
            //actuator.angularVelocity = Vector3.zero;
            actuator.AddTorque(anguVelo  / Time.deltaTime, ForceMode.Acceleration);
        }

       
    }

    public void SendTFData()
    {
        List<float> timestepData = new List<float>();
        timestepData.Add(Time.time);
        foreach (KeyValuePair<Transform, Transform> pair in armatureMap)
        {
            Vector3 errorPos = RigidbodyErrorPos(pair.Key, pair.Value);
            timestepData.Add(errorPos.x);
            timestepData.Add(errorPos.y);
            timestepData.Add(errorPos.z);
            Vector3 errorAng = RigidbodyErrorAngles(pair.Key, pair.Value);
            timestepData.Add(errorAng.x);
            timestepData.Add(errorAng.y);
            timestepData.Add(errorAng.z);
            if(sendVelocity)
            {
                Vector3 v = pair.Key.GetComponent<Rigidbody>().velocity;
                timestepData.Add(v.x);
                timestepData.Add(v.y);
                timestepData.Add(v.z);
                Vector3 av = pair.Key.GetComponent<Rigidbody>().angularVelocity;
                timestepData.Add(av.x);
                timestepData.Add(av.y);
                timestepData.Add(av.z);
            }
            
        }
        sequenceData.Enqueue(timestepData);
        /*if(tensorflowUbiiComm != null && sequenceData.Count == requiredTimeSteps)
        {
            List<float> sendData = new List<float>();
            foreach (List<float> list in sequenceData)
            {
                sendData.AddRange(list);
            }
            tensorflowUbiiComm.SetPublishData(sendData, Time.time);
            sequenceData.Dequeue();
        }*/
    }

    public void ProcessPredictionsFileData()
    {
        List<float> predictData = new List<float>();
        predictData.Add(Time.time);
        if(predictions != null)
        {
            var line = predictions.ReadLine();
            if (line.Length == 0)
                return;
            foreach (string value in line.Split(','))
            {
                var f = float.Parse(value);
                predictData.Add(f);
            }
        }
        if(predictData.Count > 0)
            ProcessData(predictData);
    }

    public void ProcessTFData()
    {
        /*if (tensorflowUbiiComm != null)
        {
            List<float> receiveData = tensorflowUbiiComm.GetSubscribeData();
            ProcessData(receiveData);
        }*/
    }

    void ProcessData(List<float> receiveData)
    {
        toFollowAvatar.StartAnimator();
        if (receiveData.Count == 0)
            return;
        float tfTime = receiveData[0];
        int idx = 1;
        foreach (KeyValuePair<Transform, Transform> pair in armatureMap)
        {
            if (idx + 5 > receiveData.Count)
            {
                Debug.LogError("Received data from tensorflow has not enough values");
                return;
            }
            if (tfTime <= lastTFProcessed)
            {
                Debug.LogError("Prcessing old values, abort ");
                return;
            }
            if(pair.Key.name != pair.Value.name)
            {
                Debug.LogError("Wrong key value pair in armaturemap, abort ");
                return;
            }
            Vector3 velo = new Vector3 { x = receiveData[idx], y = receiveData[idx + 1], z = receiveData[idx + 2] };
            ApplyForce(velo, pair.Key.GetComponent<Rigidbody>(),resetVelocityCalculation);
            idx += 3;
            Vector3 anVe = new Vector3 { x = receiveData[idx], y = receiveData[idx + 1], z = receiveData[idx + 2] };
            ApplyTorque(anVe, pair.Key.GetComponent<Rigidbody>(), resetVelocityCalculation);
            idx += 3;
        }
        lastTFProcessed = tfTime;
    }

    public Dictionary<Transform, Transform> getArmatureMap()
    {
        return armatureMap;
    }

    private Vector3 OrientTorque(Vector3 torque)
    {
        // Quaternion's Euler conversion results in (0-360)
        // For torque, we need -180 to 180.

        return new Vector3
        (
            torque.x > 180f ? 180f - torque.x : torque.x,
            torque.y > 180f ? 180f - torque.y : torque.y,
            torque.z > 180f ? 180f - torque.z : torque.z
        );
    }

}
