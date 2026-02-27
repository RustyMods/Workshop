using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace Workshop;

[Serializable]
public struct Requirement
{
    public string itemName;
    public int amount;
    public bool recover;

    public Requirement()
    {
        itemName = "";
        amount = 1;
        recover = false;
    }

    public Requirement(string itemName, int amount = 1, bool recover = true)
    {
        this.itemName = itemName;
        this.amount = amount;
        this.recover = recover;
    }
}

public class PieceRequirements
{
    public readonly List<Requirement> Requirements;

    public PieceRequirements(List<Requirement> reqs) => Requirements = reqs;

    public PieceRequirements(params Requirement[] reqs)
    {
        Requirements = reqs.ToList();
    }

    public PieceRequirements(Piece.Requirement[] reqs)
    {
        Requirements = new List<Requirement>();
        for (int i = 0; i < reqs.Length; ++i)
        {
            var req = reqs[i];
            Requirements.Add(new Requirement
            {
                itemName = req.m_resItem.name,
                amount = req.m_amount,
                recover = req.m_recover,
            });
        }
    }

    public PieceRequirements(string reqs)
    {
        Requirements = reqs.Split(';').Select(r =>
        {
            string[] parts = r.Split(',');
            return new Requirement
            {
                itemName = parts.GetString(0),
                amount = parts.GetInt(1, 1),
                recover = parts.GetBool(2, true),
            };
        }).ToList();
    }
    
    public Piece.Requirement[] ToPieceRequirement(string requester = "")
    {
        Dictionary<string, Piece.Requirement> resources = new Dictionary<string, Piece.Requirement>();
        for (int i = 0; i < Requirements.Count; ++i)
        {
            Requirement req = Requirements[i];
            if (string.IsNullOrEmpty(req.itemName)) continue;
            ItemDrop item = req.itemName.GetItemDrop(requester);
            if (item == null) continue;
            resources[req.itemName] = new Piece.Requirement
            { 
                m_amount = req.amount, 
                m_recover = req.recover, 
                m_resItem = item 
            };
        }

        return resources.Values.ToArray();
    }

    public override string ToString()
    {
        return string.Join(";", Requirements.Select(r => $"{r.itemName},{r.amount},{r.recover}"));
    }
    
    public string ToCustomString() => string.Join(";", Requirements.Select(r => $"{r.itemName},{r.amount}"));

    private static void DrawConfigTable(ConfigEntryBase cfg)
    {
        bool locked = cfg.Description.Tags
            .Select(a =>
                a.GetType().Name == "ConfigurationManagerAttributes"
                    ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                    : null).FirstOrDefault(v => v != null) ?? false;

        List<Requirement> currentReqs = new PieceRequirements((string)cfg.BoxedValue).Requirements;
        List<Requirement> newReqs = new();
        bool wasUpdated = false;

        GUILayout.BeginVertical();
        foreach (Requirement req in currentReqs)
        {
            GUILayout.BeginHorizontal();

            int amount = req.amount;
            if (int.TryParse(
                    GUILayout.TextField(amount.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 40 }),
                    out int newAmount) && newAmount != amount && !locked)
            {
                amount = newAmount;
                wasUpdated = true;
            }

            string newItemName = GUILayout.TextField(req.itemName, new GUIStyle(GUI.skin.textField) );
            string itemName = locked ? req.itemName : newItemName;
            wasUpdated = wasUpdated || itemName != req.itemName;

            bool recover = req.recover;
            if (GUILayout.Toggle(req.recover, "Recover", new GUIStyle(GUI.skin.toggle) { fixedWidth = 67 }) !=
                req.recover)
            {
                recover = !recover;
                wasUpdated = true;
            }

            if (GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked)
            {
                wasUpdated = true;
            }
            else
            {
                newReqs.Add(new Requirement { amount = amount, itemName = itemName, recover = recover });
            }

            if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked)
            {
                wasUpdated = true;
                newReqs.Add(new Requirement { amount = 1, itemName = "", recover = false });
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        if (wasUpdated)
        {
            cfg.BoxedValue = new PieceRequirements(newReqs).ToString();
        }
    }

    public static readonly ConfigurationManagerAttributes attributes = new ConfigurationManagerAttributes()
        { CustomDrawer = DrawConfigTable };
}

public static partial class Extensions
{
    public static ItemDrop GetItemDrop(this string name, string requester = "")
    {
        ItemDrop item = PrefabManager.GetPrefab(name)?.GetComponent<ItemDrop>();
        if (item == null)
        {
            if (!string.IsNullOrEmpty(requester)) Workshop.LogWarning($"[ {requester} ] The requirement item '{name}' does not exist.");
            else Workshop.LogWarning($"The requirement item '{name}' does not exist.");
        }
        return item;
    }
}