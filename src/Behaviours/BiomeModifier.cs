using System;
using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public class BiomeModifier : MonoBehaviour
{
    public ZNetView m_nview;
    public Heightmap.Biome m_biome;
    public float m_radius;
    public long m_creationTime;
    public bool m_wasEnabled;
    public bool m_playerModification;
    public int m_sortOrder;
    public bool m_paintHeightCheck;
    
    public static readonly List<BiomeModifier> instances = new  List<BiomeModifier>();
    public static int s_lastFramePoked;
    public static bool s_needsSorting;

    public void Awake()
    {
        instances.Add(this);
        s_needsSorting = true;
        m_wasEnabled = enabled;
        if (enabled)
        {
            PokeHeightmaps(true);
        }

        m_creationTime = GetCreationTime();
    }

    public void OnDestroy()
    {
        instances.Remove(this);
        s_needsSorting = true;
        if (!m_wasEnabled) return;
        PokeHeightmaps();
    }

    public long GetCreationTime()
    {
        long time = 0;
        if (m_nview && m_nview.GetZDO() != null)
        {
            int hash = m_nview.GetZDO().GetPrefab();
            ZDO zdo = m_nview.GetZDO();
            ZDOID uid = zdo.m_uid;
            time = zdo.GetLong(ZDOVars.s_terrainModifierTimeCreated);
            if (time == 0L)
            {
                time = ZDOExtraData.GetTimeCreated(uid);
                if (time != 0L)
                {
                    zdo.Set(ZDOVars.s_terrainModifierTimeCreated, time);
                    Workshop.LogError($"CreationTime should already be set for {name} Prefab: {hash}");
                }
            }
        }

        return time;
    }

    public float GetRadius() => m_radius;

    public void PokeHeightmaps(bool forceDelay = false)
    {
        List<Heightmap> heightmaps = Heightmap.GetAllHeightmaps();
        for (int i = 0; i < heightmaps.Count; ++i)
        {
            Heightmap hm = heightmaps[i];
            if (!hm.Encapsulates(this)) continue;
            hm.Poke(forceDelay);
        }

        if (!ClutterSystem.instance) return;
        ClutterSystem.instance.ResetGrass(transform.position, GetRadius());
    }

    public static List<BiomeModifier> GetAllInstances()
    {
        if (s_needsSorting)
        {
            instances.Sort(SortBiomeModifiers);
            s_needsSorting = false;
        }

        return instances;
    }

    public static int SortBiomeModifiers(BiomeModifier a, BiomeModifier b)
    {
        if (a.m_playerModification != b.m_playerModification)
        {
            return a.m_playerModification.CompareTo(b.m_playerModification);
        }
        if (a.m_sortOrder != b.m_sortOrder)
        {
            return a.m_sortOrder.CompareTo(b.m_sortOrder);
        }
        if (a.m_creationTime != b.m_creationTime)
        {
            return a.m_creationTime.CompareTo(b.m_creationTime);
        }
        return a.transform.position.sqrMagnitude.CompareTo(b.transform.position.sqrMagnitude);
    }
}