using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public class Selectable : MonoBehaviour
{
    public float m_lastHighlightTime;
    public bool m_update;

    public Piece m_piece;

    public void Awake()
    {
        m_piece = GetComponent<Piece>();
    }
    public void FixedUpdate()
    {
        if (!m_update) return;
        m_lastHighlightTime += Time.fixedDeltaTime;
        if (m_lastHighlightTime > 0.2f)
        {
            ResetHighlight();
            m_update = false;
        }
    }

    public void Highlight(Color color)
    {
        MaterialMan.instance.SetValue(gameObject, ShaderProps._EmissionColor, color * 0.4f);
        MaterialMan.instance.SetValue(gameObject, ShaderProps._Color, color);
        m_lastHighlightTime = 0f;
        m_update = true;
    }

    public void ResetHighlight()
    {
        MaterialMan.instance.ResetValue(gameObject, ShaderProps._EmissionColor);
        MaterialMan.instance.ResetValue(gameObject, ShaderProps._Color);
    }
    
    public static void HighlightObjects<T>(List<T> objects) where T : MonoBehaviour
    {
        for (int i = 0; i < objects.Count; ++i)
        {
            T obj = objects[i];
            if (obj.TryGetComponent(out Selectable selectable))
            {
                selectable.Highlight(ConfigManager.HighlightColor);
            }
        }
    }
}