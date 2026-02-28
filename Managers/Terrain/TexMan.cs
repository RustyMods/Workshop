using System.Linq;
using UnityEngine;

namespace Workshop;

public static class TexMan
{
    // forest_d
    // mistlands_cliff_d
    
    // terrain_d

    public static Sprite[] sprites;

    public static Sprite paved => sprites[0];
    public static Sprite mistlandsRock => sprites[2];
    public static Sprite mistlandsGrass => sprites[7];
    public static Sprite lava => sprites[10];
    public static Sprite meadows => sprites[12];
    public static Sprite blackForest => sprites[13];
    
    public static void GetTerrainIcons()
    {
        Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
        Texture2D terrain = textures.FirstOrDefault(t => t.name.StartsWith("terrain_d"));
        if (terrain == null)
        {
            Workshop.LogWarning("Failed to find terrain d");
            return;
        }
        
        // 1024x1024
        // 4 x 4 grid
        // need to split texture into 16 parts of 256x256
        
        int tileSize = 256;
        int cols = 4;
        int rows = 4;

        sprites = new Sprite[rows * cols];

        for (int y = 0; y < rows; ++y)
        {
            for (int x = 0; x < cols; ++x)
            {
                int texX = x * tileSize;
                int texY = (rows - 1 - y) * tileSize;

                Rect rect = new Rect(texX, texY, tileSize, tileSize);
                Vector2 pivot = new Vector2(0.5f, 0.5f);

                sprites[y * cols + x] = Sprite.Create(terrain, rect, pivot);
            }
        }
        Workshop.LogWarning($"Found terrain d, generated icons: {sprites.Length}");
    }
}