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
    private ConstructionWard.PieceData selectedPiece;

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
        craftingLabel = "Building";
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
            selectedPiece.OnCraft(gui, currentWard, craftLabel);
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
        
        if (cancelToken == null) return;
        
        cancelToken.Cancel();
        cancelToken.Dispose();
        cancelToken = null;
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
        gui.m_recipeIcon.sprite = currentWard.m_piece.m_icon;
        gui.m_recipeDecription.text = StationTab.WardDescription.tooltip;
        gui.m_recipeName.text = Localization.instance.Localize(ConstructionWard.SHARED_NAME);
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
            player.Message(MessageHud.MessageType.Center, "Loading pieces cancelled");
        }
        finally
        {
            cancelToken.Dispose();
            cancelToken = null;
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
                await ward.LoadingGhostTask;
            }

            List<ConstructionWard.PieceData> pieces = ward.GetPieces();
            TimeSpan delay = TimeSpan.FromMilliseconds(1f);
            loadingLength = pieces.Count;
            loadingTimer = 1f;
            string pieceLocalized = Localization.instance.Localize("$ward_pieces");
            int pieceCount = 0;
            for (int i = 0; i < pieces.Count; ++i)
            {
                ++loadingTimer;
                ConstructionWard.PieceData data = pieces[i];
                pieceCount += data.ghosts.Count;
                AddPiece(gui, ward, data, i);
                gui.m_recipeName.text = $"{pieceLocalized} ({pieceCount})";
                await Task.Delay(delay, token);
            }
        }
        catch (OperationCanceledException)
        {
            isLoading = false;
            isLoadingRequirements = false;
            loadingLength = 0f;
            loadingTimer = 0f;
        }
    }
    
    private async Task LoadRequirements(InventoryGui gui, Player player, ConstructionWard ward, CancellationToken token = default)
    {
        try
        {
            List<GhostPiece> ghosts = ward.GetGhostPieces();
            if (ghosts.Count > 0)
            {
                gui.m_recipeName.text = Localization.instance.Localize($"$ward_pieces ({ghosts.Count})");

                if (player.NoCostCheat())
                {
                    gui.m_recipeDecription.text = "<color=orange>NO COST</color>";
                }
                else
                {
                    int lineCount = 0;
                    StringBuilder sb = new StringBuilder(256);
                    List<CraftingStation> stations = ward.GetMissingCraftingStations();
                    if (stations.Count > 0)
                    {
                        sb.AppendLine("$tooltip_missing_station:");
                        ++lineCount;
                        for (int i = 0; i < stations.Count; ++i)
                        {
                            CraftingStation station = stations[i];
                            sb.AppendLine($"- <color=red>{station.m_name}</color>");
                            ++lineCount;
                        }
                    }

                    List<Piece.Requirement> requirements = ward.GetTotalBuildRequirements();

                    if (requirements.Count > 0)
                    {
                        TimeSpan delay = TimeSpan.FromMilliseconds(1f);
                        loadingTimer = 0f;
                        loadingLength = requirements.Count;
                        isLoadingRequirements = true;
                        sb.AppendLine("\n$blueprint_requirements:");
                        for (int i = 0; i < requirements.Count; ++i)
                        {
                            await Task.Delay(delay, token);

                            if (lineCount > 13)
                            {
                                float remaining = loadingLength - loadingTimer;
                                sb.AppendLine($"... {remaining} more. ");
                                
                                loadingTimer = loadingLength;
                                isLoadingRequirements = false;

                                break;
                            }
                            
                            ++loadingTimer;
                            Piece.Requirement requirement = requirements[i];
                            int currentAmount = ward.m_container.GetInventory()
                                .CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                            bool hasRequirement = currentAmount >= requirement.m_amount;
                            sb.AppendFormat("{0}: <color={1}>{2}</color> / <color=yellow>{3}</color>\n",
                                requirement.m_resItem.m_itemData.m_shared.m_name,
                                hasRequirement ? "orange" : "red",
                                requirement.m_amount,
                                currentAmount
                            );
                            ++lineCount;
                        }
                    }

                    gui.m_recipeDecription.text = Localization.instance.Localize(sb.ToString());
                }
            }
            else
            {
                gui.m_recipeName.text = Localization.instance.Localize(ConstructionWard.SHARED_NAME);
                gui.m_recipeDecription.text = StationTab.WardDescription.tooltip;
            }
        }
        catch (OperationCanceledException)
        {
            isLoading = false;
            isLoadingRequirements = false;
            loadingLength = 0f;
            loadingTimer = 0f;
        }
    }
    
    private void AddPiece(InventoryGui gui, ConstructionWard ward, ConstructionWard.PieceData data, int idx)
    {
        GameObject element = CreateListElement(gui, idx, 
            out Image icon, 
            out TMP_Text name, 
            out GuiBar durability, 
            out TMP_Text quality,
            out Button elementButton);

        bool hasRequirements = data.HasRequirements(ward);
        
        data.icon = icon;
        data.name = name;
        icon.sprite = data.piece.m_icon;
        name.text = Localization.instance.Localize(data.piece.m_name);
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
        InventoryGui.RecipeDataPair pair = new InventoryGui.RecipeDataPair
        {
            InterfaceElement = element
        };
        gui.m_availableRecipes.Add(pair);
    }

    private void UpdateSelectedPiece(InventoryGui gui, ConstructionWard ward, ConstructionWard.PieceData element)
    {
        if (element.piece == null) return;
        gui.m_recipeName.text = Localization.instance.Localize(element.piece.m_name + $" x{element.count}");
        StringBuilder sb = new StringBuilder(256);
        sb.Append($"{element.piece.m_description}\n");

        if (element.piece.m_craftingStation != null && 
            !ward.HasCraftingStation(element.piece.m_craftingStation))
        {
            sb.Append($"$tooltip_missing_station: <color=red>{element.piece.m_craftingStation.m_name}</color>\n");
        }
        
        List<Piece.Requirement> requirements = element.GetRequirements();

        for (int i = 0; i < requirements.Count; ++i)
        {
            Piece.Requirement requirement = requirements[i];
            int count = currentWard.m_container.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
            sb.AppendFormat("{0}: <color={1}>{2}</color> / <color=yellow>{3}</color>\n",
                requirement.m_resItem.m_itemData.m_shared.m_name,
                count >= requirement.m_amount ? "orange" : "red",
                requirement.m_amount,
                count);
        }
        
        gui.m_recipeDecription.text = Localization.instance.Localize(sb.ToString());
        bool isRemoved = element.IsDisabled(ward);
        craftButtonLabel.text = Localization.instance.Localize(isRemoved ? "$label_add" : "$label_remove");
        gui.m_craftButton.interactable = true;
    }
    
    [Obsolete]
    private void SetupDescription(InventoryGui gui, ConstructionWard ward)
    {
        List<GhostPiece> ghosts = ward.GetGhostPieces();
        if (ghosts.Count > 0)
        {
            StringBuilder sb = new StringBuilder(256);
            AppendStations(sb, ward);
            AppendCost(sb, ward);
            gui.m_recipeName.text = Localization.instance.Localize($"$ward_pieces ({ghosts.Count})");
            gui.m_recipeDecription.text = Localization.instance.Localize(sb.ToString());
            gui.m_craftButton.interactable = true;
        }
        else
        {
            gui.m_recipeName.text = Localization.instance.Localize(ConstructionWard.SHARED_NAME);
            gui.m_recipeDecription.text = StationTab.WardDescription.tooltip;
            gui.m_craftButton.interactable = false;
        }
    }
    
    [Obsolete]
    private void AppendCost(StringBuilder sb, ConstructionWard ward)
    {
        List<Piece.Requirement> requirements = ward.GetTotalBuildRequirements();
        if (requirements.Count <= 0) return;
        sb.AppendLine("\n$blueprint_requirements:");
        for (int i = 0; i < requirements.Count; ++i)
        {
            Piece.Requirement requirement = requirements[i];
            int currentAmount = ward.m_container.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
            bool hasRequirement = currentAmount >= requirement.m_amount;
            sb.AppendFormat("{0}: <color={1}>{2}</color> / <color=yellow>{3}</color>\n", 
                requirement.m_resItem.m_itemData.m_shared.m_name, 
                hasRequirement ? "orange" : "red",
                requirement.m_amount,
                currentAmount
            );
        }
    }
    
    [Obsolete]
    private void AppendStations(StringBuilder sb, ConstructionWard ward)
    {
        List<CraftingStation> stations = ward.GetMissingCraftingStations();
        if (stations.Count <= 0) return;
        sb.AppendLine("$tooltip_missing_station:");
        for (int i = 0; i < stations.Count; ++i)
        {
            CraftingStation station = stations[i];
            sb.AppendLine($"- <color=red>{station.m_name}</color>");
        }
    }

    [Obsolete]
    private void SetupPieceList(InventoryGui gui, ConstructionWard ward)
    {
        ClearList(gui);
        List<ConstructionWard.PieceData> pieces = currentWard.GetPieces();
        for (int i = 0; i < pieces.Count; ++i)
        {
            ConstructionWard.PieceData data = pieces[i];
            AddPiece(gui, ward, data, i);
        }
        ResizeList(gui);
    }
}
