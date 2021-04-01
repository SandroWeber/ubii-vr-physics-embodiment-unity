using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RigidbodyAvatar : MonoBehaviour {

    public string HumanBonesPrefix;
    public string RigidbodyNamePrefix;
    public bool runInitialization = true;

    public bool isDynamic = false;

    public bool setRBOriginToArmature = true;
    public bool useGravity = true;

    Transform armature = null;

    UnityEvent AvatarReady;
    Animator myAnimator;

    IEnumerator SetListeners()
    {
        yield return new WaitForSeconds(0.2f);

        AvatarReady = new UnityEvent();
        //AvatarReady.AddListener(ExperimentManager.Instance.Recorder.PrepareRecorder);
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
        myAnimator = GetComponent<Animator>();
        if (myAnimator != null)
            myAnimator.enabled = false;
        StartCoroutine(SetListeners());
    }
	
	// Update is called once per frame
	void Update () {
        if(myAnimator != null)
        {
            if(myAnimator.GetCurrentAnimatorStateInfo(0).IsName("Finish"))
            {
#if UNITY_EDITOR
                // Application.Quit() does not work in the editor so
                // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
            }
        }
	}


    public void StartAnimator()
    {
        myAnimator.enabled = true;
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
                var potentialSurface = RigidbodyNamePrefix + "_Surface_" + dest;

                var surfaceObj = thisTransform.Find(potentialSurface);
                if(surfaceObj != null)
                {
                    SetupBone(surfaceObj.gameObject, child.gameObject);
                }
            }
        }
    }


    void SetupBone(GameObject _surface, GameObject _bone)
    {
        _surface.layer = 9; //Layer player
        Rigidbody boneBody = _bone.AddComponent<Rigidbody>();
        boneBody.isKinematic = !isDynamic;
        boneBody.useGravity = this.useGravity;
        //boneBody.angularDrag = 0;
        boneBody.maxAngularVelocity = 100f;
        if(setRBOriginToArmature)
        {
            if(boneBody.gameObject.name == _bone.gameObject.name)
            boneBody.centerOfMass = _bone.transform.localPosition;
            else
            {
                Debug.LogError("names not allign " + boneBody.gameObject.name + boneBody.gameObject.name);
            }
        }
        RigidbodyConstraints rotateOnly = RigidbodyConstraints.FreezePosition ; 
        //jointBody.constraints = rotateOnly;
        if(_surface != null)
        {
            _surface.transform.parent = _bone.transform;
            MeshCollider meshCollider = _surface.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = _surface.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            meshCollider.isTrigger = false;
            if(isDynamic)
            {
                //mc.inflateMesh = true;
                meshCollider.convex = true;
            }
        }
    }

}
