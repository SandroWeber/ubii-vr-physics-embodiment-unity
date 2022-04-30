using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UbiiDevicePhysicsAvatar : MonoBehaviour
{
    static string NAME = "Unity Physical Avatar - Physics Avatar";
    static string DESCRIPTION = "Avatar integrated into physics engine. Publishing body part poses and subscribing to velocity control commands.";
    static string[] TAGS = new string[] { "physics", "avatar", "embodiment" };
    private UbiiComponentAvatarCurrentPose componentAvatarCurrentPose = null;
    private UbiiComponentAvatarForceControl componentAvatarForceControl = null;
    private Ubii.Devices.Device ubiiSpecs = null;

    private UbiiNode ubiiNode = null;

    void OnEnable()
    {
        this.componentAvatarCurrentPose = FindObjectOfType<UbiiComponentAvatarCurrentPose>();
        this.componentAvatarForceControl = FindObjectOfType<UbiiComponentAvatarForceControl>();
        this.ubiiNode = FindObjectOfType<UbiiNode>();
        UbiiNode.OnInitialized += OnUbiiNodeInitialized;
    }

    public async void OnUbiiNodeInitialized()
    {
        this.componentAvatarCurrentPose.OnUbiiNodeInitialized();
        this.componentAvatarForceControl.OnUbiiNodeInitialized();

        this.ubiiSpecs = new Ubii.Devices.Device {
            Name = NAME,
            Description = DESCRIPTION,
            ClientId = this.ubiiNode.Id
        };
        this.ubiiSpecs.Tags.AddRange(TAGS);
        this.ubiiSpecs.Components.Add(this.componentAvatarCurrentPose.UbiiSpecs);
        this.ubiiSpecs.Components.Add(this.componentAvatarForceControl.UbiiSpecs);

        Ubii.Services.ServiceReply reply = await this.ubiiNode.CallService(new Ubii.Services.ServiceRequest {
            Topic = UbiiConstants.Instance.DEFAULT_TOPICS.SERVICES.DEVICE_REGISTRATION,
            Device = this.ubiiSpecs
        });
        if (reply.Device != null)
        {
            foreach(Ubii.Devices.Component component in reply.Device.Components)
            {
                if (component.Topic == this.componentAvatarCurrentPose.UbiiSpecs.Topic)
                {
                    this.componentAvatarCurrentPose.UbiiSpecs = component;
                }
                else if (component.Topic == this.componentAvatarForceControl.UbiiSpecs.Topic)
                {
                    this.componentAvatarForceControl.UbiiSpecs = component;
                }
            }
        }
        else if (reply.Error != null)
        {
            Debug.LogError("UBII UbiiDevicePhysicsAvatar registration error:\n" + reply.Error);
        }
    }
}
