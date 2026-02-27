using System;
using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public class AreaProjector : MonoBehaviour
{
    public static float radius = 10f;
    public static AreaProjector instance;
    
    public bool targetPiecesOnly = true;

    public CircleProjector projector;
    
    public static implicit operator bool(AreaProjector projector) => projector != null;

    public float lastHighlightTime;
    public void Awake()
    {
        projector = GetComponentInChildren<CircleProjector>(true);
        projector.m_radius = radius;
        projector.m_mask = LayerMask.GetMask("terrain");
        projector.m_nrOfSegments = Mathf.CeilToInt(radius / 2.0f);
        instance = this;
    }

    public void OnDestroy()
    {
        instance = null;
    }

    public void Update()
    {
        if (!Player.m_localPlayer || Player.m_localPlayer.IsDead()) return;
        if (Player.m_localPlayer.m_placementMarkerInstance != null)
        {
            transform.position = Player.m_localPlayer.m_placementMarkerInstance.transform.position;
        }
        
        float scroll = ZInput.GetMouseScrollWheel();
        if (scroll > Player.m_localPlayer.m_scrollAmountThreshold)
        {
            ++radius;
        }

        if (scroll < -Player.m_localPlayer.m_scrollAmountThreshold)
        {
            --radius;
        }
        
        if (radius <= 0.0f)
        {
            radius = 1f;
        }
        projector.m_radius = radius;
        projector.m_nrOfSegments = Math.Max(Mathf.CeilToInt(radius), 4);
    }

    public void FixedUpdate()
    {
        if (Time.time - lastHighlightTime > 0.15f)
        {
            HighlightObjects();
        }
    }

    public void HighlightObjects()
    {
        if (targetPiecesOnly)
        {
            if (SelectByArea.TryGetInArea(Player.m_localPlayer, out List<ZNetView> areaPieces))
            {
                Selectable.HighlightObjects(areaPieces);
            }
        }
        else
        {
            if (SelectByArea.TryGetInArea(Player.m_localPlayer, out List<ZNetView> areaObjects))
            {
                Selectable.HighlightObjects(areaObjects);
            }
        }        
        lastHighlightTime = Time.time;
    }
}