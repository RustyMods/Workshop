using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ServerSync;
using UnityEngine;

namespace Workshop;

public enum FileType
{
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
        if (!BlueprintMan.blueprints.TryGetValue(name, out Blueprint blueprint)) return;
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
        if (BlueprintMan.blueprints.ContainsKey(blueprint.filename)) return;
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
        blueprint.Write(); // save or update blueprint locally
        BlueprintMan.blueprints[blueprint.filename] = blueprint;
        BlueprintMan.recipes[blueprint.filename] = recipe;
        BlueprintMan.SyncBlueprints();
    }
}

public static class BlueprintMan
{
    private static readonly CustomSyncedValue<string> sync = new(ConfigManager.ConfigSync, "Workshop.Blueprint.Marketplace");
    
    public static readonly Dictionary<string, byte[]> icons = new();
    public static readonly Dictionary<string, TempBlueprint> temps = new();
    public static readonly Dictionary<string, BlueprintRecipe> recipes = new();
    public static readonly Dictionary<string, Blueprint> blueprints = new();
    
    public static readonly Dictionary<string, string> blueprintFilePaths = new();
    public static readonly Dictionary<string, Blueprint> localBlueprints = new();

    public static readonly Queue<ITask> tasks = new();
    private static Task CurrentTask;

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
        string text = ConfigManager.Serialize(blueprints);
        sync.Value = text;
    }

    private static void OnSyncChanged()
    {
        if (string.IsNullOrEmpty(sync.Value)) return;
        Dictionary<string, Blueprint> incoming = ConfigManager.Deserialize<Dictionary<string, Blueprint>>(sync.Value);
        foreach (KeyValuePair<string, Blueprint> kvp in incoming)
        {
            if (blueprints.ContainsKey(kvp.Key))
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
                    existing.TransferTo(recipe))
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
                blueprints[kvp.Key] = kvp.Value;
                recipes[kvp.Key] = recipe;
            }
        }

        RevenueTab.canCollect = true;
    }

    public static bool IsReady() => BuildTools.planHammer != null;
    
    public static void ReadFiles()
    {
        string folderPath = ConfigManager.GetFolderPath();
        string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; ++i)
        {
            string path = files[i];
            string extension = Path.GetExtension(path);
            string filename = Path.GetFileName(path);
            
            if (extension == ".png")
            {
                byte[] bytes = File.ReadAllBytes(path);
                icons[filename] = bytes;
                Workshop.LogDebug($"Imported preview: {filename}");
            }
            else if (extension == ".blueprint")
            {
                Blueprint blueprint = new Blueprint(filename, FileType.Blueprint, File.ReadAllLines(path));
                localBlueprints[filename] = blueprint;
                blueprintFilePaths[filename] = path;
                Workshop.LogDebug($"Imported blueprint: {filename}");
            }
            else if (extension == ".vbuild")
            {
                Blueprint blueprint = new Blueprint(filename, FileType.VBuild, File.ReadAllLines(path));
                localBlueprints[filename] = blueprint;
                blueprintFilePaths[filename] = path;
                Workshop.LogDebug($"Imported vbuild: {filename}");
            }
            else
            {
                Workshop.LogDebug($"Unknown file: {filename}");
            }
        }
    }

    public static void OnZNetAwake(ZNet net)
    {
        if (net.IsServer())
        {
            // automatically publish local blueprints ??
            foreach (KeyValuePair<string, Blueprint> kvp in localBlueprints)
            {
                BlueprintRecipe recipe = kvp.Value.ToRecipe();
                recipes[kvp.Key] = recipe;
            }
        }
        else
        {
            foreach (KeyValuePair<string, Blueprint> kvp in localBlueprints)
            {
                TempBlueprint temp = kvp.Value.ToTemp();
                temps[kvp.Key] = temp;
            }
        
            sync.ValueChanged += OnSyncChanged;
        }
    }
    
    public static void Load(PieceTable table, bool postProcess = false)
    {
        foreach (KeyValuePair<string, BlueprintRecipe> kvp in recipes)
        {
            GameObject container = kvp.Value.Load();
            if (container == null) continue;
            if (postProcess) kvp.Value.PostProcess();
            table.m_pieces.Add(container);
        }

        foreach (KeyValuePair<string, TempBlueprint> kvp in temps)
        {
            GameObject container = kvp.Value.Load();
            if (container == null) continue;
            if (postProcess) kvp.Value.PostProcess();
            table.m_pieces.Add(container);
        }
    }
    public static bool IsBlueprint(this Piece piece) => piece.GetComponent<PlanContainer>();
    public static bool IsBlueprint(this GameObject go) => go.GetComponent<PlanContainer>();
}