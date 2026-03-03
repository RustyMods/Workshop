using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public class PiecesTab : Tab
{
    private ConstructionWard.PieceBlock selectedPiece;

    private bool isLoading;
    private bool isLoadingRequirements;
    private float loadingLength = 1.0f;
    private float loadingTimer;
    private CancellationTokenSource cancelToken;
    
    public PiecesTab(InventoryGui gui, int index = 0) : 
        base(gui, "ConstructionWardRefresh", "$ward_pieces", index)
    {
        SetGamepadHint("$ward_refresh");
        craftLabel = "$ward_build";
        craftTooltip = "$ward_build";
        craftingLabel = "$label_building";
        isWardTab = true;
    }

    public override void OnCraft(InventoryGui gui)
    {
        if (currentWard == null || !Player.m_localPlayer || currentWard.IsBuilding())
        {
            Workshop.LogDebug("Cannot interact, ward is null or building");
            return;
        }
        
        if (currentWard.m_ghostPieces.Count <= 0) return;

        if (selectedPiece != null)
        {
            selectedPiece.OnToggle(gui, currentWard, craftLabel);
        }
        else
        {
            currentWard.Build(Player.m_localPlayer);
            gui.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
        }
    }
    
    protected override void Reset()
    {
        selectedPiece = null;
        base.Reset();
    }

    public override void OnCancel(InventoryGui gui)
    {
        if (cancelToken == null) return;
        
        cancelToken.Cancel();
        cancelToken.Dispose();
        cancelToken = null;
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
        SetRecipeName(ConstructionWard.SHARED_NAME);
        SetDescription(StationTab.WardDescription.tooltip);
        SetRecipeIcon(currentWard.m_piece.m_icon);
        ClearList(gui);
        ResizeList(gui);
        if (!isLoading && !currentWard.IsBuilding())
        {
            cancelToken = new CancellationTokenSource();
            _ = LoadDescription(gui, player, currentWard, cancelToken.Token);
        }
        return true;
    }
    
    public override bool UpdateRecipe(InventoryGui gui, Player player, float dt)
    {
        if (currentWard == null) return false;

        if (currentWard.IsBuilding())
        {
            SetProgressLabel(craftingLabel);
            gui.m_craftProgressPanel.gameObject.SetActive(true);
            gui.m_craftButton.gameObject.SetActive(false);
            gui.m_craftProgressBar.SetMaxValue(currentWard.m_constructionLength);
            gui.m_craftProgressBar.SetValue(currentWard.m_constructionTimer);
            gui.m_craftButton.interactable = false;
        }
        else if (currentWard.isSearching)
        {
            SetProgressLabel("$label_searching");
            gui.m_craftProgressPanel.gameObject.SetActive(true);
            gui.m_craftButton.gameObject.SetActive(false);
            gui.m_craftProgressBar.SetMaxValue(currentWard.ghostsCount);
            gui.m_craftProgressBar.SetValue(currentWard.ghostProcessed);
            gui.m_craftButton.interactable = false;
        }
        else if (isLoading)
        {
            SetProgressLabel(isLoadingRequirements ? "$label_requirements" : "$label_loading");
            gui.m_craftProgressPanel.gameObject.SetActive(true);
            gui.m_craftButton.gameObject.SetActive(false);
            gui.m_craftProgressBar.SetMaxValue(loadingLength);
            gui.m_craftProgressBar.SetValue(loadingTimer);
            gui.m_craftButton.interactable = false;
        }
        else
        {
            gui.m_craftProgressPanel.gameObject.SetActive(false);
            gui.m_craftButton.gameObject.SetActive(true);
            gui.m_craftButton.interactable = !currentWard.IsBuilding();
        }
        
        return true;
    }
    
    private async Task LoadDescription(InventoryGui gui, Player player, ConstructionWard ward, CancellationToken cancellationToken = default)
    {
        try
        {
            isLoading = true;
            await LoadPieces(gui, ward, cancellationToken);
            await LoadRequirements(gui, player, ward, cancellationToken);

        }
        catch (OperationCanceledException)
        {
            Workshop.LogDebug("Load description cancelled");
            GridView.instance.Hide();
        }
        finally
        {
            if (cancelToken != null)
            {
                cancelToken.Dispose();
                cancelToken = null;
            }
            loadingLength = 0;
            loadingTimer = 0.0f;
            isLoading = false;
            isLoadingRequirements = false;
            ResizeList(gui);
            if (gui.m_availableRecipes.Count > 0)
            {
                gui.m_recipeEnsureVisible.CenterOnItem(
                    gui.m_availableRecipes.First().InterfaceElement.transform as RectTransform);
            }
        }
    }

    private async Task LoadPieces(InventoryGui gui, ConstructionWard ward, CancellationToken token = default)
    {
        try
        {
            ward.UpdateGhostPieces();

            if (ward.LoadingGhostTask != null)
            {
                try
                {
                    await ward.LoadingGhostTask;
                }
                catch (OperationCanceledException)
                {
                    Workshop.LogDebug("Loading ghost pieces cancelled");
                    GridView.instance.Hide();
                    isLoading = false;
                    isLoadingRequirements = false;
                    loadingLength = 0f;
                    loadingTimer = 0f;
                    if (cancelToken != null)
                    {
                        cancelToken.Dispose();
                        cancelToken = null;
                    }
                }
            }

            List<ConstructionWard.PieceBlock> pieces = ward.GetPieces();
            loadingLength = pieces.Count;
            loadingTimer = 1f;
            string pieceLocalized = Localization.instance.Localize("$ward_pieces");
            int pieceCount = 0;
            for (int i = 0; i < pieces.Count; ++i)
            {
                ++loadingTimer;
                ConstructionWard.PieceBlock data = pieces[i];
                pieceCount += data.count;
                AddPiece(gui, ward, data, i, token);
                gui.m_recipeName.text = $"{pieceLocalized} ({pieceCount})";
            }
        }
        catch (OperationCanceledException)
        {
            Workshop.LogDebug("Loading pieces cancelled");
            GridView.instance.Hide();
            isLoading = false;
            isLoadingRequirements = false;
            loadingLength = 0f;
            loadingTimer = 0f;
            if (cancelToken != null)
            {
                cancelToken.Dispose();
                cancelToken = null;
            }
        }
    }
    
    private async Task LoadRequirements(InventoryGui gui, Player player, ConstructionWard ward, CancellationToken token = default)
    {
        try
        {
            List<GhostPiece> ghosts = ward.GetGhostPieces();
            if (ghosts.Count > 0)
            {
                List<Piece.Requirement> requirements = ward.GetTotalBuildRequirements();
                GridView.instance.Setup(ward, requirements);
                await Task.Delay(TimeSpan.FromMilliseconds(1), token);
                EnableRecipeRequirementList(true);
                SetupRequirementList(player, ward, ward.GetRequiredCraftingStations().ToArray());
            }
            else
            {
                gui.m_recipeName.text = Localization.instance.Localize(ConstructionWard.SHARED_NAME);
                gui.m_recipeDecription.text = StationTab.WardDescription.tooltip;
            }
        }
        catch (OperationCanceledException)
        {
            Workshop.LogDebug("Loading requirements cancelled");
            GridView.instance.Hide();
            isLoading = false;
            isLoadingRequirements = false;
            loadingLength = 0f;
            loadingTimer = 0f;
            if (cancelToken != null)
            {
                cancelToken.Dispose();
                cancelToken = null;
            }
        }
    }
    
    private void SetupRequirementList(Player player, ConstructionWard ward, params CraftingStation[] stations)
    {
        bool noCost = player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost);
        for (int i = 0; i < 4; ++i)
        {
            Transform elementRoot = _inventoryGui.m_recipeRequirementList[i].transform;
            if (i > stations.Length - 1 || stations[i] == null)
            {
                InventoryGui.HideRequirement(elementRoot);
            }
            else
            {
                CraftingStation station = stations[i];
                Image icon = elementRoot.Find("res_icon").GetComponent<Image>();
                TMP_Text name = elementRoot.Find("res_name").GetComponent<TMP_Text>();
                TMP_Text amount = elementRoot.Find("res_amount").GetComponent<TMP_Text>();
                UITooltip tooltip = elementRoot.GetComponent<UITooltip>();
                bool hasStation = noCost || ward.HasCraftingStation(station);
                icon.gameObject.SetActive(true);
                name.gameObject.SetActive(true);
                amount.gameObject.SetActive(true);
                icon.sprite = station.m_icon;
                icon.color = hasStation ? Color.white : Color.gray;
                string localizedName = Localization.instance.Localize(station.m_name);
                name.text = localizedName;
                tooltip.m_text = localizedName;
                amount.text = "";
            }
        }
    }
    
    private void AddPiece(InventoryGui gui, ConstructionWard ward, ConstructionWard.PieceBlock data, int idx, CancellationToken token = default)
    {
        GameObject element = null;
        try
        {
            element = CreateListElement(gui, idx,
                out Image icon,
                out TMP_Text name,
                out GuiBar durability,
                out TMP_Text quality,
                out Button elementButton);
            
            bool hasRequirements = data.HasRequirements(ward);

            data.icon = icon;
            data.name = name;
            icon.sprite = data.m_sprite;
            name.text = Localization.instance.Localize(data.m_name);
            name.color = hasRequirements ? Color.white : new Color(0.66f, 0.66f, 0.66f, 1f);
            bool disabled = data.IsDisabled(currentWard);
            icon.color = disabled ? Color.black : hasRequirements ? Color.white : new Color(0.66f, 0.66f, 0.66f, 1f);
            name.fontStyle = disabled ? FontStyles.Strikethrough : FontStyles.Normal;
            durability.gameObject.SetActive(false);
            if (data.count > 0)
            {
                quality.text = data.count.ToString();
                quality.gameObject.SetActive(true);
            }
            else
            {
                quality.gameObject.SetActive(false);
            }

            elementButton.onClick.AddListener(() =>
            {
                if (currentWard.IsBuilding()) return;
                selectedPiece = data;
                SetElement(gui, idx, true);
                UpdateSelectedPiece(gui, currentWard, data);
            });

            gui.m_availableRecipes.Add(new InventoryGui.RecipeDataPair
            {
                InterfaceElement = element
            });
        }
        catch (OperationCanceledException)
        {
            if (element != null)
            {
                UnityEngine.Object.Destroy(element);
            }
        }
    }

    private void UpdateSelectedPiece(InventoryGui gui, ConstructionWard ward, ConstructionWard.PieceBlock element)
    {
        GridView.instance.Setup(ward, element.GetRequirements(), true);
        
        SetupRequirementList(Player.m_localPlayer, ward, element.station);

        SetRecipeName(element.m_name + $" x{element.count}");
        
        bool isRemoved = element.IsDisabled(ward);
        SetCraftButton(isRemoved ? "$label_add" : "$label_remove", true);
    }
}
