using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workshop;

public static class Commands
{
    public static void Init()
    {
        _ = new Command("local", "prints list of local blueprint names", args =>
        {
            foreach (KeyValuePair<string, Blueprint> kvp in BlueprintMan.localBlueprints)
            {
                args.Context.AddString($"- <color=orange>{kvp.Key}</color>");
            }
        });

        _ = new Command("recipes", "prints list of loaded recipes", args =>
        {
            foreach (KeyValuePair<string, BlueprintRecipe> kvp in BlueprintMan.recipes)
            {
                if (kvp.Value.Loaded)
                {
                    args.Context.AddString($"- KEY: <color=orange>{kvp.Key}</color>");
                    args.Context.AddString($"Recipe Name: <color=orange>{kvp.Value.name}</color>");
                    args.Context.AddString($"Display Name: <color=orange>{kvp.Value.settings.Name}</color>");
                }
            }
        });

        _ = new Command("blueprints", "prints list of blueprints", args =>
        {
            
        });

        _ = new Command("plans", "prints list of loaded prefab plans", args =>
        {
            int invalid = 0;
            for (var i = 0; i < MockManager.temp.Count; ++i)
            {
                GameObject temp = MockManager.temp[i];
                if (temp == null)
                {
                    ++invalid;
                }
                else
                {
                    args.Context.AddString($"- <color=orange>{temp.name}</color>");
                }
            }

            if (invalid > 0)
            {
                args.Context.AddString($"Null objects: <color=orange>{invalid}</color>");
            }
        });

        _ = new Command("clean", "clears invalid plans from memory", args =>
        {
            List<GameObject> prefabsToRemove = MockManager.temp.FindAll(p => p == null);
            for (int i = 0; i < prefabsToRemove.Count; ++i)
            {
                GameObject temp = prefabsToRemove[i];
                MockManager.temp.Remove(temp);
            }
            args.Context.AddString($"-  <color=orange>removed {prefabsToRemove.Count}</color>");
        });

        _ = new Command("ledger", "prints current purchase ledger", args =>
        {
            args.Context.AddString("> List of recipe names and the number of times purchased: ");
            foreach (KeyValuePair<string, int> ledger in Marketplace.PurchaseLedger)
            {
                args.Context.AddString($"- <color=orange>{ledger.Key}</color> ( {ledger.Value} )");
            }
        });
    }
    
    
}