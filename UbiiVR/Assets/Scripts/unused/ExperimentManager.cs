using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentManager : MonoBehaviour {

    private static ExperimentManager instance = null;

    public static ExperimentManager Instance
    {
        get {            
            return instance;
        }
    }

    RigidbodyAvatar avatar;
    RigidbodyRecorder recorder;
    RigidBodyMoveAlong moveAlong;

    public RigidbodyAvatar Avatar
    {
        get { return avatar; }
    }

    public RigidbodyRecorder Recorder
    {
        get { return recorder; }
    }

    public RigidBodyMoveAlong MoveAlong
    {
        get { return moveAlong; }
    }

    // Use this for initialization
    void Start () {
        instance = FindObjectOfType<ExperimentManager>();
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }

        recorder = FindObjectOfType<RigidbodyRecorder>();
        RigidbodyAvatar[] avatars = FindObjectsOfType<RigidbodyAvatar>();
        foreach(RigidbodyAvatar av in avatars)
        {
            if(av.name.Contains("GroundTruth"))
            {
                avatar = av;
            }
        }
        moveAlong = FindObjectOfType<RigidBodyMoveAlong>();
	}
	
	// Update is called once per frame
	void Update () {
		


	}
}
