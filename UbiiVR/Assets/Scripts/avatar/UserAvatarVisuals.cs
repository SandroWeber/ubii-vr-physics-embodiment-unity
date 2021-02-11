﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserAvatarVisuals : MonoBehaviour {

    public float opacity = 0.5f;

	// Use this for initialization
	void Start () {
        this.SetOpacity(this.opacity);
	}
	
	// Update is called once per frame
	void Update () {
        // Bachelors Thesis VRHand
        if (Input.GetKey("m"))
        {
            //disableRenderer();
            this.SetOpacity(0f);
        }
        if (Input.GetKey("n"))
        {
            //enableRenderer();
            this.SetOpacity(0.5f);
        }
    }

    public void SetOpacity(float opacity)
    {
        this.opacity = opacity;

        Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
			if(renderer.gameObject.GetComponent<ParticleSystem>() != null)
				continue;
			foreach (Material material in renderer.materials)
            {
                Color color = material.color;
                color.a = opacity;
                material.SetColor("_Color", color);
                this.SetRenderModeTransparent(material);
            }
        }
    }

    // Bachelors Thesis VRHand
    public void disableRenderer()
    {
        Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    public void enableRenderer()
    {
        Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }
    }

    private void SetRenderModeTransparent (Material material)
    {
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
}
