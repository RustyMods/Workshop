using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public class SelectByBounds : ISelectMany
{
    private static readonly List<Piece> SelectByBoundsPieces = new();

    public static bool IsSelectByBoundsPiece(Piece piece) => SelectByBoundsPieces.Contains(piece);
    
    public SelectByBounds(string id, string name, int index = 1) : base(id, name, index)
    {
        SelectByBoundsPieces.Add(piece);
    }

    protected override bool TryGetSelection(Player player, out List<ZNetView> objects) => TryGetConnectedPieces(player, out objects);

    private static readonly HashSet<ZNetView> Visited = new(5000);
    private static readonly Queue<ZNetView> ToProcess = new(5000);
    private static readonly List<ZNetView> Results = new(6000);
    private static readonly Collider[] OverlapBuffer = new Collider[128];
    
    private const float ConnectionDistance = 0.5f; // Max distance to consider "connected"
    private const int MaxIterations = 6000; // Safety limit
    
    public static bool TryGetConnectedPieces(Player player, out List<ZNetView> objects)
    {
        objects = new List<ZNetView>();
        
        if (player.GetHoveringPiece() == null) return false;

        ZNetView znv = player.GetHoveringPiece().m_nview;
        if (znv == null) return false;
        
        Visited.Clear();
        ToProcess.Clear();
        Results.Clear();
        
        ToProcess.Enqueue(znv);
        Visited.Add(znv);
        
        int iterations = 0;
        
        while (ToProcess.Count > 0 && iterations < MaxIterations)
        {
            ++iterations;
            ZNetView current = ToProcess.Dequeue();
            Results.Add(current);
            
            FindConnectedNeighbors(current, ToProcess, Visited);
        }
        
        objects.AddRange(Results);
        return objects.Count > 0;
    }

    private static void FindConnectedNeighbors(ZNetView current, Queue<ZNetView> toProcess, HashSet<ZNetView> visited)
    {
        // Get bounds of current object
        Bounds bounds = GetObjectBounds(current.gameObject);
        
        // Expand slightly for connection detection
        bounds.Expand(ConnectionDistance * 2f);
        
        // Single physics query for all potential neighbors
        int hitCount = Physics.OverlapBoxNonAlloc(
            bounds.center,
            bounds.extents,
            OverlapBuffer,
            Quaternion.identity,
            Piece.s_pieceRayMask
        );
        
        // Process hits
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = OverlapBuffer[i];
            if (col.isTrigger) continue;
            
            ZNetView view = col.GetComponentInParent<ZNetView>();
            if (view == null || view == current) continue;
            if (view.m_type == ZDO.ObjectType.Terrain || !view.m_persistent) continue;
            if (visited.Contains(view)) continue;
            
            // Check if actually connected (close enough)
            if (AreConnected(current.gameObject, view.gameObject))
            {
                visited.Add(view);
                toProcess.Enqueue(view);
            }
        }
    }

    private static bool AreConnected(GameObject obj1, GameObject obj2)
    {
        Bounds bounds1 = GetObjectBounds(obj1);
        Bounds bounds2 = GetObjectBounds(obj2);
        
        // Quick distance check
        float distance = Vector3.Distance(bounds1.center, bounds2.center);
        float maxDistance = bounds1.extents.magnitude + bounds2.extents.magnitude + ConnectionDistance;
        
        if (distance > maxDistance) return false;
        
        // More precise check: are bounds close enough?
        return bounds1.Intersects(bounds2) || 
               BoundsDistance(bounds1, bounds2) <= ConnectionDistance;
    }

    private static float BoundsDistance(Bounds b1, Bounds b2)
    {
        float dx = Mathf.Max(0, Mathf.Max(b1.min.x - b2.max.x, b2.min.x - b1.max.x));
        float dy = Mathf.Max(0, Mathf.Max(b1.min.y - b2.max.y, b2.min.y - b1.max.y));
        float dz = Mathf.Max(0, Mathf.Max(b1.min.z - b2.max.z, b2.min.z - b1.max.z));
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static Bounds GetObjectBounds(GameObject obj)
    {
        // Try to get cached bounds from WearNTear
        if (obj.TryGetComponent(out WearNTear wnt) && wnt.m_bounds != null && wnt.m_bounds.Count > 0)
        {
            // Combine all bounds
            Bounds combined = new Bounds(wnt.m_bounds[0].m_pos, wnt.m_bounds[0].m_size * 2f);
            for (int i = 1; i < wnt.m_bounds.Count; i++)
            {
                Bounds b = new Bounds(wnt.m_bounds[i].m_pos, wnt.m_bounds[i].m_size * 2f);
                combined.Encapsulate(b);
            }
            return combined;
        }
        
        // Fallback: calculate from colliders
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one);
        }
        
        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            if (!colliders[i].isTrigger)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }
        
        return bounds;
    }
}