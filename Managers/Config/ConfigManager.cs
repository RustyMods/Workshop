using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Workshop;

public enum Toggle
{
    On = 1,
    Off = 0
}

public static class ConfigManager
{
    private const string ConfigFileName = Workshop.ModGUID + ".cfg";
    private static readonly string ConfigFileFullPath;
    public static readonly ConfigSync ConfigSync;
    private static readonly ISerializer serializer;
    private static readonly IDeserializer deserializer;
    public static readonly string ConfigFolderPath;
    public static object configManager;

    static ConfigManager()
    {
        ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        ConfigSync = new ConfigSync(Workshop.ModGUID)
        { 
            DisplayName = Workshop.ModName, 
            CurrentVersion = Workshop.ModVersion, 
            MinimumRequiredVersion = Workshop.ModVersion 
        };
        serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
            .DisableAliases()
            .Build();
        deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        ConfigFolderPath = Path.Combine(Paths.ConfigPath, Workshop.ModName);
    }
    
    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    private static ConfigEntry<float> _buildInterval = null!;
    private static ConfigEntry<float> _buildRange = null!;
    private static ConfigEntry<float> _ghostTransparency = null!;
    private static ConfigEntry<Toggle> _placeEffect = null!;
    private static ConfigEntry<Color> _highlightColor = null!;
    private static ConfigEntry<float> _placementIncrement = null!;
    private static ConfigEntry<KeyCode> _saveKeyCode = null!;
    private static ConfigEntry<KeyCode> _toggleGhost = null!;
    private static ConfigEntry<float> _maxStationRange = null!;
    private static ConfigEntry<KeyCode> _resetManualSnap = null!;
    private static ConfigEntry<float> _maxCameraDistance = null!;
    private static ConfigEntry<Color> _connectionMinColor = null!;
    private static ConfigEntry<Color> _connectionMaxColor = null!;
    private static ConfigEntry<Toggle> _connectionEnabled = null!;
    private static ConfigEntry<string> _wardRequirements = null!;
    private static ConfigEntry<string> _blueprintTableRequirements = null!;
    private static ConfigEntry<Color> _wardEmissionColor = null!;
    private static ConfigEntry<float> _wardEmissionIntensity = null!;
    public static ConfigEntry<string> _planHammerRecipe = null!;
    public static ConfigEntry<string> _ghostHammerRecipe = null!;
    private static ConfigEntry<Color> _ghostTint = null!;
    private static ConfigEntry<float> _fresnelPower = null!;
    private static ConfigEntry<float> _ghostPower = null!;
    
    public static float BuildInterval => _buildInterval.Value;
    public static float BuildRange => _buildRange.Value;
    public static float GhostTransparency => _ghostTransparency.Value;
    public static bool UsePlaceEffects => _placeEffect.Value is Toggle.On;
    public static Color HighlightColor => _highlightColor.Value;
    public static float PlacementIncrement => _placementIncrement.Value;
    public static KeyCode SaveKeyCode => _saveKeyCode.Value;
    public static KeyCode ToggleGhost => _toggleGhost.Value;
    public static float MaxCraftingStationRange => _maxStationRange.Value;
    public static KeyCode ResetManualSnapKey => _resetManualSnap.Value;
    public static float MaxCameraDistance => _maxCameraDistance.Value;
    public static Color ConnectionMinColor => _connectionMinColor.Value;
    public static Color ConnectionMaxColor => _connectionMaxColor.Value;
    public static bool ConnectionEnabled => _connectionEnabled.Value is Toggle.On;
    public static Piece.Requirement[] WardRecipe => new PieceRequirements(_wardRequirements.Value).ToPieceRequirement();
    public static Piece.Requirement[] BlueprintTableRecipe => new PieceRequirements(_blueprintTableRequirements.Value).ToPieceRequirement();
    public static Color WardEmissionColor => _wardEmissionColor.Value;
    public static float WardEmissionIntensity => _wardEmissionIntensity.Value;
    public static Piece.Requirement[] PlanHammerRecipe => new PieceRequirements(_planHammerRecipe.Value).ToPieceRequirement();
    public static Piece.Requirement[] GhostHammerRecipe => new PieceRequirements(_ghostHammerRecipe.Value).ToPieceRequirement();
    public static Color GhostTint => _ghostTint.Value;
    public static float FresnelPower => _fresnelPower.Value;
    public static float GhostPower => _ghostPower.Value;
    
