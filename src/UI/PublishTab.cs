using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public class PublishTab : Tab, OnHideTextReceiver
{
    public static PublishTab instance;
    private string currentPrice = "";
    private PieceRequirements currentRequirements;
    private TempBlueprint selectedBlueprint;
    public bool isTyping;
    private float confirmTimer;
    private bool confirming;
    private const float confirmLengthSeconds = 5f;
    
    public PublishTab(InventoryGui gui, int index = 0) : base(gui, "PublishTab", "$label_publish", index)
    {
        instance = this;
        SetGamepadHint("$hint_publish");
        craftLabel = "$label_publish";
        craftTooltip = "$hint_publish";
        craftingLabel = "$label_publishing";
        isTableTab = true;
    }

    protected override void Reset()
    {
        currentPrice = "";
        currentRequirements = null;
        confirming = false;
        confirmTimer = 0f;
        selectedBlueprint = null;
        base.Reset();
    }

    public override void OnCancel(InventoryGui gui)
    {
        base.OnCancel(gui);
        confirming = false;
        confirmTimer = 0f;
        currentPrice = "";
        currentRequirements = null;
        gui.UpdateCraftingPanel();
        if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_publish_canceled");
    }

    public override void OnCraft(InventoryGui gui)
    {
        if (currentRequirements != null)
        {
            confirming = true;
            confirmTimer = 0f;
            gui.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
        }
        else
        {
            TextInput.instance.RequestText(this, "$label_set_price", 100);
            isTyping = true;
        }
    }

    public override bool SetupCraftingPanel(InventoryGui gui, Player player, bool focusView)
    {
        if (currentStation == null) return false;
        base.SetupCraftingPanel(gui, player, focusView);
        
        gui.m_craftButton.interactable = false;
        gui.m_craftProgressPanel.gameObject.SetActive(false);
        gui.m_craftButton.gameObject.SetActive(true);
        EnableItemCraftType(gui, false);
        EnableVariantButton(gui, false);
        EnableMinStationLevelIcon(gui, false);
        HideRequirements(gui);
        SetupBlueprintList(gui);
        gui.m_recipeIcon.enabled = false;
        gui.m_recipeName.enabled = false;
        gui.m_recipeDecription.enabled = false;

        if (gui.m_availableRecipes.Count > 0)
        {
            gui.m_availableRecipes.First().InterfaceElement.GetComponentInChildren<Button>().onClick.Invoke();
        }
        return true;
    }

    public override bool UpdateRecipe(InventoryGui gui, Player player, float dt)
    {
        if (BlueprintMan.localBlueprints.Count <= 0 || 
            currentStation == null || 
            button.interactable) return false;

        if (confirming && currentRequirements != null && selectedBlueprint != null)
        {
            confirmTimer += dt;
            gui.m_craftProgressPanel.gameObject.SetActive(true);
            gui.m_craftButton.gameObject.SetActive(false);
            gui.m_craftProgressBar.SetMaxValue(confirmLengthSeconds);
            gui.m_craftProgressBar.SetValue(confirmTimer);
            if (confirmTimer > confirmLengthSeconds)
            {
                confirmTimer = 0f;
                confirming = false;
                selectedBlueprint.settings.requirements.Clear();
                selectedBlueprint.settings.requirements.AddRange(currentRequirements.Requirements);
                selectedBlueprint.blueprint.Format(selectedBlueprint.settings);
                SendToServer(player, selectedBlueprint);
                currentRequirements = null;
                currentPrice = "";
                selectedBlueprint = null;
                gui.m_craftButton.interactable = false;
                HideRequirements(gui);
                gui.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                gui.UpdateCraftingPanel();
            }
        }
        return true;
    }

    private void SetupBlueprintList(InventoryGui gui)
    {
        ClearList(gui);

        List<TempBlueprint> temps = BlueprintMan.temps.Values.ToList();
        for (int i = 0; i < temps.Count; ++i)
        {
            TempBlueprint temp = temps[i];
            AddTemp(gui, temp, i);
        }
        ResizeList(gui);
    }

    private void AddTemp(InventoryGui gui, TempBlueprint blueprint, int idx)
    {
        GameObject element = CreateListElement(gui, idx, 
            out Image icon, 
            out TMP_Text name, 
            out GuiBar durability, 
            out TMP_Text quality,
            out Button elementButton);

        icon.sprite = BuildTools.BlueprintIcon;
        icon.color = Color.white;
        name.text = blueprint.settings.Name;
        name.color = Color.white;
        durability.gameObject.SetActive(false);
        quality.gameObject.SetActive(false);
        elementButton.onClick.AddListener(() =>
        {
            SetElement(gui, idx, true);
            OnTempSelected(gui, blueprint);
        });
        InventoryGui.RecipeDataPair pair = new InventoryGui.RecipeDataPair
        {
            InterfaceElement = element
        };
        gui.m_availableRecipes.Add(pair);
    }

    private void OnTempSelected(InventoryGui gui, TempBlueprint temp)
    {
        Preview.UpdateBlueprintPreview(temp);
        Preview.EnableBlueprintPreview(gui, true);
        selectedBlueprint = temp;
        PieceRequirements requirements = new PieceRequirements(temp.settings.requirements);
        currentPrice = requirements.ToCustomString();
        currentRequirements = null;

        if (BlueprintMan.recipes.ContainsKey(temp.settings.filename))
        {
            SetCraftButtonLabel("Update Price");
        }
        else
        {
            SetCraftButtonLabel("$label_set_price");
        }
        SetCraftButtonTooltip("$tooltip_input_price");

        gui.m_craftButton.interactable = true;
        SetupRequirementList(gui, requirements.ToPieceRequirement());
        SetMinStationLevelIcon(gui, 0, defaultMinStationLevelIconColor, defaultMinStationLevelIcon);
    }
    
    public string GetText() => currentPrice;

    public void SetText(string text)
    {
        currentPrice = text;
        isTyping = false;
        
        PieceRequirements update = new PieceRequirements(text);
        currentRequirements = update;
        craftButtonLabel.text = Localization.instance.Localize(craftLabel);
        craftButtonTooltip.m_text = Localization.instance.Localize(craftTooltip);
        SetupRequirementList(InventoryGui.instance, currentRequirements.ToPieceRequirement());
    }

    public void OnHide()
    {
        isTyping = false;
    }
    
    private static void SendToServer(Player player, TempBlueprint temp)
    {
        Marketplace.SendBlueprintToServer(temp);
        player.Message(MessageHud.MessageType.Center, "$msg_sent_blueprint_to_server");
    }
}