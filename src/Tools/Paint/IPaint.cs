using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public static class PaintVars
{
    public static readonly int TerrainColors = "Workshop.Paint.BiomeColors".GetStableHashCode();
}
public abstract class IPaint
{
    public static readonly Dictionary<Piece, IPaint> m_paintTools = new();
    private static readonly Dictionary<TerrainModifier.PaintType, IPaint> m_paintMask = new();
    public static bool IsPaintTool(Piece piece) => m_paintTools.ContainsKey(piece);
    public static bool TryGetPaintTool(TerrainModifier.PaintType type, out IPaint paintTool) => m_paintMask.TryGetValue(type, out paintTool);

    protected readonly Piece piece;
    protected readonly TerrainOp terrainOp;
    public bool adminOnly;
    public bool overrideAlpha = false;
    public bool blend = true;
    
    public bool isBiomePaint;
    public bool blendTerrain = true;

    public bool reset;
    protected IPaint(string id, string name, TerrainModifier.PaintType type, int index = 1)
    {
        ItemDrop cultivator = PrefabManager.GetPrefab("Cultivator").GetComponent<ItemDrop>();
        List<GameObject> pieces = cultivator.m_itemData.m_shared.m_buildPieces.m_pieces;
        GameObject replant = pieces[1];
        
        GameObject prefab = Object.Instantiate(replant.gameObject, MockManager.transform);
        prefab.name = id;
        piece = prefab.GetComponent<Piece>();
        piece.m_icon = BuildTools.ghostHammer.GetIcon();
        piece.m_name = name;
        piece.m_description = name + "_desc";
        piece.m_vegetationGroundOnly = false;
        piece.m_category = Piece.PieceCategory.Misc;
        terrainOp = prefab.GetComponent<TerrainOp>();
        terrainOp.m_settings.m_paintType = type;
        terrainOp.m_settings.m_level = false;
        terrainOp.m_settings.m_smooth = false;
        terrainOp.m_settings.m_raise = false;

        m_paintTools[piece] = this;
        m_paintMask[type] = this;
        
        BuildTools.ghostHammer.InsertPiece(prefab, index);
    }
    
    public abstract Color GetColor();

    public virtual Color32 GetBiomeColor() => new Color32();

    public virtual Heightmap.Biome GetBiome() => Heightmap.Biome.None;
}