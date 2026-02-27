using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workshop;

public static class BlueprintTable
{
    public const string SHARED_NAME = "$piece_blueprint_table";
    public static bool IsBlueprintTable(this Piece piece) => piece != null && piece.m_name.Equals(SHARED_NAME);
    public static CraftingStation CRAFTING_STATION;
    public static Piece PIECE;
    
    public static GameObject Setup(out CraftingStation station)
    {
        GameObject workbench = PrefabManager.GetPrefab("piece_workbench");
        GuidePoint workBenchGuide = workbench.GetComponentInChildren<GuidePoint>();
        GameObject forge = PrefabManager.GetPrefab("forge");
        Piece forgePiece = forge.GetComponent<Piece>();
        WearNTear forgeWear = forge.GetComponent<WearNTear>();
        CraftingStation forgeStation = forge.GetComponent<CraftingStation>();
        CircleProjector forgeProjector = forge.GetComponentInChildren<CircleProjector>();
        
        GameObject asset = AssetBundleManager.LoadAsset<GameObject>("buildtoolbundle", "piece_blueprint_bench_RS");
        PIECE = asset.GetComponent<Piece>();
        PIECE.m_placeEffect = forgePiece.m_placeEffect;
        PIECE.m_name = SHARED_NAME;
        PIECE.m_description = SHARED_NAME + "_desc";
        PIECE.m_resources = ConfigManager.BlueprintTableRecipe;
        WearNTear wear = asset.GetComponent<WearNTear>();
        wear.m_destroyedEffect = forgeWear.m_destroyedEffect;
        wear.m_hitEffect = forgeWear.m_hitEffect;
        station = asset.GetComponent<CraftingStation>();
        station.m_craftItemEffects = forgeStation.m_craftItemEffects;
        station.m_craftItemDoneEffects = forgeStation.m_craftItemDoneEffects;
        station.m_repairItemDoneEffects = forgeStation.m_repairItemDoneEffects;
        CircleProjector projector = asset.GetComponentInChildren<CircleProjector>();
        projector.m_prefab = forgeProjector.m_prefab;
        Renderer renderer = projector.GetComponentInChildren<Renderer>();
        renderer.material.shader = ShaderMan.GetShader("Particles/Standard Unlit2", renderer.material.shader);
        GuidePoint guide = asset.GetComponentInChildren<GuidePoint>();
        guide.m_ravenPrefab = workBenchGuide.m_ravenPrefab;
        PrefabManager.RegisterPrefab(asset);
        return asset;
    }

    public static void OnRecipeChange(object sender, EventArgs args)
    {
        Piece.Requirement[] recipe = ConfigManager.BlueprintTableRecipe;
        if (PIECE != null)
        {
            PIECE.m_resources = recipe;
        }

        IEnumerable<Piece> instances = Resources.FindObjectsOfTypeAll<Piece>().Where(p => p.m_name == PIECE.m_name);
        foreach (Piece instance in instances)
        {
            instance.m_resources = recipe;
        }
    }
}