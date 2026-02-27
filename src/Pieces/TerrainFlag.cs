using System;
using System.Text;
using UnityEngine;

namespace Workshop;

public static class FlagVars
{
    public static readonly int PaintType = "Workshop.Blueprint.Flag.PaintType".GetStableHashCode();
    public static readonly int IsSquare = "Workshop.Blueprint.Flag.IsSquare".GetStableHashCode();
    public static readonly int Radius = "Workshop.Blueprint.Flag.Radius".GetStableHashCode();
    public static readonly int SmoothRadius = "Workshop.Blueprint.Flag.SmoothRadius".GetStableHashCode();
    public static readonly int Level = "Workshop.Blueprint.Flag.Level".GetStableHashCode();
}
public class TerrainFlag : MonoBehaviour, Interactable, Hoverable, TextReceiver
{
    public string m_name = "Terrain Marker";

    public ZNetView m_nview;
    public Piece m_piece;
    public CustomProjector m_projector;
    public Renderer[] m_renderers;
    
    public TerrainModifier m_terrainModifier;
    public string m_text = "dirt;circle;20;25;true";
    public TerrainModifier.PaintType m_type;
    public bool m_isSquare;
    public float m_radius;
    public float m_smoothRadius;
    public bool m_level;
    public bool m_isReady;
    public bool interactable = true;
    
    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        m_piece = GetComponent<Piece>();
        m_projector = GetComponent<CustomProjector>();
        m_renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void FixedUpdate()
    {
        if (!Player.m_localPlayer) return;
        if (!Player.m_localPlayer.TakeInput()) return;
        
        if (!ZInput.GetKeyDown(KeyCode.F6)) return;
        
        interactable = !interactable;
        foreach (Renderer r in m_renderers)
        {
            r.enabled = interactable;
        }
    }

    public void Start()
    {
        foreach (Renderer r in m_renderers)
        {
            r.enabled = interactable;
        }
        if (m_nview == null || !m_nview.IsValid()) return;
        ZDO zdo = m_nview.GetZDO();
        if (zdo == null) return;
        m_isSquare = zdo.GetBool(FlagVars.IsSquare);
        m_projector.m_square = m_isSquare;
        m_radius = zdo.GetFloat(FlagVars.Radius);
        m_projector.m_radius = m_radius;
        m_smoothRadius = zdo.GetFloat(FlagVars.SmoothRadius, 10f);
        m_type = (TerrainModifier.PaintType)zdo.GetInt(FlagVars.PaintType);
        m_level = zdo.GetBool(FlagVars.Level, true);
        m_isReady = m_radius > 0f;
        m_text = $"{m_type};{(m_isSquare ? "square" : "circle")};{m_radius:0.0};{m_smoothRadius:0.0};{m_level}";
        if (m_isReady) Poke(true);
    }
    
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (!interactable) return false;
        
        if (alt || !m_isReady)
        {
            TextInput.instance.RequestText(this, "$label_set_terrain", 100);
            return false;
        }
        
        if (!m_isReady) return false;

        if (!m_projector.enabled)
        {
            m_projector.enabled = true;
            return false;
        }

        bool poke = true;
        
        if (m_terrainModifier == null)
        {
            m_terrainModifier = gameObject.AddComponent<TerrainModifier>();
            poke = false;
        }
        
        m_terrainModifier.m_paintType = m_type;
        m_terrainModifier.m_levelRadius = m_radius;
        m_terrainModifier.m_paintRadius = m_radius;
        m_terrainModifier.m_smoothRadius = m_smoothRadius;
        m_terrainModifier.m_square = m_isSquare;
        m_terrainModifier.m_level = m_level;

        if (poke)
        {
            m_terrainModifier.PokeHeightmaps();
        }

        return true;
    }

    public void Poke(bool create = false)
    {
        if (!m_isReady) return;
        
        if (m_terrainModifier == null && create)
        {
            m_terrainModifier = gameObject.AddComponent<TerrainModifier>();
            m_terrainModifier.m_paintType = m_type;
            m_terrainModifier.m_levelRadius = m_radius;
            m_terrainModifier.m_paintRadius = m_radius;
            m_terrainModifier.m_smoothRadius = m_smoothRadius;
            m_terrainModifier.m_square = m_isSquare;
            m_terrainModifier.m_level = m_level;
        }
        if (m_terrainModifier != null) m_terrainModifier.PokeHeightmaps();
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    
    public string GetHoverText()
    {
        if (!interactable) return "";
        
        StringBuilder sb = new StringBuilder();
        sb.Append("[<color=yellow><b>$KEY_Use</b></color>] $hover_poke");
        sb.Append("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $label_set_terrain");
        
        return Localization.instance.Localize(sb.ToString());
    }

    public string GetHoverName() => m_name;

    public string GetText() => m_text;

    public void SetText(string text)
    {
        m_text = text;
        string[] parts = text.Split(';');
        TerrainModifier.PaintType type = parts.GetEnum(0, TerrainModifier.PaintType.Dirt);
        bool square = parts.GetString(1, "circle")
            .Equals("square", StringComparison.InvariantCultureIgnoreCase);
        float radius = parts.GetFloat(2, 2f);
        float smooth = parts.GetFloat(3);
        bool level = parts.GetBool(4, true);

        if (Player.m_localPlayer)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, 
                $"Set terrain: type: {type}; {(square ? "square" : "circle")}; radius: {radius:0.0}; smooth radius: {smooth:0.0}; level: {level}");
        }
        
        SetPaintType(type);
        SetIsSquare(square);
        SetRadius(radius);
        SetSmoothRadius(smooth);
        SetLevel(level);
        m_isReady = m_radius > 0f;
    }

    public void SetPaintType(TerrainModifier.PaintType type)
    {
        if (m_type == type) return;
        m_type = type;
        if (m_nview && m_nview.IsValid())
        {
            m_nview.GetZDO().Set(FlagVars.PaintType, (int)type);
        }
    }

    public void SetIsSquare(bool isSquare)
    {
        if (m_isSquare == isSquare) return;
        m_isSquare = isSquare;
        m_projector.m_square = isSquare;
        if (m_nview && m_nview.IsValid())
        {
            m_nview.GetZDO().Set(FlagVars.IsSquare, m_isSquare);
        }
    }

    public void SetRadius(float radius)
    {
        if (Mathf.Approximately(m_radius, radius)) return;
        m_radius = radius;
        m_projector.m_radius = radius;
        m_isReady = radius > 0f;
        if (m_nview && m_nview.IsValid())
        {
            m_nview.GetZDO().Set(FlagVars.Radius, m_radius);
        }
    }

    public void SetSmoothRadius(float radius)
    {
        if (Mathf.Approximately(m_smoothRadius, radius)) return;
        m_smoothRadius = radius;
        if (m_nview && m_nview.IsValid())
        {
            m_nview.GetZDO().Set(FlagVars.SmoothRadius, m_smoothRadius);
        }
    }

    public void SetLevel(bool level)
    {
        if (m_level == level) return;
        m_level = level;
        if (m_nview && m_nview.IsValid())
        {
            m_nview.GetZDO().Set(FlagVars.Level, level);
        }
    }
}