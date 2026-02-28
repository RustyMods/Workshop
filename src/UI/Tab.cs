using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public abstract class Tab
{
    public static readonly List<Tab> tabs = new();
    public static Tab currentTab;
    private const float spacing = 107f;
    public static Transform listRoot;
    public static TMP_Text craftButtonLabel;
    public static UITooltip craftButtonTooltip;
    public static TMP_Text progressLabel;
    public static string defaultCraftLabel;
    public static string defaultProgressLabel;
    public static Sprite defaultMinStationLevelIcon;
    public static Color defaultMinStationLevelIconColor;
    public static bool tabCraftEnabled;
    public static bool tabUpgradeEnabled = true;
    public static ConstructionWard currentWard;
    public static CraftingStation currentStation;

    public readonly int index;
    public readonly GameObject tabPrefab;
    public readonly Button button;
    private readonly RectTransform rect;
    public readonly GameObject selected;
    private readonly UIGamePad gamepad;
    private readonly TMP_Text label;
    private readonly TMP_Text selectedLabel;
    private readonly TMP_Text hint;

    public string craftLabel = "";
    public string craftingLabel = "";
    public string craftHint = "";
    public string craftTooltip = "";
    public string tabLabel;
    
    public bool isTableTab = false;
    public bool isWardTab = false;

    private readonly Vector3 basePosition;
    
    public static implicit operator bool(Tab tab) => tab != null;

    protected Tab(InventoryGui gui, string name, string tabLabel, int index = 0)
    {
        this.index = index;
        GameObject craftTab = gui.m_tabCraft.gameObject;
        tabPrefab = Object.Instantiate(craftTab, craftTab.transform.parent);
        button = tabPrefab.GetComponent<Button>();
        rect = tabPrefab.GetComponent<RectTransform>();
        button.onClick.RemoveAllListeners();
        ButtonSfx sfx = tabPrefab.GetComponent<ButtonSfx>();
        sfx.Start();
        button.interactable = true;
        tabPrefab.name = name;
        button.onClick.AddListener(OnClick);
        gamepad = tabPrefab.GetComponent<UIGamePad>();
        label = rect.Find("Text").GetComponent<TMP_Text>();
        selected = rect.Find("Selected").gameObject;
        selectedLabel = rect.Find("Selected/Text (1)").GetComponent<TMP_Text>();
        hint = rect.Find("gamepad_hint/Text").GetComponent<TMP_Text>();
        basePosition = rect.localPosition;
        this.tabLabel = tabLabel;
        SetLabel(tabLabel);
        PlaceTab();
        tabs.Add(this);
    }
    
    private void PlaceTab(bool craftTabVisible = true, bool upgradeTabVisible = true)
    {
        int num = craftTabVisible ? 0 : 1;
        num += upgradeTabVisible ? 0 : 1;
        
        rect.localPosition = new Vector3(
            basePosition.x + spacing * (index - num), 
            basePosition.y, 
            basePosition.z);
    }

    protected virtual void OnClick()
    {
        currentTab = this;
        for (int i = 0; i < tabs.Count; ++i)
        {
            Tab tab = tabs[i];
            tab.button.interactable = tab != this;
        }
        
        InventoryGui.instance.m_tabCraft.interactable = true;
        InventoryGui.instance.m_tabUpgrade.interactable = true;
        InventoryGui.instance.UpdateCraftingPanel();
    }
    
    public bool InTab() => !button.interactable;

    private void Disable()
    {
        if (tabPrefab == null) return;
        tabPrefab.SetActive(false);
    }
    protected virtual void Reset()
    {
        button.interactable = true;
    }
    public virtual void OnCraft(InventoryGui gui)
    {
        gui.m_craftTimer = 0.0f;
        gui.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
    }
    public virtual void OnCancel(InventoryGui gui)
    {
        if (gui.m_craftTimer < 0.0f) return;
        gui.m_craftTimer = -1f;
    }
    public virtual bool SetupCraftingPanel(InventoryGui gui, Player player, bool focusView)
    {
        SetCraftButtonLabel(craftLabel);
        SetCraftButtonTooltip(craftTooltip);
        SetProgressLabel(craftLabel);
        SetBaseTabs(gui, player);
        return true;
    }
    public virtual bool UpdateRecipe(InventoryGui gui, Player player, float dt) => false;

    protected void SetGamepadHint(string text) => hint.text = Localization.instance.Localize(text);
    
    public void SetGamepadKey(string key) => gamepad.m_zinputKey = key;
    
    protected void SetLabel(string text)
    {
        string localized = Localization.instance.Localize(text).ToUpper();
        label.text = localized;
        selectedLabel.text = localized;
    }
    public virtual void SetElement(InventoryGui gui, int idx, bool center)
    {
        for (int i = 0; i < gui.m_availableRecipes.Count; ++i)
        {
            InventoryGui.RecipeDataPair element = gui.m_availableRecipes[i];
            GameObject select = element.InterfaceElement.transform.Find("selected").gameObject;
            select.SetActive(idx == i);
        }

        if (center && idx >= 0)
        {
            gui.m_recipeEnsureVisible.CenterOnItem(gui.m_availableRecipes[idx].InterfaceElement.transform as RectTransform);
        }
    }

    public virtual void OnInventoryGuiHide()
    {
    }

    public static void SetBaseTabs(InventoryGui gui, Player player, bool craft = true, bool upgrade = true)
    {
        gui.m_tabCraft.gameObject.SetActive(craft);
        bool shouldUpgradeBeVisible = player.GetCurrentCraftingStation() ||
                                      player.NoCostCheat() ||
                                      ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost);
        gui.m_tabUpgrade.gameObject.SetActive(shouldUpgradeBeVisible && upgrade);
    }
    
    public static void SetCraftButtonLabel(string text) => craftButtonLabel.text = Localization.instance.Localize(text);

    public static void SetCraftButtonTooltip(string text) =>
        craftButtonTooltip.m_text = Localization.instance.Localize(text);

    public static void SetProgressLabel(string text) => progressLabel.text = Localization.instance.Localize(text);
    
    public static void UpdateTabPlacement(bool craftTabVisible, bool upgradeTabVisible)
    {
        for (int i = 0; i < tabs.Count; ++i)
        {
            Tab tab = tabs[i];
            tab.PlaceTab(craftTabVisible, upgradeTabVisible);
        }
    }

    protected static GameObject CreateListElement(InventoryGui gui, int index, 
        out Image icon, 
        out TMP_Text name, 
        out GuiBar durability,
        out TMP_Text quality, 
        out Button elementButton)
    {
        GameObject element = Object.Instantiate(gui.m_recipeElementPrefab, gui.m_recipeListRoot);
        element.SetActive(true);
        if (element.transform is RectTransform rect)
        {
            rect.anchoredPosition = new Vector2(0.0f, index * -gui.m_recipeListSpace);
        }
        icon = element.transform.Find("icon").GetComponent<Image>();
        name = element.transform.Find("name").GetComponent<TMP_Text>();
        durability = element.transform.Find("Durability").GetComponent<GuiBar>();
        quality = element.transform.Find("QualityLevel").GetComponent<TMP_Text>();
        elementButton = element.GetComponent<Button>();

        return element;
    }

    protected static void SetupRequirementList(InventoryGui gui, Piece.Requirement[] requirements)
    {
        for (int i = 0; i < 4; ++i)
        {
            Transform elementRoot = gui.m_recipeRequirementList[i].transform;

            if (i > requirements.Length - 1)
            {
                InventoryGui.HideRequirement(elementRoot);
            }
            else
            {
                Piece.Requirement requirement = requirements[i];
                Image icon = elementRoot.Find("res_icon").GetComponent<Image>();
                TMP_Text name = elementRoot.Find("res_name").GetComponent<TMP_Text>();
                TMP_Text amount = elementRoot.Find("res_amount").GetComponent<TMP_Text>();
                UITooltip tooltip = elementRoot.GetComponent<UITooltip>();
                    
                icon.gameObject.SetActive(true);
                name.gameObject.SetActive(true);
                amount.gameObject.SetActive(true);
                icon.sprite = requirement.m_resItem.m_itemData.GetIcon();
                icon.color = Color.white;
                string localizedName = Localization.instance.Localize(requirement.m_resItem.m_itemData.m_shared.m_name);
                name.text = localizedName;
                tooltip.m_text = localizedName;
                amount.text = requirement.m_amount.ToString();
                amount.color = new Color(1f, 0.5f, 0f, 1f);
            }
        }
    }

    protected static void SetStationFields(InventoryGui gui, string name, Sprite icon, int level)
    {
        gui.m_craftingStationName.text = Localization.instance.Localize(name);
        gui.m_craftingStationIcon.sprite = icon;
        gui.m_craftingStationIcon.gameObject.SetActive(true);
        gui.m_craftingStationLevel.text = level.ToString();
        gui.m_craftingStationLevelRoot.gameObject.SetActive(true);
    }

    protected static void ClearList(InventoryGui gui)
    {
        gui.m_tempRecipes.Clear();
        for (int i = 0; i < gui.m_availableRecipes.Count; ++i)
        {
            InventoryGui.RecipeDataPair element = gui.m_availableRecipes[i];
            Object.Destroy(element.InterfaceElement);
        }
        gui.m_availableRecipes.Clear();
    }

    protected static void ResizeList(InventoryGui gui)
    {
        gui.m_recipeListRoot.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(gui.m_recipeListBaseSize, 
                gui.m_availableRecipes.Count * gui.m_recipeListSpace));
    }

    protected static void EnableRecipeRequirementList(bool enable)
    {
        listRoot.gameObject.SetActive(enable);
    }
    
    protected static void EnableItemCraftType(InventoryGui gui, bool enable) => gui.m_itemCraftType.gameObject.SetActive(enable);
    protected static void EnableVariantButton(InventoryGui gui, bool enable) => gui.m_variantButton.gameObject.SetActive(enable);
    protected static void EnableMinStationLevelIcon(InventoryGui gui, bool enable) => gui.m_minStationLevelIcon.gameObject.SetActive(enable);

    protected static void SetMinStationLevelIcon(InventoryGui gui, int level, Color color, Sprite icon = null)
    {
        gui.m_minStationLevelIcon.gameObject.SetActive(icon != null);
        gui.m_minStationLevelIcon.sprite = icon;
        gui.m_minStationLevelText.text = level == 0 ? "" : level.ToString();
        gui.m_minStationLevelIcon.color = color;
    }

    protected static void HideRequirements(InventoryGui gui)
    {
        for (int i = 0; i < 4; ++i)
        {
            GameObject requirement = gui.m_recipeRequirementList[i];
            InventoryGui.HideRequirement(requirement.transform);
        }
    }

    public static void OnTabCraftPressed(InventoryGui gui)
    {
        ResetAll(gui);
        tabCraftEnabled = false;
        tabUpgradeEnabled = true;
    }

    public static void OnUpgradeTabPressed(InventoryGui gui)
    {
        ResetAll(gui);
        tabUpgradeEnabled = false;
        tabCraftEnabled = true;
    }

    public static void ResetAll(InventoryGui gui)
    {
        for (int i = 0; i < tabs.Count; ++i)
        {
            Tab tab = tabs[i];
            tab.Reset();
        }
        currentTab = null;
        craftButtonLabel.text = Localization.instance.Localize(defaultCraftLabel);
        progressLabel.text = Localization.instance.Localize(defaultProgressLabel);
        gui.m_tabCraft.interactable = tabCraftEnabled;
        gui.m_tabUpgrade.interactable = tabUpgradeEnabled;
        EnableRecipeRequirementList(true);
    }

    public static void ResetCraftingPanel(InventoryGui gui)
    {
        craftButtonLabel.text = Localization.instance.Localize(defaultCraftLabel);
        progressLabel.text = Localization.instance.Localize(defaultProgressLabel);
        gui.m_tabCraft.gameObject.SetActive(true);
        Preview.EnableBlueprintPreview(gui, false);
        SetMinStationLevelIcon(gui, 1, defaultMinStationLevelIconColor, defaultMinStationLevelIcon);
    }

    public static void DisableAll()
    {
        for (int i = 0; i < tabs.Count; ++i)
        {
            Tab tab = tabs[i];
            tab.Disable();
        }
    }
}