using System;
using UnityEngine;

namespace Workshop;

public class PlanTerrain
{
    private static PlanTerrain _defaultPlanTerrain = new ("circle;0;0;0;10;0;5;dirt");

    public string Shape;
    public Vector3 Position;
    public float Radius;
    public int Rotation;
    public float SmoothRadius;
    public bool Level;
    public TerrainModifier.PaintType PaintType;

    public PlanTerrain(){}

    public PlanTerrain(string line)
    {
        string[] parts = line.Split(';');
        Shape = parts.GetString(0);
        Position = parts.GetVector(1);
        Radius = parts.GetFloat(4);
        Rotation = parts.GetInt(5);
        SmoothRadius = parts.GetFloat(6);
        PaintType = parts.GetEnum(7, TerrainModifier.PaintType.Dirt);
        Level = parts.GetBool(8, true);
    }

    public GameObject Create(Transform parent, int index)
    {
        GameObject instance = new GameObject(BuildTools.TerrainFlag.name);
        instance.transform.SetParent(parent);
        instance.transform.localPosition = Position;
        instance.transform.localRotation = Quaternion.identity;
        Plan plan = instance.AddComponent<Plan>();
        plan.m_radius = Radius;
        plan.m_smoothRadius = SmoothRadius;
        plan.m_type = PaintType;
        plan.m_isSquare = Shape.Equals("square", StringComparison.InvariantCultureIgnoreCase);
        return instance;
    }
        
    public override string ToString() => $"{Shape};{Position.ToCustomString(';')};{Radius};{SmoothRadius};{PaintType};{Level}";
}