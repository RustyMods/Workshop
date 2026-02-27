using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Workshop;

public static class HeightmapController
{
    public static bool Encapsulates(this Heightmap hm, BiomeModifier modifier)
    {
        float radius = modifier.GetRadius() + 4f;
        float bounds = hm.m_width * hm.m_scale * 0.5f;
        Vector3 mod = modifier.transform.position;
        Vector3 map = hm.transform.position;
        return mod.x + radius >= map.x - bounds && 
               mod.x - radius <= map.x + bounds && 
               mod.z + radius >= map.z - bounds &&  
               mod.z - radius <= map.z + bounds;
    }

    public static void ApplyBiomeModifiers(this Heightmap hm)
    {
        List<BiomeModifier> modifiers = BiomeModifier.GetAllInstances();
        for (int i = 0; i < modifiers.Count; ++i)
        {
            BiomeModifier mod = modifiers[i];
            if (mod.enabled && hm.Encapsulates(mod))
            {
                hm.PaintBiome(mod.transform.position, mod.m_radius, mod.m_biome, mod.m_paintHeightCheck, false);
            }
        }
    }

    public static void PaintBiome(this Heightmap hm, Vector3 worldPos, float radius, Heightmap.Biome biome, bool heightCheck, bool apply)
    {
        // worldPos.x -= 0.5f;
        // worldPos.z -= 0.5f;
        // float num1 = worldPos.y - hm.transform.position.y;
        // hm.WorldToVertexMask(worldPos, out int x1, out int y1);
        // float f = radius / hm.m_scale;
        // int num2 = Mathf.CeilToInt(f);
        // Vector2 a1 = new Vector2(x1, y1);
        // for (int y2 = y1 - num2; y2 <= y1 + num2; ++y2)
        // {
        //     for (int x2 = x1 - num2; x2 <= x1 + num2; ++x2)
        //     {
        //         if (x2 >= 0 && y2 >= 0 && x2 < hm.m_paintMask.width + 1)
        //     }
        // }

    }
    
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.ApplyModifiers))]
    private static class Heightmap_ApplyModifiers_Patch
    {
        private static void Prefix(Heightmap __instance)
        {
            __instance.ApplyBiomeModifiers();
        }
    }
}