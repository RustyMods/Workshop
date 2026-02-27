using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public class RevenueTab : Tab
{
    private BlueprintRecipe selectedRecipe;
    private bool collecting;
    private float collectingTimer;
    private const float confirmLengthSeconds = 5f;
    public static bool canCollect = true;
    private List<KeyValuePair<BlueprintRecipe, int>> revenue;
    private static readonly Tutorial RevenueDescription = new Tutorial("Revenue", "Revenue.md");
    
    public RevenueTab(InventoryGui gui, int index = 0) : base(gui, "RevenueTab", "$label_revenue", index)
    {
        SetGamepadHint("$hint_collect_revenue");
        craftLabel = "$label_collect";
        craftTooltip = "$tooltip_collect";
        craftingLabel = "$label_collecting";
        isTableTab = true;
    }
    
    public override void OnCraft(InventoryGui gui)
    {
        collecting = true;
        collectingTimer = 0f;
        gui.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
    }

    public override void OnCancel(InventoryGui gui)
    {
        base.OnCancel(gui);
        collecting = false;
        collectingTimer = 0f;
        selectedRecipe = null;
    }

    protected override void Reset()
    {
        selectedRecipe = null;
        collecting = false;
        collectingTimer = 0f;
        base.Reset();
    }

    private void UpdateSelectedRecipe(InventoryGui gui, BlueprintRecipe recipe)
    {
        Preview.EnableBlueprintPreview(gui, true);
        craftButtonLabel.text = Localization.instance.Localize(craftLabel);
        Preview.UpdateBlueprintPreview(recipe);
        SetupRequirementList(gui, recipe.m_resources);
        SetMinStationLevelIcon(gui, 0, defaultMinStationLevelIconColor, defaultMinStationLevelIcon);
    }

    private void BaseUpdateCollect(InventoryGui gui, float dt)
    {
        collectingTimer += dt;
        gui.m_craftProgressPanel.gameObject.SetActive(true);
        gui.m_craftButton.gameObject.SetActive(false);
        gui.m_craftProgressBar.SetMaxValue(confirmLengthSeconds);
        gui.m_craftProgressBar.SetValue(collectingTimer);
    }

    private void UpdateCollect(InventoryGui gui, Player player, BlueprintRecipe recipe, float dt)
    {
        BaseUpdateCollect(gui, dt);
        if (collectingTimer > confirmLengthSeconds)
        {
            collectingTimer = 0f;
            collecting = false;
            selectedRecipe = null;
            revenue = null;
            canCollect = ZNet.instance.IsServer();
            
            Marketplace.CollectRevenue(player, recipe.m_resources);
            Marketplace.SendCollectedNotice(player, recipe, 1);
            gui.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
            gui.UpdateCraftingPanel();
        }
    }

    private void UpdateCollectAll(InventoryGui gui, Player player, List<KeyValuePair<BlueprintRecipe, int>> list, float dt)
    {
        BaseUpdateCollect(gui, dt);
        if (collectingTimer > confirmLengthSeconds)
        {
            collectingTimer = 0f;
            collecting = false;
            selectedRecipe = null;
            canCollect = ZNet.instance.IsServer(); // if server, does not need to wait for sync
            for (int i = 0; i < list.Count; ++i)
            {
                KeyValuePair<BlueprintRecipe, int> pair = list[i];
                Marketplace.CollectRevenue(player, pair.Key.m_resources, pair.Value);
                Marketplace.SendCollectedNotice(player, pair.Key, pair.Value);
            }
            revenue = null;
            gui.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
            gui.UpdateCraftingPanel();
        }
    }

    public override bool SetupCraftingPanel(InventoryGui gui, Player player, bool focusView)
    {
        if (currentStation == null) return false;
        base.SetupCraftingPanel(gui, player, focusView);
        
        HideRequirements(gui);

        gui.m_craftProgressPanel.gameObject.SetActive(false);
        gui.m_craftButton.gameObject.SetActive(true);
        gui.m_recipeIcon.sprite = null;
        gui.m_recipeName.text = Localization.instance.Localize("$label_purchase_ledger");
        gui.m_recipeDecription.text = RevenueDescription.tooltip;
        
        Marketplace.GetRevenue(player, out revenue);
        gui.m_craftButton.interactable = revenue.Count > 0;

        EnableItemCraftType(gui, false);
        EnableVariantButton(gui, false);
        EnableMinStationLevelIcon(gui, false);
        SetupRevenueList(gui, revenue);
        SetupDescription(gui, currentStation);
        return true;
    }

    public override bool UpdateRecipe(InventoryGui gui, Player player, float dt)
    {
        if (currentStation == null || button.interactable) return false;

        if (!collecting) return true;
        if (selectedRecipe != null)
        {
            UpdateCollect(gui, player, selectedRecipe, dt);
        }
        else if (revenue != null)
        {
            UpdateCollectAll(gui, player, revenue, dt);
        }
        return true;
    }

    private void SetupDescription(InventoryGui gui, CraftingStation station)
    {
        SetStationFields(gui, station.m_name, station.m_icon, station.GetLevel());
        gui.m_recipeDecription.text = RevenueDescription.tooltip;
        gui.m_craftButton.interactable = true;
    }

    private void SetupRevenueList(InventoryGui gui, List<KeyValuePair<BlueprintRecipe, int>> list)
    {
        ClearList(gui);
        if (canCollect)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                KeyValuePair<BlueprintRecipe, int> pair = list[i];
                AddRevenue(gui, pair.Key, pair.Value, i);
            }
        }
        ResizeList(gui);
    }

    private void AddRevenue(InventoryGui gui, BlueprintRecipe recipe, int amount, int idx)
    {
        GameObject element = CreateListElement(gui, idx, 
            out Image icon, 
            out TMP_Text name, 
            out GuiBar durability, 
            out TMP_Text quality,
            out Button elementButton);

        icon.sprite = recipe.m_item.m_itemData.GetIcon();
        icon.color = Color.white;
        name.text = Localization.instance.Localize(recipe.settings.Name);
        name.color = Color.white;
        durability.gameObject.SetActive(false);
        if (amount > 0)
        {
            quality.text = amount.ToString();
            quality.gameObject.SetActive(true);
        }
        else
        {
            quality.gameObject.SetActive(false);
        }
        
        InventoryGui.RecipeDataPair pair = new InventoryGui.RecipeDataPair
        {
            InterfaceElement = element
        };
        
        elementButton.onClick.AddListener(() => OnSelectRecipe(gui, recipe, idx));
        gui.m_availableRecipes.Add(pair);
    }

    private void OnSelectRecipe(InventoryGui gui, BlueprintRecipe recipe, int idx)
    {
        selectedRecipe = recipe;
        gui.m_craftButton.interactable = true;
        UpdateSelectedRecipe(gui, selectedRecipe);
        SetElement(gui, idx, true);
    }
}