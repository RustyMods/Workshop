using System;
using UnityEngine;

namespace Workshop;

public class BlueprintHammer : IHammer
{
    public BlueprintHammer(string sharedName, CraftingStation station) : base(sharedName, station)
    {
        GameObject asset = AssetBundleManager.LoadAsset<GameObject>("buildtoolbundle", "BlueprintBuildHammer_RS");
        ItemDrop hammer = PrefabManager.GetPrefab("Hammer").GetComponent<ItemDrop>();
        item = asset.GetComponent<ItemDrop>();
        item.m_itemData.m_shared.m_triggerEffect = hammer.m_itemData.m_shared.m_triggerEffect;
        table = new GameObject("_BlueprintHammerPieceTable", typeof(PieceTable)).GetComponent<PieceTable>();

        Piece.PieceCategory blueprint = PieceTableMan.GetCategory("Blueprints");
        
        table.transform.SetParent(MockManager.transform);
        table.m_categories.Add(Piece.PieceCategory.Misc);
        table.m_categories.Add(Piece.PieceCategory.Crafting);
        table.m_categories.Add(blueprint);
        table.m_categoryLabels.Add(hammer.m_itemData.m_shared.m_buildPieces.m_categoryLabels[0]);
        table.m_categoryLabels.Add(hammer.m_itemData.m_shared.m_buildPieces.m_categoryLabels[1]);
        table.m_categoryLabels.Add("$piece_category_blueprint");
        table.m_skill = Skills.SkillType.None;

        int max = PieceTableMan.GetMaxCategories();
        table.m_selectedPiece = new Vector2Int[max];
        table.m_lastSelectedPiece = new Vector2Int[max];
        
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
        recipe.m_resources = ConfigManager.GhostHammerRecipe;
        recipe.Register();
        PrefabManager.RegisterPrefab(asset);
        ConfigManager._planHammerRecipe.SettingChanged += OnRecipeChange;
    }

    public override void OnRecipeChange(object sender, EventArgs e)
    {
        recipe.m_resources = ConfigManager.GhostHammerRecipe;
    }
}