using UnityEngine;

namespace Workshop;

public class PlainsGrass : IPaint
{
    public PlainsGrass(string id, string name, int index = 1) : base(id, name, PaintMan.GetPaintType("PlainsGrass"), index)
    {
        isBiomePaint = true;
    }

    public override Color GetColor() => new Color(0f, 0f, 0f, 1f);

    public override Color32 GetBiomeColor() => new Color32(0, 0, 0, byte.MaxValue);

    public override Heightmap.Biome GetBiome() => Heightmap.Biome.Plains;
}