using UnityEngine;

namespace Workshop;

public class AshlandsGrass : IPaint
{
    public AshlandsGrass(string id, string name, int index = 1) : 
        base(id, name, PaintMan.GetPaintType("AshlandsGrass"), index)
    {
        isBiomePaint = true;
    }
    
    public override Color GetColor() => new Color(1f, 1f, 1f, 0f);
    
    public override Color32 GetBiomeColor() => new (byte.MaxValue, 0,  0, byte.MaxValue);

    public override Heightmap.Biome GetBiome() => Heightmap.Biome.AshLands;
}