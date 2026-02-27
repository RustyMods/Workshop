using System;
using UnityEngine;

namespace Workshop;

public class GhostHammer : IHammer
{
    public GhostHammer(string sharedName, CraftingStation station) : base(sharedName, station)
    {
        GameObject asset = AssetBundleManager.LoadAsset<GameObject>("buildtoolbundle", "BlueprintHammer_RS");
        ItemDrop hammer = PrefabManager.GetPrefab("Hammer").GetComponent<ItemDrop>();
        item = asset.GetComponent<ItemDrop>();
        item.m_itemData.m_shared.m_triggerEffect = hammer.m_itemData.m_shared.m_triggerEffect;
        table = new GameObject("_PlanHammerPieceTable", typeof(PieceTable)).GetComponent<PieceTable>();
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
    }

    public override void OnRecipeChange(object sender, EventArgs e)
    {
        recipe.m_resources = ConfigManager.PlanHammerRecipe;
    }
}