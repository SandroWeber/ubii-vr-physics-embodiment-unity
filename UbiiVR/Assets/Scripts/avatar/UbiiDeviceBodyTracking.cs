using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UbiiDeviceBodyTracking : MonoBehaviour
{
    static string NAME = "Unity Physical Avatar - Body Tracking";
    static string DESCRIPTION = "All components responsible for tracking user body.";
    static string[] TAGS = new string[] { "body tracking" };
    private UbiiComponentIkTargets componentIkTargets = null;
    private Ubii.Devices.Device ubiiSpecs = null;

    private UbiiNode ubiiNode = null;

    void OnEnable()
    {
        this.componentIkTargets = FindObjectOfType<UbiiComponentIkTargets>();
        this.ubiiNode = FindObjectOfType<UbiiNode>();
        UbiiNode.OnInitialized += OnUbiiNodeInitialized;
    }

    public async void OnUbiiNodeInitialized()
    {
        this.componentIkTargets.OnUbiiNodeInitialized();

        this.ubiiSpecs = new Ubii.Devices.Device {
            Name = NAME,
            Description = DESCRIPTION,
            ClientId = this.ubiiNode.Id
        };
        this.ubiiSpecs.Tags.AddRange(TAGS);
        this.ubiiSpecs.Components.Add(this.componentIkTargets.UbiiSpecs);

        Ubii.Services.ServiceReply reply = await this.ubiiNode.CallService(new Ubii.Services.ServiceRequest {
            Topic = UbiiConstants.Instance.DEFAULT_TOPICS.SERVICES.DEVICE_REGISTRATION,
            Device = this.ubiiSpecs
        });
        if (reply.Device != null)
        {
            foreach(Ubii.Devices.Component component in reply.Device.Components)
            {
                if (component.Topic == this.componentIkTargets.UbiiSpecs.Topic)
                {
                    this.componentIkTargets.UbiiSpecs = component;
                }
            }
        }
        else if (reply.Error != null)
        {
            Debug.LogError("UBII UbiiDeviceBodyTracking registration error:\n" + reply.Error);
        }
    }
}
