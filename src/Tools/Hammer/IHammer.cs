using System;
using UnityEngine;

namespace Workshop;

public abstract class IHammer
{
    public string sharedName;
    public ItemDrop item;
    public Recipe recipe;
    public CraftingStation station;
    public PieceTable table;
    
    public IHammer(string sharedName, CraftingStation station)
    {
        this.sharedName = sharedName;
        this.station = station;
    }

    public void InsertPiece(GameObject prefab, int index = 0)
    {
        if (table.m_pieces.Contains(prefab)) return;
        
        if (table.m_pieces.Count <= index) AddPiece(prefab);
        else table.m_pieces.Insert(index, prefab);
    }

    public void AddPiece(GameObject prefab)
    {
        if (table.m_pieces.Contains(prefab)) return;
        table.m_pieces.Add(prefab);
    }
    public void RemovePiece(GameObject prefab) => table.m_pieces.Remove(prefab);

    public void RemoveAll<T>() where T : MonoBehaviour
    {
        table.m_pieces.RemoveAll(p => p.GetComponent<T>());
    }

    public Sprite GetIcon() => item.m_itemData.GetIcon();

    public abstract void OnRecipeChange(object sender, EventArgs e);
}