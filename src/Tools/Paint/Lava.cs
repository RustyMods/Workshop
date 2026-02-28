using UnityEngine;

namespace Workshop;

public class Lava : IPaint
{
    public Lava(string id, string name, int index = 1) : base(id, name, PaintMan.GetPaintType("Lava"), index)
    {
        isBiomePaint = true;
        terrainOp.m_settings.m_square = false;
        terrainOp.m_settings.m_smooth = true;
        terrainOp.m_settings.m_raise = true;
        terrainOp.m_settings.m_raiseRadius = 2f;
        terrainOp.m_settings.m_raiseDelta = 1;
        terrainOp.m_settings.m_raisePower = 0.1f;
        piece.m_icon = SpriteManager.GetSprite("lava_icon.png");
    }
    
    public override Color GetColor() => new Color(0f, 0f, 0f, 1f);
    
    public override Color32 GetBiomeColor() => new (byte.MaxValue, 0,  0, byte.MaxValue);

    public override Heightmap.Biome GetBiome() => Heightmap.Biome.AshLands;
}