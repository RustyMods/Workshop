using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public class StationTab : Tab
{
    public static readonly Tutorial WardDescription = new("Construction", "Ward.md");
    
    public StationTab(InventoryGui gui, int index = 0) : base(gui, "StationTab","$label_stations", index)
    {
        craftingLabel = "$label_hide_connections";
        craftTooltip = "$tooltip_connections";
        isWardTab = true;
    }

    public override void OnCraft(InventoryGui gui)
    {
        if (currentWard == null) return;
        currentWard.ToggleConnectionEffect();
        craftButtonLabel.text = Localization.instance.Localize(currentWard.connectionEnabled ? 
            "$label_hide_connections" : 
            "$label_show_connections");
    }

    public override bool SetupCraftingPanel(InventoryGui gui, Player player, bool focusView)
    {
        if (currentWard == null) return false;
        
        base.SetupCraftingPanel(gui, player, focusView);
        
        SetStationFields(
            gui, 
            ConstructionWard.SHARED_NAME, 
            currentWard.m_piece.m_icon, 
            currentWard.GetConnectedStations().Count);
        
        EnableItemCraftType(gui, false);
        EnableVariantButton(gui, false);
        EnableMinStationLevelIcon(gui, false);
        EnableRecipeRequirementList(false);
        HideRequirements(gui);
        
        gui.m_craftProgressPanel.gameObject.SetActive(false);
        gui.m_craftButton.gameObject.SetActive(true);
        gui.m_craftButton.interactable = true;
        craftButtonLabel.text = Localization.instance.Localize(currentWard.connectionEnabled ? "Hide Connection" : "Show Connection");
        SetupStationList(gui, currentWard);
        SetupDescription(gui, currentWard);
        
        return true;
    }

    public override bool UpdateRecipe(InventoryGui gui, Player player, float dt)
    {
        return currentWard != null;
    }

    private void SetupDescription(InventoryGui gui, ConstructionWard ward)
    {
        gui.m_recipeDecription.text = WardDescription.tooltip;
        gui.m_recipeIcon.sprite = ward.m_piece.m_icon;
        gui.m_recipeName.text = Localization.instance.Localize(ConstructionWard.SHARED_NAME);
    }

    private void SetupStationList(InventoryGui gui, ConstructionWard ward)
    {
        ClearList(gui);

        List<CraftingStation> stations = ward.GetConnectedStations();
        for (int i = 0; i < stations.Count; ++i)
        {
            CraftingStation station = stations[i];
            AddCraftingStation(gui, station, i);
        }
        
        ResizeList(gui);
    }

    private void AddCraftingStation(InventoryGui gui, CraftingStation station, int idx)
    {
        GameObject element = CreateListElement(gui, idx, 
            out Image icon, 
            out TMP_Text name, 
            out GuiBar durability, 
            out TMP_Text quality,
            out Button elementButton);

        icon.sprite = station.m_icon;
        icon.color = Color.white;
        name.text = Localization.instance.Localize(station.m_name);
        name.color = Color.white;
        durability.gameObject.SetActive(false);
        quality.text = station.GetLevel().ToString();
        quality.color = Color.white;
        elementButton.interactable = true;

        InventoryGui.RecipeDataPair pair = new InventoryGui.RecipeDataPair
        {
            InterfaceElement = element
        };
        
        elementButton.onClick.AddListener(() =>
        {
            SetElement(gui, idx, true);
        });
        
        gui.m_availableRecipes.Add(pair);
    }
}