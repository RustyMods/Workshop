using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ServerSync;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Workshop;

public static class Marketplace
{
    public static readonly CustomSyncedValue<string> sync = new(ConfigManager.ConfigSync, "Workshop.Blueprint.SoldBlueprint");
    public static Dictionary<string, int> PurchaseLedger = new();
    public static void OnBlueprintSoldChanged()
    {
        if (string.IsNullOrEmpty(sync.Value))
        {
            RevenueTab.canCollect = true;
            return;
        }

        try
        {
            Dictionary<string, int> purchaseLedger = ConfigManager.Deserialize<Dictionary<string, int>>(sync.Value);
            PurchaseLedger = purchaseLedger;
        }
        catch
        {
            Workshop.LogWarning("Failed to receive server sold ledger");
        }
        finally
        {
            RevenueTab.canCollect = true;
        }
    }

    public static void Init()
    {
        ZRoutedRpc.instance.Register<string>(nameof(RPC_ReceiveBlueprint), RPC_ReceiveBlueprint);
        ZRoutedRpc.instance.Register<string>(nameof(RPC_ReceiveBlueprintPurchaseNotice), RPC_ReceiveBlueprintPurchaseNotice);
        ZRoutedRpc.instance.Register<string, int>(nameof(RPC_SendCollectedNotice), RPC_SendCollectedNotice);
        
        Load();
    }

