using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Workshop;

public static class PaintMan
{
    private static readonly Dictionary<string, TerrainModifier.PaintType> paintTypes;
    private static readonly Dictionary<TerrainModifier.PaintType, string> customPaintTypes;

    static PaintMan()
    {
        paintTypes = new Dictionary<string, TerrainModifier.PaintType>();
        customPaintTypes = new Dictionary<TerrainModifier.PaintType, string>();
    }

    public static void Init()
    {
        Harmony harmony = Workshop.instance._harmony;
        harmony.Patch(AccessTools.Method(typeof(Enum), nameof(Enum.GetValues)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_Enum_GetValues))));
        harmony.Patch(AccessTools.Method(typeof(Enum), nameof(Enum.GetNames)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_Enum_GetNames))));
        harmony.Patch(AccessTools.Method(typeof(Enum), nameof(Enum.GetName)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_Enum_GetName))));
        
        harmony.Patch(AccessTools.Method(typeof(TerrainComp), nameof(TerrainComp.PaintCleared)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_TerrainComp_PaintCleared))));
        harmony.Patch(AccessTools.Method(typeof(TerrainComp), nameof(TerrainComp.Awake)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_TerrainComp_Awake))));
        harmony.Patch(AccessTools.Method(typeof(TerrainComp), nameof(TerrainComp.Initialize)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_TerrainComp_Initialize))));
        harmony.Patch(AccessTools.Method(typeof(TerrainComp), nameof(TerrainComp.Save)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_TerrainComp_Save))));
        harmony.Patch(AccessTools.Method(typeof(TerrainComp), nameof(TerrainComp.Load)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_TerrainComp_Load))));
        
        harmony.Patch(AccessTools.Method(typeof(Heightmap), nameof(Heightmap.RebuildRenderMesh)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan),
                nameof(Patch_Heightmap_RebuildRenderMesh))));

        harmony.Patch(AccessTools.Method(typeof(ClutterSystem), nameof(ClutterSystem.GetPatchBiomes)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan),
                nameof(Patch_ClutterSystem_GetPatchBiomes))));
        harmony.Patch(AccessTools.Method(typeof(ClutterSystem), nameof(ClutterSystem.GetGroundInfo)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PaintMan), nameof(Patch_ClutterSystem_GetGroundInfo))));
    }

    public static TerrainModifier.PaintType GetPaintType(string name)
    {
        if (Enum.TryParse(name, true, out TerrainModifier.PaintType type)) return type;
        if (paintTypes.TryGetValue(name, out type)) return type;

        Dictionary<TerrainModifier.PaintType, string> map = GetPaintTypes();
        foreach (KeyValuePair<TerrainModifier.PaintType, string> kvp in map)
        {
            if (kvp.Value != name) continue;
            type = kvp.Key;
            paintTypes[name] = type;
            return type;
        }

        type = (TerrainModifier.PaintType)name.GetStableHashCode();
        paintTypes[name] = type;
        customPaintTypes[type] = name;
        return type;
    }

    private static Dictionary<TerrainModifier.PaintType, string> GetPaintTypes()
    {
        Array values = Enum.GetValues(typeof(TerrainModifier.PaintType));
        string[] names = Enum.GetNames(typeof(TerrainModifier.PaintType));
        Dictionary<TerrainModifier.PaintType, string> map = new();
        for (int i = 0; i < values.Length; ++i)
        {
            map[(TerrainModifier.PaintType)values.GetValue(i)] = names[i];
        }

        foreach (KeyValuePair<TerrainModifier.PaintType, string> kvp in customPaintTypes)
        {
            map[kvp.Key] = kvp.Value;
        }
        return map;
    }

    private static void Patch_Enum_GetValues(Type enumType, ref Array __result)
    {
        if (enumType != typeof(TerrainModifier.PaintType)) return;
        if (paintTypes.Count == 0) return;
        TerrainModifier.PaintType[] f = new TerrainModifier.PaintType[__result.Length + paintTypes.Count];
        __result.CopyTo(f, 0);
        paintTypes.Values.CopyTo(f, __result.Length);
        __result = f;
    }

    private static bool Patch_Enum_GetName(Type enumType, object value, ref string __result)
    {
        if (enumType != typeof(TerrainModifier.PaintType)) return true;
        if (customPaintTypes.TryGetValue((TerrainModifier.PaintType)value, out string data))
        {
            __result = data;
            return false;
        }

        return true;
    }

    private static void Patch_Enum_GetNames(Type enumType, ref string[] __result)
    {
        if (enumType != typeof(TerrainModifier.PaintType)) return;
        if (paintTypes.Count == 0) return;
        __result = __result.AddRangeToArray(paintTypes.Keys.ToArray());
    }

    private static bool Patch_TerrainComp_PaintCleared(TerrainComp __instance,
        Vector3 worldPos,
        float radius,
        TerrainModifier.PaintType paintType,
        bool heightCheck,
        bool apply)
    {
        if (!IPaint.TryGetPaintTool(paintType, out IPaint paint)) return true;
        
        TerrainColors terrainColors = __instance.GetComponent<TerrainColors>();
        worldPos.x -= 0.5f;
        worldPos.z -= 0.5f;
        float heightOffset  = worldPos.y - __instance.transform.position.y;
        __instance.m_hmap.WorldToVertexMask(worldPos, out int x, out int y);
        float radiusInVertices = radius / __instance.m_hmap.m_scale;
        int vertexSearchRadius = Mathf.CeilToInt(radiusInVertices);
        Vector2 centerVertex = new Vector2(x, y);
            
        for (int i = y - vertexSearchRadius; 
             i <= y + vertexSearchRadius; 
             ++i)
        {
            UpdatePaints(
                __instance, 
                terrainColors,
                paint, 
                centerVertex, 
                x, 
                vertexSearchRadius, 
                i, 
                heightCheck, 
                heightOffset, 
                radiusInVertices);
        }
        return false;
    }
    
    private static void UpdatePaints(TerrainComp comp, TerrainColors terrainColors, IPaint paint, Vector2 centerVertex, int centerVertex_x, int vertexSearchRadius, int vertex_y, bool heightCheck, float heightOffset, float radiusInVertices)
    {
        for (int i = centerVertex_x - vertexSearchRadius; 
             i <= centerVertex_x + vertexSearchRadius; 
             ++i)
        {
            UpdatePaint(
                comp, 
                terrainColors,
                paint, 
                centerVertex, 
                i, 
                vertex_y, 
                heightCheck, 
                heightOffset, 
                radiusInVertices);
        }
    }

    private static void UpdatePaint(TerrainComp comp, TerrainColors terrainColors, IPaint paint, Vector3 centerVertex, int vertex_x, int vertex_y, bool heightCheck, float heightOffset, float radiusInVertices)
    {
        float distanceFromCenter = Vector2.Distance(centerVertex, new Vector2(vertex_x, vertex_y));
        int gridStride = comp.m_width + 1;
                    
        bool withinBounds = vertex_x >= 0 && vertex_y >= 0 && vertex_x < gridStride && vertex_y < gridStride;
        bool passesHeightCheck = !heightCheck || comp.m_hmap.GetHeight(vertex_x, vertex_y) <= heightOffset;

        if (withinBounds && passesHeightCheck)
        {
            float blendWeight = Mathf.Pow(1f - Mathf.Clamp01(distanceFromCenter / radiusInVertices), 0.1f);
            Color current = comp.m_hmap.GetPaintMask(vertex_x, vertex_y);
            float alpha = current.a;
            
            Color blendedColor = Color.Lerp(current, paint.GetColor(), blendWeight);
            blendedColor.a = alpha;
                
            int index = vertex_y * gridStride + vertex_x;
            comp.m_modifiedPaint[index] = true;
            comp.m_paintMask[index] = blendedColor;

            if (paint.isBiomePaint && terrainColors != null)
            {
                terrainColors.SetBiomeColor(index, paint.GetBiomeColor());
            }
        }
    }

    private static void Patch_TerrainComp_Awake(TerrainComp __instance) => __instance.gameObject.AddComponent<TerrainColors>();

    private static void Patch_TerrainComp_Initialize(TerrainComp __instance) => __instance.GetComponent<TerrainColors>()?.Initialize();
    
    private static void Patch_TerrainComp_Save(TerrainComp __instance) => __instance.GetComponent<TerrainColors>()?.Save();

    private static void Patch_TerrainComp_Load(TerrainComp __instance) => __instance.GetComponent<TerrainColors>()?.Load();
    
    private static void Patch_Heightmap_RebuildRenderMesh(Heightmap __instance)
    {
        if (__instance.m_isDistantLod || __instance.m_renderMesh == null) return;
        
        if (!TerrainColors.TryFindTerrainColors(__instance.transform.position, out TerrainColors component)) return;
        component.ApplyToHeightmap(__instance);
    }
    
    private static Color32 GetBiomeColorFromMesh(this Heightmap hm, int x, int y)
    {
        List<Color32> colors = new List<Color32>();
        hm.m_renderMesh.GetColors(colors);
        if (colors.Count <= 0) return new Color32(0, 0, 0, 0);
        if (x < 0 || y < 0 || x >= hm.m_width || y >= hm.m_width) return colors[0];
        int index = x * (hm.m_width + 1) + y;
        if (index > colors.Count - 1) return colors[0];
        return colors[index];
    }

    private static Heightmap.Biome GetBiomeFromMesh(this Heightmap hm, Vector3 point, float oceanLevel = 0.02f, bool waterAlwaysOcean = false)
    {
        if (hm.m_isDistantLod | waterAlwaysOcean)
        {
            return WorldGenerator.instance.GetBiome(point.x, point.z, oceanLevel, waterAlwaysOcean);
        }

        if (!TerrainColors.TryFindTerrainColors(hm.transform.position, out TerrainColors component))
        {
            return hm.GetBiome(point);
        }

        return component.GetBiome(hm, point);
    }

    private static Heightmap.Biome FindBiomeClutterByMesh(Vector3 point)
    {
        if (ZoneSystem.instance && !ZoneSystem.instance.IsZoneLoaded(point))
        {
            return Heightmap.Biome.None;
        }
        Heightmap hm = Heightmap.FindHeightmap(point);
        if (hm == null) return Heightmap.Biome.None;
        return hm.GetBiomeFromMesh(point);
    }

    private static Heightmap.Biome GetPatchBiomesByMesh(Vector3 center, float halfSize)
    {
        Heightmap.Biome bc1 = FindBiomeClutterByMesh(new Vector3(center.x - halfSize, 0.0f, center.z - halfSize));
        Heightmap.Biome bc2 = FindBiomeClutterByMesh(new Vector3(center.x + halfSize, 0.0f, center.z - halfSize));
        Heightmap.Biome bc3 = FindBiomeClutterByMesh(new Vector3(center.x - halfSize, 0.0f, center.z + halfSize));
        Heightmap.Biome bc4 = FindBiomeClutterByMesh(new Vector3(center.x - halfSize, 0.0f, center.z + halfSize));
        
        if (bc1 == Heightmap.Biome.None ||
            bc2 == Heightmap.Biome.None ||
            bc3 == Heightmap.Biome.None ||
            bc4 == Heightmap.Biome.None)
        {
            return Heightmap.Biome.None;
        }
        
        return bc1 | bc2 | bc4 | bc3;
    }

    private static bool GetGroundInfoByMesh(
        this ClutterSystem cs, 
        Vector3 p, 
        out Vector3 point, 
        out Vector3 normal, 
        out Heightmap hmap,
        out Heightmap.Biome biome)
    {
        Vector3 origin = p + Vector3.up * 500f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1000f, cs.m_placeRayMask))
        {
            point = hit.point;
            normal = hit.normal;
            hmap = hit.collider.GetComponent<Heightmap>();
            biome = hmap.GetBiomeFromMesh(point);
            return true;
        }

        point = p;
        normal = Vector3.up;
        hmap = null;
        biome = Heightmap.Biome.None;
        return false;
    }

    private static bool Patch_ClutterSystem_GetPatchBiomes(Vector3 center, float halfSize, ref Heightmap.Biome __result)
    {
        __result = GetPatchBiomesByMesh(center, halfSize);
        return false;
    }

    private static bool Patch_ClutterSystem_GetGroundInfo(
        ClutterSystem __instance, 
        Vector3 p, 
        out Vector3 point,
        out Vector3 normal, 
        out Heightmap hmap, 
        out Heightmap.Biome biome, 
        ref bool __result)
    {
        __result = __instance.GetGroundInfoByMesh(p, out point, out normal, out hmap, out biome);
        return false;
    }
}