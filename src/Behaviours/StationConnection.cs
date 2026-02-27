using System;
using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public class StationConnection : MonoBehaviour
{
    private static readonly List<StationConnection> instances = new List<StationConnection>();
    public ParticleSystem system;
    public ParticleSystem glow;
    public ParticleSystem poff;
    public ParticleSystem backward;
    
    public void Awake()
    {
        instances.Add(this);
        if (!ConfigManager.ConnectionEnabled)
        {
            Stop();
        }
    }

    public void Start()
    {
        glow.gameObject.SetActive(true);
        SetColor(ConfigManager.ConnectionMinColor, ConfigManager.ConnectionMaxColor);
    }

    public void OnDestroy()
    {
        instances.Remove(this);
    }

    public void SetColor(Color min, Color max)
    {
        ParticleSystem.MainModule main = system.main;
        main.startColor = new ParticleSystem.MinMaxGradient
        {
            mode = ParticleSystemGradientMode.TwoColors,
            colorMin = min,
            colorMax = max
        };
    }

    public void Stop()
    {
        system.Stop();
        glow.Stop();
        poff.Stop();
        backward.Stop();
    }

    public void Play()
    {
        system.Play();
        glow.Play();
        poff.Play();
        backward.Play();
    }

    private static void HideAll()
    {
        for (int i = 0; i < instances.Count; i++)
        {
            StationConnection instance = instances[i];
            instance.Stop();
        }
    }

    private static void ShowAll()
    {
        for (int i = 0; i < instances.Count; i++)
        {
            StationConnection instance = instances[i];
            instance.Play();
        }
    }

    public static void OnToggle(object sender, EventArgs args)
    {
        if (ConfigManager.ConnectionEnabled)
        {
            ShowAll();
        }
        else
        {
            HideAll();
        }
    }

    public static void OnColorChange(object sender, EventArgs args)
    {
        for (int i = 0; i < instances.Count; i++)
        {
            var instance = instances[i];
            instance.SetColor(ConfigManager.ConnectionMinColor, ConfigManager.ConnectionMaxColor);
        }
    }
}