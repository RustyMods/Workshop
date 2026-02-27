using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public static class TerrainModifiers
{
    public static bool TryFindTerrain(Vector3 point, float range, out List<PlanTerrain> terrainComps)
    {
        terrainComps = new List<PlanTerrain>();
        List<TerrainModifier> modifiers = new();
        TerrainModifier.GetModifiers(point, range, modifiers);
        foreach (TerrainModifier modifier in modifiers)
        {
            PlanTerrain plan = new PlanTerrain
            {
                PaintType = modifier.m_paintType,
                Position = modifier.transform.position,
                Radius = modifier.m_levelRadius,
                SmoothRadius = modifier.m_smoothRadius,
                Shape = modifier.m_square ? "square" : "circle",
                Level = modifier.m_level,
            };
            terrainComps.Add(plan);
        }
        
        return terrainComps.Count > 0;
    }
}