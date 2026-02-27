using UnityEngine;

namespace Workshop;

public class PavedGrass : IPaint
{
    public PavedGrass(string id, string name, int index = 1) : base(
        id, name, PaintMan.GetPaintType("PavedGrass"), index)
    {
    }
    
    public override Color GetColor() => new Color(0.0f, 0.0f, 0.5f, 0.5f);

    public override Heightmap.Biome GetBiome() => Heightmap.Biome.None;
}