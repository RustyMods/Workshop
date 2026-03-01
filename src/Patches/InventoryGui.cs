using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    private static class InventoryGui_Awake_Patch
    {
        private static void Postfix(InventoryGui __instance)
        {
            Tab.listRoot = __instance.m_recipeRequirementList[0].transform.parent;
            Tab.craftButtonLabel = __instance.m_craftButton.GetComponentInChildren<TMP_Text>();
            Tab.craftButtonTooltip = __instance.m_craftButton.GetComponent<UITooltip>();
            Tab.progressLabel = __instance.m_craftProgressPanel.GetComponentInChildren<TMP_Text>();
            Tab.defaultCraftLabel = Tab.craftButtonLabel.text;
            Tab.defaultProgressLabel = Tab.progressLabel.text;
            Tab.defaultMinStationLevelIcon = __instance.m_minStationLevelIcon.sprite;
            Tab.defaultMinStationLevelIconColor = __instance.m_minStationLevelIcon.color;
            
            Tab.tabs.Clear();
            
            _ = new Preview(__instance);
            _ = new GridView(__instance);
            
            _ = new PiecesTab(__instance, 2);
            _ = new StationTab(__instance, 3);
            
            _ = new RevenueTab(__instance, 2);
            _ = new PublishTab(__instance, 3);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
    private static class Player_GetAvailableRecipes_Patch
    {
        private static void Postfix(Player __instance, ref List<Recipe> available)
        {
            CraftingStation currentStation = __instance.GetCurrentCraftingStation();
            if (currentStation == null) return;
            if (currentStation.m_name != BlueprintTable.SHARED_NAME) return;
            available.RemoveAll(r => r.m_craftingStation != BlueprintTable.CRAFTING_STATION);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static class InventoryGui_Show_Patch
    {
        private static void Postfix(InventoryGui __instance, Container container)
        {
            Tab.currentWard = null;
            Tab.currentStation = null;
            
            if (container != null && 
                container.TryGetComponent(out ConstructionWard ward))
            {
                Tab.currentWard = ward;
            }
            
            if (Tab.currentWard != null)
            {
                for (int i = 0; i < Tab.tabs.Count; ++i)
                {
                    Tab tab = Tab.tabs[i];
                    if (tab.tabPrefab == null) continue;
                    tab.tabPrefab.SetActive(tab.isWardTab);
                }
            }
            else if (Player.m_localPlayer &&
                     Player.m_localPlayer.GetCurrentCraftingStation() is {} station &&
                     station.m_name.Equals(BlueprintTable.CRAFTING_STATION.m_name))
            {
                for (int i = 0; i < Tab.tabs.Count; ++i)
                {
                    Tab tab = Tab.tabs[i];
                    if (tab.tabPrefab == null) continue;
                    tab.tabPrefab.SetActive(tab.isTableTab);
                }

                Tab.currentStation = station;
            }
            else
            {
                Tab.DisableAll();
                Tab.ResetAll(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabCraftPressed))]
    private static class InventoryGui_OnTabCraftPressed_Patch
    {
        private static void Prefix(InventoryGui __instance)
        {
            Tab.OnTabCraftPressed(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabUpgradePressed))]
    private static class InventoryGui_OnTabUpgradePressed_Patch
    {
        private static void Prefix(InventoryGui __instance)
        {
            Tab.OnUpgradeTabPressed(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class InventoryGui_Update_Patch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            return Tab.currentTab != PublishTab.instance || 
                   !PublishTab.instance.isTyping;
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    private static class InventoryGui_Hide_Patch
    {
        private static void Postfix(InventoryGui __instance)
        {
            Workshop.instance.Invoke(nameof(Workshop.DelayedTabsHide), 0.2f);
            Tab.ResetAll(__instance);
            Tab.currentWard = null;
            Tab.currentStation = null;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
    private static class InventoryGui_UpdateCraftingPanel_Patch
    {
        private static bool Prefix(InventoryGui __instance, bool focusView)
        {
            Tab.ResetCraftingPanel(__instance);

            return !Player.m_localPlayer ||
                   !Tab.currentTab ||
                   !Tab.currentTab.InTab() ||
                   !Tab.currentTab.SetupCraftingPanel(__instance, Player.m_localPlayer, focusView);
        }

        private static void Finalizer(InventoryGui __instance)
        {
            Tab.UpdateTabPlacement(
                __instance.m_tabCraft.gameObject.activeSelf,
                __instance.m_tabUpgrade.gameObject.activeSelf);
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    private static class InventoryGui_UpdateRecipe_Patch
    {
        private static bool Prefix(InventoryGui __instance, Player player, float dt)
        {
            return !Tab.currentTab || !Tab.currentTab.UpdateRecipe(__instance, player, dt);
        }
        
        private static void Postfix(InventoryGui __instance, Player player, float dt)
        {
            if (__instance.m_selectedRecipe.Recipe is BlueprintRecipe)
            {
                __instance.m_recipeName.enabled = false;
                __instance.m_recipeDecription.enabled = false;
                __instance.m_recipeIcon.enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetRecipe))]
    private static class InventoryGui_OnSelectedRecipe_Patch
    {
        private static void Postfix(InventoryGui __instance, int index, bool center)
        {
            if (__instance.m_selectedRecipe.Recipe is BlueprintRecipe blueprint)
            {
                Preview.EnableBlueprintPreview(__instance, true);
                Preview.UpdateBlueprintPreview(blueprint);
            }
            else
            {
                Preview.EnableBlueprintPreview(__instance, false);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
    private static class InventoryGui_OnCraftPressed_Patch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (Tab.currentTab == null) return true;
            Tab.currentTab.OnCraft(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftCancelPressed))]
    private static class InventoryGui_OnCraftCancelPressed_Patch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (Tab.currentTab == null) return true;
            Tab.currentTab.OnCancel(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    private static class InventoryGui_DoCrafting_Patch
    {
        private static void Postfix(InventoryGui __instance, Player player)
        {
            if (__instance.m_craftRecipe is BlueprintRecipe blueprint && 
                player.GetInventory().ContainsItemByName(blueprint.m_item.m_itemData.m_shared.m_name))
            {
                Marketplace.SendPurchaseNotice(player, blueprint);
            }
        }
    }
}