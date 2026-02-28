using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServerSync;
using UnityEngine;

namespace Workshop;

public enum FileType
{
    Unknown,
    Blueprint, 
    VBuild
}

public enum TaskType
{
    RecipeChange,
    NewBlueprint,
    RemoveBlueprint,
}

[Serializable]
public abstract class ITask
{
    public TaskType type;
    public abstract void Execute();
}

[Serializable]
public class RecipeTask : ITask
{
    public string name;
    public List<Requirement> requirements;
    public override void Execute()
    {
        if (!BlueprintMan.recipes.TryGetValue(name, out BlueprintRecipe recipe)) return;
        recipe.m_resources = new PieceRequirements(requirements).ToPieceRequirement();
        if (!BlueprintMan.publishBlueprints.TryGetValue(name, out Blueprint blueprint)) return;
        blueprint.Format(recipe.settings);
        BlueprintMan.SyncBlueprints();
    }
}

[Serializable]
public class BlueprintTask : ITask
{
    public Blueprint blueprint;
    public override void Execute()
    {
        if (BlueprintMan.publishBlueprints.ContainsKey(blueprint.filename)) return;
        BlueprintRecipe recipe = ScriptableObject.CreateInstance<BlueprintRecipe>();
        recipe.name = blueprint.filename;
        recipe.settings.Parse(blueprint);

        if (!BlueprintMan.temps.TryGetValue(blueprint.filename, out TempBlueprint existing) || 
            !existing.TransferTo(recipe))
        {
            GameObject container = recipe.Load();
            if (container == null) return;
            recipe.PostProcess();
            BuildTools.planHammer.AddPiece(container);
        }
        blueprint.Write(Path.Combine(Path.Combine(BlueprintMan.GetPublishPath(), blueprint.filename))); // save or update blueprint locally
        BlueprintMan.publishBlueprints[blueprint.filename] = blueprint;
        BlueprintMan.recipes[blueprint.filename] = recipe;
        BlueprintMan.SyncBlueprints();
    }
}

public static class BlueprintMan
{
    private const string PUBLISH = "Publish";
    private const string LOCAL = "Local";
    private const string ICON = "Icons";
    private static readonly string PublishPath;
    private static readonly string LocalPath;
    private static readonly string IconPath;
    private static readonly CustomSyncedValue<string> sync;
    public static readonly Dictionary<string, byte[]> icons;
    // same blueprint can exist in temps and recipes, if so, do not load twice, simply transfer temp container to recipe container
    public static readonly Dictionary<string, TempBlueprint> temps;
    public static readonly Dictionary<string, BlueprintRecipe> recipes;
    public static readonly Dictionary<string, string> blueprintFilePaths;
    public static readonly Dictionary<string, Blueprint> localBlueprints;
    public static readonly Dictionary<string, Blueprint> publishBlueprints;
    public static readonly Queue<ITask> tasks;
    private static Task CurrentTask;

    static BlueprintMan()
    {
        PublishPath = Path.Combine(ConfigManager.GetFolderPath(), PUBLISH);
        LocalPath = Path.Combine(ConfigManager.GetFolderPath(), LOCAL);
        IconPath = Path.Combine(ConfigManager.GetFolderPath(), ICON);
        sync = new CustomSyncedValue<string>(ConfigManager.ConfigSync, "Workshop.Blueprint.Marketplace");
        icons = new Dictionary<string, byte[]>();
        temps = new Dictionary<string, TempBlueprint>();
        recipes = new Dictionary<string, BlueprintRecipe>();
        blueprintFilePaths = new Dictionary<string, string>();
        localBlueprints = new Dictionary<string, Blueprint>();
        publishBlueprints = new Dictionary<string, Blueprint>();
        tasks = new Queue<ITask>();
    }

    public static string GetPublishPath()
    {
        if (!Directory.Exists(PublishPath)) Directory.CreateDirectory(PublishPath);
        return PublishPath;
    }

    public static string GetLocalPath()
    {
        if (!Directory.Exists(LocalPath)) Directory.CreateDirectory(LocalPath);
        return LocalPath;
    }

    public static string GetIconPath()
    {
        if (!Directory.Exists(IconPath)) Directory.CreateDirectory(IconPath);
        return IconPath;
    }

