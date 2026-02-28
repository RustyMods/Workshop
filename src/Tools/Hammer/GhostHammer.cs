using System;
using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public class GhostHammer : IHammer
{
    public static GhostHammer tool;
    private readonly List<GameObject> vanillaPieces = new();
    private readonly List<GameObject> unknownPieces = new();
    private readonly List<Piece> unknownPieces2 = new();
    public GhostHammer(string sharedName, CraftingStation station) : base(sharedName, station)
    {
        tool = this;
        GameObject asset = AssetBundleManager.LoadAsset<GameObject>("buildtoolbundle", "BlueprintHammer_RS");
        ItemDrop hammer = PrefabManager.GetPrefab("Hammer").GetComponent<ItemDrop>();
        item = asset.GetComponent<ItemDrop>();
        item.m_itemData.m_shared.m_triggerEffect = hammer.m_itemData.m_shared.m_triggerEffect;
        table = new GameObject("_PlanHammerPieceTable_RS").AddComponent<PieceTable>();
        table.transform.SetParent(MockManager.transform);
        table.m_pieces.AddRange(hammer.m_itemData.m_shared.m_buildPieces.m_pieces);
        table.m_categories.AddRange(hammer.m_itemData.m_shared.m_buildPieces.m_categories);
        table.m_categoryLabels.AddRange(hammer.m_itemData.m_shared.m_buildPieces.m_categoryLabels);
        table.m_skill = Skills.SkillType.None;
        item.m_itemData.m_shared.m_buildPieces = table;
        item.m_itemData.m_shared.m_useDurability = false;
        item.m_itemData.m_shared.m_name = sharedName;
        item.m_itemData.m_shared.m_description = sharedName + "_desc";
        Recipe hammerRecipe = PrefabManager.GetRecipe(hammer.m_itemData);
        recipe = ScriptableObject.CreateInstance<Recipe>();
        recipe.m_item = item;
        recipe.m_amount = 1;
        recipe.m_enabled = true;
        recipe.m_qualityResultAmountMultiplier = hammerRecipe.m_qualityResultAmountMultiplier;
        recipe.m_listSortWeight = hammerRecipe.m_listSortWeight;
        recipe.m_craftingStation = station;
        recipe.m_repairStation = station;
        recipe.m_minStationLevel = 1;
        recipe.m_requireOnlyOneIngredient = false;
        recipe.m_resources = ConfigManager.PlanHammerRecipe;
        recipe.Register();
        PrefabManager.RegisterPrefab(asset);
        ConfigManager._ghostHammerRecipe.SettingChanged += OnRecipeChange;
        
        vanillaPieces.AddRange(hammer.m_itemData.m_shared.m_buildPieces.m_pieces);
        vanillaPieces.AddRange(PrefabManager.GetPrefab("Cultivator").GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces);
        vanillaPieces.AddRange(PrefabManager.GetPrefab("Hoe").GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces);
        vanillaPieces.AddRange(PrefabManager.GetPrefab("Feaster").GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces);
    }

    public override void AddPiece(GameObject prefab)
    {
        if (prefab.GetComponent<ConstructionWard>() || 
            prefab.GetComponent<TerrainFlag>() || 
            prefab.name == "piece_blueprint_bench_RS" || 
            prefab.name.EndsWith("_planned")) return;

        if (!vanillaPieces.Contains(prefab))
        {
            unknownPieces.Add(prefab);
            if (prefab.TryGetComponent(out Piece component)) unknownPieces2.Add(component);
        }
        base.AddPiece(prefab);
    }

    public bool IsUnknownPiece(Piece piece) => unknownPieces2.Contains(piece);

    public override void OnRecipeChange(object sender, EventArgs e)
    {
        recipe.m_resources = ConfigManager.PlanHammerRecipe;
    }
}