    public static void Save()
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        string worldName = ZNet.instance.GetWorldName();
        string filename = $"{Workshop.ModName}.{worldName}.Marketplace.dat";
        string filepath = Path.Combine(ConfigManager.ConfigFolderPath, filename);
        string text = ConfigManager.Serialize(PurchaseLedger);
        byte[] compressed = CompressAndEncode(text);
        File.WriteAllBytes(filepath, compressed);
    }

    private static void Load()
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        try
        {
            string worldName = ZNet.instance.GetWorldName();
            string filename = $"{Workshop.ModName}.{worldName}.Marketplace.dat";
            string filepath = Path.Combine(ConfigManager.ConfigFolderPath, filename);
            if (!File.Exists(filepath)) return;
            byte[] data = File.ReadAllBytes(filepath);
            string text = DecompressAndDecode(data);
            PurchaseLedger = ConfigManager.Deserialize<Dictionary<string, int>>(text);
        }
        catch (Exception ex)
        {
            Workshop.LogWarning(ex.Message);
        }
    }
    
    public static void OnLogout() => PurchaseLedger = new Dictionary<string, int>();
    private static byte[] CompressAndEncode(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);

        using var output = new MemoryStream();
        using var gzip = new GZipStream(output, CompressionMode.Compress);
        gzip.Write(data, 0, data.Length);
        gzip.Close();
        return output.ToArray();
    }
    private static string DecompressAndDecode(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    public static void SyncLedger()
    {
        string package = ConfigManager.Serialize(PurchaseLedger);
        sync.Value = package;
        Workshop.LogDebug("Sent updated ledger to peers");
    }

    public static void SendBlueprintToServer(TempBlueprint temp)
    {
        BlueprintTask task = new BlueprintTask
        {
            blueprint = temp.blueprint,
            type = TaskType.NewBlueprint
        };

        if (ZNet.instance.IsServer())
        {
            BlueprintMan.tasks.Enqueue(task);
            BlueprintMan.ProcessTasks();
        }
        else
        {
            temp.blueprint.Write(); // save updated blueprint to local disk
            string text = ConfigManager.Serialize(task);
            ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_ReceiveBlueprint), text);
        }
    }

    public static void RPC_ReceiveBlueprint(long sender, string pkg)
    {
        BlueprintTask task = ConfigManager.Deserialize<BlueprintTask>(pkg);
        BlueprintMan.tasks.Enqueue(task);
        BlueprintMan.ProcessTasks();
    }

    public static void SendPurchaseNotice(Player player, BlueprintRecipe recipe)
    {
        if (ZNet.instance.IsServer())
        {
            RPC_ReceiveBlueprintPurchaseNotice(0L, recipe.name);
        }
        else
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_ReceiveBlueprintPurchaseNotice), recipe.settings.filename);
        }
    }

    public static void RPC_ReceiveBlueprintPurchaseNotice(long sender, string filename)
    {
        if (!BlueprintMan.recipes.ContainsKey(filename))
        {
            Workshop.LogWarning("Received notice of a purchased blueprint, but not recipe not found !");
            return;
        }

        if (PurchaseLedger.ContainsKey(filename))
        {
            ++PurchaseLedger[filename];
        }
        else
        {
            PurchaseLedger[filename] = 1;
        }
        
        SyncLedger();
    }

    public static void SendCollectedNotice(Player player, BlueprintRecipe recipe, int amount)
    {
        if (recipe.m_resources == null || recipe.m_resources.Length == 0) return;
        
        if (ZNet.instance.IsServer())
        {
            RPC_SendCollectedNotice(0L, recipe.settings.filename, amount);
        }
        else
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_SendCollectedNotice), recipe.name, amount);
        }
    }

    public static void RPC_SendCollectedNotice(long sender, string filename, int amount)
    {
        if (!PurchaseLedger.ContainsKey(filename)) return;
        if (PurchaseLedger[filename] > amount)
        {
            PurchaseLedger[filename] -= amount;
        }
        else
        {
            PurchaseLedger.Remove(filename);
        }
        SyncLedger();
    }

    public static bool IsBlueprintPendingCollection(string filename) => PurchaseLedger.ContainsKey(filename);
    public static bool GetRevenue(Player player, out List<KeyValuePair<BlueprintRecipe, int>> revenue)
    {
        revenue = new List<KeyValuePair<BlueprintRecipe, int>>();
        if (player == null) return false;
        foreach (KeyValuePair<string, int> kvp in PurchaseLedger)
        {
            if (BlueprintMan.recipes.TryGetValue(kvp.Key, out BlueprintRecipe recipe) && 
                recipe.settings.Creator.Equals(player.GetPlayerName(), StringComparison.CurrentCultureIgnoreCase))
            {
                if (recipe.m_resources == null || recipe.m_resources.Length == 0) continue;
                revenue.Add(new KeyValuePair<BlueprintRecipe, int>(recipe, kvp.Value));
            }
        }

        return revenue.Count > 0;
    }

    public static void CollectRevenue(Player player, Piece.Requirement[] revenue, int count = 1)
    {
        for (int i = 0; i < revenue.Length; ++i)
        {
            Piece.Requirement requirement = revenue[i];
            requirement.m_amount *= count;
            GiveItem(player, requirement);
        }
    }

    private static void GiveItem(Player player, Piece.Requirement requirement)
    {
        if (requirement.m_amount <= 0) return;
        
        if (requirement.m_amount > requirement.m_resItem.m_itemData.m_shared.m_maxStackSize)
        {
            Piece.Requirement split = new Piece.Requirement
            {
                m_resItem = requirement.m_resItem,
                m_amount = requirement.m_amount - requirement.m_resItem.m_itemData.m_shared.m_maxStackSize,
            };
            GiveItem(player, split);
            requirement.m_amount = requirement.m_resItem.m_itemData.m_shared.m_maxStackSize;
        }
        
        if (player.GetInventory().CanAddItem(requirement.m_resItem.m_itemData))
        {
            player.GetInventory().AddItem(
                requirement.m_resItem.name, 
                requirement.m_amount, 
                requirement.m_resItem.m_itemData.m_quality, 
                requirement.m_resItem.m_itemData.m_variant, 
                0L, 
                "");
        }
        else
        {
            GameObject instance = Object.Instantiate(
                requirement.m_resItem.gameObject, 
                player.transform.position + player.transform.forward + player.transform.up,
                player.transform.rotation);
            ItemDrop item = instance.GetComponent<ItemDrop>();
            item.m_itemData.m_stack = requirement.m_amount;
            Rigidbody rigidbody = instance.GetComponent<Rigidbody>();
            rigidbody.linearVelocity = (player.transform.forward + Vector3.up) *
                                       (item.m_itemData.GetWeight() >= 300.0f ? 0.5f : 5f);
            player.m_dropEffects.Create(player.transform.position, Quaternion.identity);
            player.Message(MessageHud.MessageType.TopLeft, "$msg_dropped " + item.m_itemData.m_shared.m_name, item.m_itemData.m_stack, item.m_itemData.GetIcon());
        }
    }
}