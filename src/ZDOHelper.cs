using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Workshop;

public static class ZDOHelper
{
    public static void TryDeserialize(ZDO ZDO, string zdo, Vector3 pos, Quaternion rot)
    {
        if (string.IsNullOrEmpty(zdo)) return;
        int hash = ZDO.m_prefab;
        bool persistent = ZDO.Persistent;
        try
        {
            ZPackage pkg = new ZPackage(zdo);
            ZDO.Deserialize(pkg);
        }
        catch (Exception e)
        {
            Workshop.LogDebug("Failed to read ZDO package: " + zdo);
            Workshop.LogDebug(e.Message);
        }
        finally
        {
            ZDO.SetPrefab(hash);
            ZDO.Persistent = persistent;
            ZDO.SetPosition(pos);
            ZDO.SetRotation(rot);
        }
    }

    public static void LoadItemStand(Player player, ZDO ZDO, ConstructionWard ward, GameObject instance, ItemStandItemData attach)
    {
        if (attach == null || !attach.isValid) return;
        
        GameObject itemPrefab = PrefabManager.GetPrefab(attach.prefab);
        if (itemPrefab == null) return;
        ItemDrop prefabItem = itemPrefab.GetComponent<ItemDrop>();
        if (prefabItem == null) return;
        
        if (!player.NoCostCheat() && 
            !ward.m_container.GetInventory().ContainsItemByName(prefabItem.m_itemData.m_shared.m_name))
        {
            return;
        }

        if (!player.NoCostCheat())
        {
            ward.m_container.GetInventory().RemoveItem(prefabItem.m_itemData.m_shared.m_name, 1);
        }
        
        ZNetView.m_forceDisableInit = true;
        GameObject item = Object.Instantiate(itemPrefab);
        ZNetView.m_forceDisableInit = false;
        
        if (item.TryGetComponent(out ItemDrop itemDrop))
        {
            itemDrop.m_itemData.m_variant = attach.variant;
            itemDrop.m_itemData.m_quality = attach.quality;
            ZDO.Set(ZDOVars.s_item, itemDrop.m_itemData.m_dropPrefab.name);
            ItemDrop.SaveToZDO(itemDrop.m_itemData, ZDO);
            if (instance.TryGetComponent(out ItemStand itemStand))
            {
                itemStand.SetVisualItem(
                    itemDrop.m_itemData.m_dropPrefab.name, 
                    itemDrop.m_itemData.m_variant, 
                    itemDrop.m_itemData.m_quality, 
                    itemStand.GetOrientation());
            }
        }
        Object.Destroy(item);
    }

    public static void LoadDoorState(ZDO ZDO, GameObject instance, int state)
    {
        ZDO.Set(ZDOVars.s_state, state);
        if (instance.TryGetComponent(out Door door))
        {
            door.SetState(state);
        }
    }
}