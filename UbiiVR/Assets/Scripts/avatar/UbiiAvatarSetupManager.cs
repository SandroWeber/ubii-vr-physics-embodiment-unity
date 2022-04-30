using UnityEngine;
using System.Threading.Tasks;

public class UbiiAvatarSetupManager : MonoBehaviour
{
    private UbiiNode ubiiNode = null;
    private bool ready = false;
    private Ubii.Devices.Component componentIkTargets = null;
    private Ubii.Devices.Component profileComponentIkTargets = null;
    private Ubii.Devices.Component componentAvatarPoses = null;
    private Ubii.Devices.Component profileComponentAvatarPoses = null;
    private Ubii.Devices.Component componentAvatarForceControl = null;
    private Ubii.Devices.Component profileComponentAvatarForceControl = null;
    private Ubii.Processing.ProcessingModule pmAvatarMotionControl = null;
    private Ubii.Processing.ProcessingModule profilePmAvatarMotionControl = null;

    private Ubii.Clients.Client clientPmAvatarMotionControls = null;

    private Ubii.Sessions.Session sessionAvatarControls = null;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("Initialize", 0.1f, 2f);
    }

    void OnEnable()
    {
        profileComponentIkTargets = new Ubii.Devices.Component
        {
            MessageFormat = "ubii.dataStructure.Object3DList",
            IoType = Ubii.Devices.Component.Types.IOType.Publisher
        };
        profileComponentIkTargets.Tags.Add("ik targets");

        profileComponentAvatarPoses = new Ubii.Devices.Component
        {
            MessageFormat = "ubii.dataStructure.Object3DList",
            IoType = Ubii.Devices.Component.Types.IOType.Publisher
        };
        profileComponentAvatarPoses.Tags.AddRange(new string[] { "avatar", "bones", "pose" });

        profileComponentAvatarForceControl = new Ubii.Devices.Component
        {
            MessageFormat = "ubii.dataStructure.Object3DList",
            IoType = Ubii.Devices.Component.Types.IOType.Subscriber
        };
        profileComponentAvatarForceControl.Tags.AddRange(new string[] { "avatar", "bones", "control", "velocity" });

        // this one could be defined more abstractly
        profilePmAvatarMotionControl = new Ubii.Processing.ProcessingModule
        {
            Name = "Unity Physical Avatar - Motion Controls PM"
        };

        ubiiNode = FindObjectOfType<UbiiNode>();
    }

    private async void Initialize()
    {
        if (this.componentIkTargets == null) await this.ScanForViableComponents(this.profileComponentIkTargets);
        if (this.componentAvatarPoses == null) await this.ScanForViableComponents(this.profileComponentAvatarPoses);
        if (this.componentAvatarForceControl == null) await this.ScanForViableComponents(this.profileComponentAvatarForceControl);
        if (this.clientPmAvatarMotionControls == null) await this.ScanForViableProcessingModule();

        if (this.componentIkTargets != null && this.componentAvatarPoses != null && this.componentAvatarForceControl != null && this.clientPmAvatarMotionControls != null)
        {
            Debug.Log("found all necessary components and PM!");
            //start session
            bool success = await this.StartSession();
            if (success) CancelInvoke("Initialize");
        }
    }

    private async Task<bool> ScanForViableComponents(Ubii.Devices.Component componentProfile)
    {
        bool success = false;

        Ubii.Devices.ComponentList requestComponentList = new Ubii.Devices.ComponentList();
        requestComponentList.Elements.Add(componentProfile);

        Ubii.Services.ServiceReply reply = await ubiiNode?.CallService(new Ubii.Services.ServiceRequest
        {
            Topic = UbiiConstants.Instance.DEFAULT_TOPICS.SERVICES.COMPONENT_GET_LIST,
            ComponentList = requestComponentList
        });

        if (reply.ComponentList != null)
        {
            if (reply.ComponentList.Elements.Count == 1)
            {
                if (componentProfile == this.profileComponentIkTargets)
                {
                    this.componentIkTargets = reply.ComponentList.Elements[0];
                    //Debug.Log("Found component for IK targets: " + this.componentIkTargets);
                    success = true;
                }
                else if (componentProfile == this.profileComponentAvatarPoses)
                {
                    this.componentAvatarPoses = reply.ComponentList.Elements[0];
                    //Debug.Log("Found component for avatar poses: " + this.componentAvatarPoses);
                    success = true;
                }
                else if (componentProfile == this.profileComponentAvatarForceControl)
                {
                    this.componentAvatarForceControl = reply.ComponentList.Elements[0];
                    //Debug.Log("Found component for avatar force control: " + this.componentAvatarForceControl);
                    success = true;
                }

            }
            else
            {
                Debug.LogWarning("Avatar Setup - multiple components matching profile for IK target component, need additional specification: " + reply.ComponentList.Elements);
            }
        }

        return success;
    }

    private async Task<bool> ScanForViableProcessingModule()
    {
        bool success = false;

        Ubii.Clients.Client profileClientRequest = new Ubii.Clients.Client();
        profileClientRequest.ProcessingModules.Add(this.profilePmAvatarMotionControl);

        Ubii.Services.ServiceRequest request = new Ubii.Services.ServiceRequest
        {
            Topic = UbiiConstants.Instance.DEFAULT_TOPICS.SERVICES.CLIENT_GET_LIST,
            ClientList = new Ubii.Clients.ClientList()
        };
        request.ClientList.Elements.Add(profileClientRequest);

        Ubii.Services.ServiceReply reply = await ubiiNode?.CallService(request);
        if (reply.ClientList != null)
        {
            if (reply.ClientList.Elements.Count == 1)
            {
                this.clientPmAvatarMotionControls = reply.ClientList.Elements[0];
                //Debug.Log("Found client for avatar force control PM: " + this.clientPmAvatarMotionControls);
            }
            else
            {
                Debug.LogWarning("Avatar Setup - multiple clients matching profile for Processing Module found, need additional specification: " + reply.ClientList.Elements);
            }
        }

        return success;
    }

    private async Task<bool> StartSession()
    {
        bool success = false;

        Ubii.Sessions.Session session = new Ubii.Sessions.Session
        {
            Name = "Unity Physical Avatar - Motion Control",
            Editable = false,
            Description = "Session managing motion control for a physics-based user avatar."
        };
        session.Tags.AddRange(new string[] { "avatar", "motion control", "ik", "inverse kinematics", "velocity" });

        session.ProcessingModules.Add(new Ubii.Processing.ProcessingModule
        {
            Name = this.profilePmAvatarMotionControl.Name,
            NodeId = this.clientPmAvatarMotionControls.Id
        });

        Ubii.Sessions.IOMapping ioMapping = new Ubii.Sessions.IOMapping
        {
            ProcessingModuleName = this.profilePmAvatarMotionControl.Name,
        };
        ioMapping.InputMappings.Add(new Ubii.Sessions.TopicInputMapping
        {
            InputName = "ikTargets",
            Topic = this.componentIkTargets.Topic
        });
        ioMapping.InputMappings.Add(new Ubii.Sessions.TopicInputMapping
        {
            InputName = "avatarCurrentPoses",
            Topic = this.componentAvatarPoses.Topic
        });
        ioMapping.OutputMappings.Add(new Ubii.Sessions.TopicOutputMapping
        {
            OutputName = "avatarTargetVelocities",
            Topic = this.componentAvatarForceControl.Topic
        });
        session.IoMappings.Add(ioMapping);

        Ubii.Services.ServiceReply reply = await ubiiNode?.CallService(new Ubii.Services.ServiceRequest {
            Topic = UbiiConstants.Instance.DEFAULT_TOPICS.SERVICES.SESSION_RUNTIME_START,
            Session = session
        });
        if (reply.Session != null)
        {
            this.sessionAvatarControls = reply.Session;
            Debug.Log("Session start service call success: " + this.sessionAvatarControls);
            success = true;
        }

        return success;
    }
}