    private static Task Process()
    {
        try
        {
            while (tasks.Count > 0)
            {
                ITask task = tasks.Dequeue();
                task.Execute();
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
    
    public static void ProcessTasks()
    {
        if (tasks.Count <= 0) return;
        if (CurrentTask != null) return;
        CurrentTask = Process();
    }

    public static void SyncBlueprints()
    {
        string text = ConfigManager.Serialize(publishBlueprints);
        sync.Value = text;
    }

    private static void OnSyncChanged()
    {
        if (string.IsNullOrEmpty(sync.Value)) return;
        Dictionary<string, Blueprint> incoming = ConfigManager.Deserialize<Dictionary<string, Blueprint>>(sync.Value);
        foreach (KeyValuePair<string, Blueprint> kvp in incoming)
        {
            if (publishBlueprints.ContainsKey(kvp.Key))
            {
                if (recipes.TryGetValue(kvp.Key, out BlueprintRecipe recipe))
                {
                    BlueprintRecipe tempRecipe = ScriptableObject.CreateInstance<BlueprintRecipe>();
                    tempRecipe.settings.Parse(kvp.Value);
                    PieceRequirements resources = new PieceRequirements(tempRecipe.settings.requirements);
                    recipe.m_resources = resources.ToPieceRequirement();
                    UnityEngine.Object.Destroy(tempRecipe);
                }
            }
            else
            {
                BlueprintRecipe recipe = ScriptableObject.CreateInstance<BlueprintRecipe>();
                recipe.settings.Parse(kvp.Value);
                recipe.name = kvp.Key;

                if (!temps.TryGetValue(kvp.Key, out TempBlueprint existing) || 
                    !existing.TransferTo(recipe))
                {
                    GameObject container = recipe.Load();
                    if (container == null) continue;
                    recipe.PostProcess();
                    BuildTools.planHammer.AddPiece(container);
                }
                else
                {
                    temps.Remove(kvp.Key);
                }
                publishBlueprints[kvp.Key] = kvp.Value;
                recipes[kvp.Key] = recipe;
            }
        }

        RevenueTab.canCollect = true;
    }

    public static bool IsReady() => BuildTools.planHammer != null;
    
    public static void ReadFiles()
    {
        string[] iconPaths = Directory.GetFiles(GetIconPath(), "*.png", SearchOption.AllDirectories);
        for (int i = 0; i < iconPaths.Length; ++i)
        {
            string path = iconPaths[i];
            string filename = Path.GetFileName(path);
            byte[] bytes = File.ReadAllBytes(path);
            icons[filename] = bytes;
        }

        string[] publishPaths = Directory.GetFiles(GetPublishPath(), "*", SearchOption.AllDirectories);
        for (int i = 0; i < publishPaths.Length; ++i)
        {
            string path = publishPaths[i];
            string extension = Path.GetExtension(path);
            string filename = Path.GetFileName(path);
            FileType type = FileType.Unknown;
            if (extension == ".blueprint")
            {
                type = FileType.Blueprint;
            }
            else if (extension == ".vbuild")
            {
                type = FileType.VBuild;
            }

            if (type == FileType.Unknown) continue;
            
            Blueprint blueprint = new Blueprint(filename, type, File.ReadAllLines(path));
            publishBlueprints[filename] = blueprint;
            blueprintFilePaths[filename] = path;
        }
        
        string[] localPaths = Directory.GetFiles(GetLocalPath(), "*", SearchOption.AllDirectories);
        for (int i = 0; i < localPaths.Length; ++i)
        {
            string path = localPaths[i];
            string extension = Path.GetExtension(path);
            string filename = Path.GetFileName(path);
            
            FileType type = extension switch
            {
                ".blueprint" => FileType.Blueprint,
                ".vbuild" => FileType.VBuild,
                _ => FileType.Unknown
            };

            if (type == FileType.Unknown) continue;
            
            Blueprint blueprint = new Blueprint(filename, type, File.ReadAllLines(path));
            localBlueprints[filename] = blueprint;
            blueprintFilePaths[filename] = path;
        }
    }

    public static void OnZNetAwake(ZNet net)
    {
        foreach (KeyValuePair<string, Blueprint> kvp in localBlueprints)
        {
            TempBlueprint temp = kvp.Value.ToTemp();
            temp.blueprint = kvp.Value;
            temps[kvp.Key] = temp;
        }
        
        if (net.IsServer())
        {
            foreach (KeyValuePair<string, Blueprint> kvp in publishBlueprints)
            {
                BlueprintRecipe recipe = kvp.Value.ToRecipe();
                recipe.blueprint = kvp.Value;
                recipes[kvp.Key] = recipe;
            }
            SyncBlueprints();
        }
        else
        {
            sync.ValueChanged += OnSyncChanged;
        }
    }

    public static void Dispose()
    {
        foreach (KeyValuePair<string, TempBlueprint> kvp in temps)
        {
            kvp.Value.Dispose();
        }

        foreach (KeyValuePair<string, BlueprintRecipe> kvp in recipes)
        {
            kvp.Value.Dispose();
        }
        
        temps.Clear();
        recipes.Clear();
    }
    
    public static void Load(PieceTable table, bool postProcess = false)
    {
        foreach (KeyValuePair<string, TempBlueprint> kvp in temps)
        {
            GameObject container = kvp.Value.Load();
            if (container == null) continue;
            if (postProcess) kvp.Value.PostProcess();
            table.m_pieces.Add(container);
        }
        
        foreach (KeyValuePair<string, BlueprintRecipe> kvp in recipes)
        {
            if (!temps.TryGetValue(kvp.Key, out TempBlueprint temp) || !temp.TransferTo(kvp.Value))
            {
                GameObject container = kvp.Value.Load();
                if (container == null) continue;
                if (postProcess) kvp.Value.PostProcess();
                table.m_pieces.Add(container);
            }
        }
    }
    public static bool IsBlueprint(this Piece piece) => piece.GetComponent<PlanContainer>();
    public static bool IsBlueprint(this GameObject go) => go.GetComponent<PlanContainer>();
}