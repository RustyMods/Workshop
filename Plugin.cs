using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Workshop;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Workshop : BaseUnityPlugin, OnHideTextReceiver
{
    public const string ModName = "Workshop";
    public const string ModVersion = "0.0.1";
    public const string Author = "RustyMods";
    public const string ModGUID = Author + "." + ModName;
    public readonly Harmony _harmony = new(ModGUID);
    public static Workshop instance;
    
    [Header("Saving")]
    private Action<string> OnTextReceived;
    private Func<string> OnGetText;
    public TempBlueprint temp;
    public BlueprintRecipe recipe;
    public BlueprintSettings settings;
    public bool isSaving;
    public string oldFilePath = "";
    public string oldRecipeName = "";
    
    public void Awake()
    {
        instance = this;
        ConfigManager.Start();
        ConstructionWard.Setup();
        BlueprintMan.ReadFiles();
        PieceTableMan.Init();
        PaintMan.Init();
        Commands.Init();
        Assembly assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
    }

    public void Update()
    {
        GhostMan.UpdateToggle();
    }

    public void DelayedTabsHide()
    {
        Tab.DisableAll();
    }

    private void OnDestroy()
    {
        Config.Save();
        Marketplace.Save();
    }

    public static void LogDebug(string msg)
    {
        instance.Logger.LogDebug(msg);
    }

    public static void LogError(string msg)
    {
        instance.Logger.LogError(msg);
    }

    public static void LogWarning(string msg)
    {
        instance.Logger.LogWarning(msg);
    }

    public static void LogInfo(string msg)
    {
        instance.Logger.LogInfo(msg);
    }

    public static void LogFatal(string msg)
    {
        instance.Logger.LogFatal(msg);
    }
    
    public void Save(Piece piece, List<Piece> pieces, BlueprintRecipe existing = null)
    {
        if (isSaving) return;
        if (piece is null || pieces.Count <= 0)
        {
            LogWarning("Failed to save blueprint");
            return;
        }
        isSaving = true;

        if (existing is null)
        {
            temp = new TempBlueprint();
            temp.settings.Parse(piece, pieces);
            temp.settings.filename = piece.name + ".blueprint";
            settings = temp.settings;
            OnGetText = () => settings.Name;
            OnTextReceived = text =>
            {
                settings.Name = text;
                settings.filename = text.ToLower().Replace(" ", "_") + ".blueprint";
                Invoke(nameof(SaveDescription), 0.5f);
            };
            
            TextInput.instance.RequestText(this, "Set Name", 100);
        }
        else
        {
            recipe = existing;
            settings = recipe.settings;
            OnGetText = () => settings.Name;
            OnTextReceived = text =>
            {
                settings.Name = text;
                Invoke(nameof(SaveDescription), 0.5f);
            };
            TextInput.instance.RequestText(this, "Update Name", 100);
        }
    }

    public void SaveDescription()
    {
        OnGetText = () => settings.Description;
        TextInput.instance.RequestText(this, recipe != null ? 
            "Update Description" : 
            "Set Description", 100);
        OnTextReceived = desc =>
        {
            settings.Description = desc;
            OnTextReceived = null;
            OnGetText = null;
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, recipe != null ? 
                $"Updated blueprint: {settings.filename}" : 
                $"Saved blueprint: {settings.filename}");
            Write();
        };
    }

    public void Write()
    {
        Blueprint blueprint = new Blueprint();
        blueprint.Format(settings);
        blueprint.filename = settings.filename;
        blueprint.type = FileType.Blueprint;
        blueprint.Write();

        BlueprintMan.localBlueprints[settings.filename] = blueprint;

        if (recipe != null)
        {
            if (recipe.Loaded)
            {
                recipe.blueprint = blueprint;
                recipe.piece.m_name = settings.Name;
                recipe.piece.m_description = settings.Description;
                recipe.m_item.m_itemData.m_shared.m_name = settings.Name;
            }
            else
            {
                LogWarning($"Failed to update blueprint: {recipe.name}");
            }
        }
        else
        {
            temp.blueprint = blueprint;
            GameObject prefab = temp.Load();
            if (prefab != null)
            {
                temp.PostProcess();
                BuildTools.planHammer.InsertPiece(prefab);
                BlueprintMan.temps[settings.filename] = temp;
            }
            else
            {
                LogWarning("Failed to load temp blueprint");
            }
        }

        settings = null;
        recipe = null;
        temp = null;
        isSaving = false;
    }

    public string GetText() => OnGetText?.Invoke() ?? "";

    public void SetText(string text) => OnTextReceived?.Invoke(text);
    
    public void OnHide()
    {
        if (isSaving) LogDebug("Cancelled blueprint save");
        recipe = null;
        temp = null;
        settings = null;
        isSaving = false;
    }
}