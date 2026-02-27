using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workshop;

[Serializable]
public class Blueprint
{
    public FileType type;
    public string filename;
    public string[] lines;
    
    public Blueprint(){}

    public Blueprint(string filename, FileType type, string[] lines)
    {
        this.filename = filename;
        this.type = type;
        this.lines = lines;
    }

    public BlueprintRecipe ToRecipe()
    {
        BlueprintRecipe recipe = ScriptableObject.CreateInstance<BlueprintRecipe>();
        recipe.name = filename;
        recipe.blueprint = this;
        recipe.settings.Parse(this);
        return recipe;
    }

    public TempBlueprint ToTemp()
    {
        TempBlueprint local = new TempBlueprint();
        local.settings.Parse(this);
        return local;
    }

    public void Format(BlueprintSettings settings)
    {
        List<string> format = new List<string>();
        format.Add("#Name:" + settings.Name);
        format.Add("#Creator:" + settings.Creator);
        format.Add("#Description:" + settings.Description);
        format.Add("#Center:" + settings.Center);
        format.Add("#Requirements:" + new PieceRequirements(settings.requirements).ToCustomString());
        format.Add("#Coordinates:" + settings.Coordinates.ToCustomString());
        format.Add("#Rotation:" + settings.Rotation.ToCustomString());
        format.Add("#Icon:" + settings.Icon);
        if (settings.SnapPoints.Count > 0)
        {
            format.Add("#SnapPoints");
            for (int i = 0; i < settings.SnapPoints.Count; ++i)
            {
                format.Add($"{settings.SnapPoints[i]}");
            }
        }

        if (settings.Pieces.Count > 0)
        {
            format.Add("#Pieces");
            for (int i = 0; i < settings.Pieces.Count; ++i)
            {
                format.Add($"{settings.Pieces[i]}");
            }
        }

        if (settings.Terrains.Count > 0)
        {
            format.Add("#Terrains");
            for (int i = 0; i < settings.Terrains.Count; ++i)
            {
                format.Add($"{settings.Terrains[i]}");
            }
        }
        
        lines = format.ToArray();
        type = FileType.Blueprint;
        filename = settings.filename;
    }
    
    public void Write(string filepath = "")
    {
        if (string.IsNullOrEmpty(filepath))
        {
            filepath = Path.Combine(ConfigManager.ConfigFolderPath, filename);
        }
        File.WriteAllLines(filepath, lines);
        BlueprintMan.blueprintFilePaths[filename] = filepath;
    }
}