    public static void Start()
    {
        _serverConfigLocked = config("1 - General", 
            "Lock Configuration", 
            Toggle.On,
            "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        _buildInterval = config("2 - Settings", 
            "Build Interval", 
            0.05f, 
            "Set interval between piece placement");
        
        _buildRange = config("3 - Construction Ward", 
            "Build Range", 
            20f, 
            "Set construction ward build range");
        
        _ghostTransparency = config("2 - Settings", 
            "Piece Opacity",
            0.2f,
            new ConfigDescription("Set ghost piece transparency", new AcceptableValueRange<float>(0f, 1f)));
        _ghostTransparency.SettingChanged += (_, _) => GhostMan.UpdateMaterials();

        _ghostPower = config("2 - Settings", "Ghost Power", 0.5f,
            new ConfigDescription("Set ghost material influence", new AcceptableValueRange<float>(0f, 1f)));
        _ghostPower.SettingChanged += (_, _) => GhostMan.UpdateMaterials();
        
        _ghostTint = config("2 - Settings",
            "Ghost Tint", 
            new Color(0f, 0.3f, 0.6f, 1f), 
            "Set ghost piece tint color");
        _ghostTint.SettingChanged += (_, _) => GhostMan.UpdateMaterials();

        _fresnelPower = config("2 - Settings",
            "Fresnel",
            0.7f,
            "Set ghost piece rim brightness");
        _fresnelPower.SettingChanged += (_, _) => GhostMan.UpdateMaterials();
        
        _placeEffect = config("2 - Settings", 
            "Place Effect", 
            Toggle.On,
            "If on, Plan pieces will create effect when placed by construction ward");
        
        _highlightColor = config("2 - Settings", 
            "Highlight Color", 
            new Color(1f, 0.92f, 0.016f, 1f), 
            "Set highlight color when selecting many", 
            false);
        
        _placementIncrement = config("2 - Settings", 
            "Placement Increment", 
            0.05f,
            "Set placement increment, when triggering modifier using arrow keys", 
            false);
        
        _saveKeyCode = config("2 - Settings", 
            "Save Key", 
            KeyCode.F3,
            "Set keycode to save current selection as blueprint", 
            false);
        
        _toggleGhost = config("2 - Settings", 
            "Toggle Ghost", 
            KeyCode.F4, 
            "Set keycode to toggle ghost material", 
            false);

        _maxStationRange = config("3 - Construction Ward",
            "Ward Station Range",
            20f,
            "Max crafting station range from construction ward");
        
        _resetManualSnap = config("2 - Settings",
            "Reset SnapPoint", 
            KeyCode.Tab,
            "Set manual snap point back to auto",
            false);

        _maxCameraDistance = config("2 - Settings",
            "Max Camera Distance",
            100f,
            "Set max camera distance when using Plan Tools",
            false);

        _connectionMinColor = config("3 - Construction Ward",
            "Connection Min Color",
            new Color(1f, 0.8578507f, 0.09433961f, 1f),
            "Set station connection start color");
        _connectionMinColor.SettingChanged += StationConnection.OnColorChange;
        
        _connectionMaxColor = config("3 - Construction Ward",
            "Connection Max Color",
            new Color(1f, 0.2837479f, 0.04705882f, 1f),
            "Set station connection start color");
        _connectionMaxColor.SettingChanged += StationConnection.OnColorChange;

        _connectionEnabled = config("3 - Construction Ward",
            "Station Connection Particles", 
            Toggle.On,
            "If on, particles will connection station with construction ward", 
            false);
        _connectionEnabled.SettingChanged += StationConnection.OnToggle;

        _wardRequirements = config("3 - Construction Ward", 
            "Requirements", 
            new PieceRequirements(new Requirement("Stone", 10), new Requirement("FineWood", 5), new Requirement("SurtlingCore", 2)).ToString(),
            new ConfigDescription("Define construction ward build requirements", 
                null, 
                PieceRequirements.attributes));
        _wardRequirements.SettingChanged += ConstructionWard.OnRecipeChange;
        
        _blueprintTableRequirements = config("4 - Blueprint Table", 
            "Requirements", 
            new PieceRequirements(new Requirement("Wood", 10), new Requirement("FineWood", 5), new Requirement("Iron", 2)).ToString(),
            new ConfigDescription("Define blueprint table build requirements", 
                null, 
                PieceRequirements.attributes));
        _blueprintTableRequirements.SettingChanged += BlueprintTable.OnRecipeChange;

        _wardEmissionColor = config("3 - Construction Ward",
            "Emission Color", 
            new Color(0.934f, 0.57213f, 0.1848674f, 1f), 
            "Set construction ward emission color");
        _wardEmissionColor.SettingChanged += ConstructionWard.OnEmissionColorChange;
        
        _wardEmissionIntensity = config("3 - Construction Ward",
            "Emission Intensity",
            1.3f, "Set construction ward emission intensity");
        _wardEmissionIntensity.SettingChanged += ConstructionWard.OnEmissionColorChange;

        _planHammerRecipe = config("4 - Plan Hammer", "Requirements",
            new PieceRequirements(new Requirement("Wood", 10)).ToString(),
            new ConfigDescription("Set Plan hammer recipe", null, PieceRequirements.attributes));
        
        _ghostHammerRecipe = config("4 - Build Hammer", "Requirements",
            new PieceRequirements(new Requirement("Wood", 10)).ToString(),
            new ConfigDescription("Set Build hammer recipe", null, PieceRequirements.attributes));
        
        SetupWatcher();
    }
    
    public static T Deserialize<T>(string data) => deserializer.Deserialize<T>(data);
    public static string Serialize<T>(T obj)
    {
        if (obj == null) return "";
        using StringWriter sw = new StringWriter();
        serializer.Serialize(sw, obj, obj.GetType());
        return sw.ToString();
    }

    public static void OnFejdStartup()
    {
        Assembly bepinexConfigManager = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");

        Type configManagerType = bepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
        configManager = configManagerType == null
            ? null
            : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(configManagerType);
    }

    public static string GetFolderPath()
    {
        if (!Directory.Exists(ConfigFolderPath)) Directory.CreateDirectory(ConfigFolderPath);
        return ConfigFolderPath;
    }
    
    private static void SetupWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private static void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            Workshop.LogDebug("ReadConfigValues called");
            Workshop.instance.Config.Reload();
        }
        catch
        {
            Workshop.LogError($"There was an issue loading your {ConfigFileName}");
            Workshop.LogError("Please check your config entries for spelling and format!");
        }
    }
    
    private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Workshop.instance.Config.Bind(group, name, value, extendedDescription);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private static ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }
}