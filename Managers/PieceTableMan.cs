using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Workshop;

public static class PieceTableMan
{
    private static readonly Dictionary<string, Piece.PieceCategory> categories;
    private static readonly Dictionary<Piece.PieceCategory, string> customCategories;
    
    static PieceTableMan()
    {
        categories = new Dictionary<string, Piece.PieceCategory>();
        customCategories = new  Dictionary<Piece.PieceCategory, string>();
    }

    public static void Init()
    {
        Harmony harmony = Workshop.instance._harmony;
        harmony.Patch(AccessTools.Method(typeof(Enum), nameof(Enum.GetValues)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(PieceTableMan), nameof(Patch_Enum_GetValues))));
        harmony.Patch(AccessTools.Method(typeof(Enum), nameof(Enum.GetNames)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(PieceTableMan), nameof(Patch_Enum_GetNames))));
        harmony.Patch(AccessTools.Method(typeof(Enum), nameof(Enum.GetName)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PieceTableMan), nameof(Patch_Enum_GetName))));
        harmony.Patch(AccessTools.Method(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PieceTableMan), nameof(Patch_PieceTable_UpdateAvailable))));
    }

    public static int GetMaxCategories() => Enum.GetValues(typeof(Piece.PieceCategory)).Length + customCategories.Count;

    public static void Patch_PieceTable_UpdateAvailable(PieceTable __instance)
    {
        int max = GetMaxCategories();
        __instance.m_availablePieces.Clear();
        for (int i = 0; i < max; ++i)
        {
            __instance.m_availablePieces.Add(new List<Piece>());
        }
    }

    public static Piece.PieceCategory GetCategory(string name)
    {
        if (Enum.TryParse(name, true, out Piece.PieceCategory category)) return category;
        if (categories.TryGetValue(name, out category)) return category;
        
        Dictionary<Piece.PieceCategory, string> map = GetCategories();
        foreach (KeyValuePair<Piece.PieceCategory, string> kvp in map)
        {
            if (kvp.Value == name)
            {
                category = kvp.Key;
                categories[name] = category;
                return category;
            }
        }

        category = (Piece.PieceCategory)(map.Count - 1);
        categories[name] = category;
        customCategories[category] = name;
        return category;
    }

    private static Dictionary<Piece.PieceCategory, string> GetCategories()
    {
        Array values = Enum.GetValues(typeof(Piece.PieceCategory));
        string[] names = Enum.GetNames(typeof(Piece.PieceCategory));
        Dictionary<Piece.PieceCategory, string> map = new();
        for (int i = 0; i < values.Length; ++i)
        {
            map[(Piece.PieceCategory)values.GetValue(i)] = names[i];
        }

        foreach (KeyValuePair<Piece.PieceCategory, string> kvp in customCategories)
        {
            map[kvp.Key] = kvp.Value;
        }
        return map;
    }
    
    private static void Patch_Enum_GetValues(Type enumType, ref Array __result)
    {
        if (enumType != typeof(Piece.PieceCategory)) return;
        if (categories.Count == 0) return;
        Piece.PieceCategory[] f = new Piece.PieceCategory[__result.Length + categories.Count];
        __result.CopyTo(f, 0);
        categories.Values.CopyTo(f, __result.Length);
        __result = f;
    }
    
    private static bool Patch_Enum_GetName(Type enumType, object value, ref string __result)
    {
        if (enumType != typeof(Piece.PieceCategory)) return true;
        if (customCategories.TryGetValue((Piece.PieceCategory)value, out string data))
        {
            __result = data;
            return false;
        }
        return true;
    }

    private static void Patch_Enum_GetNames(Type enumType, ref string[] __result)
    {
        if (enumType != typeof(Piece.PieceCategory)) return;
        if (categories.Count == 0) return;
        __result = __result.AddRangeToArray(categories.Keys.ToArray());
    }
}