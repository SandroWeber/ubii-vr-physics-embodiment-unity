using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RigidbodyAvatarNoHierachy : MonoBehaviour {

    public string HumanBonesPrefix;
    public string RigidbodynamePrefix;
    public bool runInitialization = true;

    public bool isDynamic = false;

    Transform armature = null;

    UnityEvent AvatarReady;

    IEnumerator SetListeners()
    {
        yield return new WaitForSeconds(1);

        AvatarReady = new UnityEvent();
        //AvatarReady.AddListener(ExperimentManager.Instance.Recorder.FindRigidbodies);
        //AvatarReady.AddListener(ExperimentManager.Instance.MoveAlong.PrepareFollow);

        if (runInitialization)
        {
            SetupRigidbodyStructureAndComponents();
            AvatarReady.Invoke();
        }
    }

    public Transform Armature
    {
        get { return armature;  }
    }

    // Use this for initialization
    void Start () {
        StartCoroutine(SetListeners());
    }
	
	// Update is called once per frame
	void Update () {
		
	}



    void SetupRigidbodyStructureAndComponents ()
    {
        var thisTransform = this.transform;
        armature =  null;
        foreach ( Transform child in this.transform)
        {
            if(child.name == "Armature")
            {
                armature = child;
            }
        }

        if(armature == null)
        {
            Debug.LogError("Armature is missing, Rigidbody Avatar misses a human rig, or rig is not named 'Armature'");
        }

        //Iterate through Joints and Surfaces and parent them and apply components
        Transform[] allChildren = armature.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            var template = child.name;
            if(!template.Contains(HumanBonesPrefix))
            {
                continue;
            }
            else
            {
                var dest = template.Split('_')[1];
                var potentialSurface = RigidbodynamePrefix + "_Surface_" + dest;

                var surfaceObj = thisTransform.Find(potentialSurface);
                if(surfaceObj != null)
                {
                    SetupJoint(surfaceObj.gameObject, child.gameObject);
                }
            }
        }
    }


    void SetupJoint(GameObject _surface, GameObject _bone)
    {
        _surface.layer = 9; //Layer player
        Rigidbody jointBody = _bone.AddComponent<Rigidbody>();
        jointBody.isKinematic = !isDynamic;
        jointBody.useGravity = false;
        RigidbodyConstraints rotateOnly = RigidbodyConstraints.FreezePosition ;
        //jointBody.constraints = rotateOnly;
    }

}
