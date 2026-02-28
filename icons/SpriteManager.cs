using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Workshop;

public static class SpriteManager
{
    private static readonly Dictionary<string, Sprite> sprites;

    static SpriteManager()
    {
        sprites = new Dictionary<string, Sprite>();
    }
    
    public static Sprite GetSprite(string fileName, string folderName = "icons")
    {
        if (sprites.TryGetValue(fileName, out Sprite sprite)) return sprite;
        
        Assembly assembly = Assembly.GetExecutingAssembly();
        string path = $"{Workshop.ModName}.{folderName}.{fileName}";
        using var stream = assembly.GetManifestResourceStream(path);
        if (stream == null) return null;
        byte[] buffer = new byte[stream.Length];
        _ = stream.Read(buffer, 0, buffer.Length);
        Texture2D texture = new Texture2D(2, 2);
        sprite = texture.LoadImage4x(buffer) ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero) : null;
        if (sprite != null)
        {
            sprite.name = fileName;
            sprites.Add(fileName, sprite);
        }
        else
        {
            throw new Exception($"Invalid sprite file: {fileName}");
        }
        
        return sprite;
    }
}