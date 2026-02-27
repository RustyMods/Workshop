using UnityEngine;

namespace Workshop;

public class CultivatedGrass : IPaint
{
    public CultivatedGrass(string id, string name, int index = 1) : base(id, name, PaintMan.GetPaintType("CultivatedGrass"), index)
    {
    }
    
    public override Color GetColor() => new Color(0.5f, 1f, 0.0f, 0.5f);

    public override Heightmap.Biome GetBiome()
    {
        throw new System.NotImplementedException();
    }
}