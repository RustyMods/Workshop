using System;
using UnityEngine;

namespace Workshop;

public class PlanSnapPoint
{
    private const string DefaultName = "$hud_snappoint_edge";
    private readonly Vector3 Position;
    private readonly string Name;

    public PlanSnapPoint(string line)
    {
        string[] parts = line.Split(';');
        Position = parts.GetVector(0);
        Name = parts.GetString(3, DefaultName);
    }

    public GameObject Create(Transform parent, int index)
    {
        string name = Name.Equals(DefaultName) ? Name + $" {index}" : Name;
        GameObject instance = new GameObject(name);
        instance.transform.SetParent(parent);
        instance.transform.localPosition = Position;
        instance.tag = "snappoint";
        instance.layer = 10;
        instance.SetActive(false);
        return instance;
    }

    public override string ToString() => Position.ToCustomString(';');
}