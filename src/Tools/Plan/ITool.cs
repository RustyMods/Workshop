using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Workshop;

public abstract class ITool
{
    public static readonly Dictionary<Piece, ITool> m_toolPieces = new();
    
    public readonly int index;
    public readonly Piece piece;
    public bool adminOnly;

    public abstract void OnUse(Player player);

    protected ITool(string id, string name, int index = 1)
    {
        this.index = index;
        
        GameObject go = new GameObject(id, typeof(Piece));
        go.transform.SetParent(MockManager.transform);
        piece = go.GetComponent<Piece>();
        piece.m_icon = BuildTools.ghostHammer.GetIcon();
        piece.m_repairPiece = true;
        piece.m_name = name;
        piece.m_description = name + "_desc";
        piece.m_category = Piece.PieceCategory.Misc;

        m_toolPieces[piece] = this;
        BuildTools.ghostHammer.InsertPiece(piece.gameObject, index);
    }
    
    public static bool IsTool(Piece piece) => m_toolPieces.ContainsKey(piece);
    
    public static bool TryGetTool(Piece piece, out ITool tool) => m_toolPieces.TryGetValue(piece, out tool);
}