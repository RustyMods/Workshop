using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public abstract class View
{
    private static List<View> _views = new();
    
    protected GameObject _root;
    protected InventoryGui _inventoryGui;
    protected bool _useDescription;
    protected bool _useIcon;
    protected bool _useName;

    protected View(InventoryGui gui)
    {
        _inventoryGui = gui;
        _views.Add(this);
    }
    
    public virtual void Show()
    {
        _inventoryGui.m_recipeDecription.enabled = _useDescription;
        _inventoryGui.m_recipeIcon.enabled = _useIcon;
        _inventoryGui.m_recipeName.enabled = _useName;
        
        _root.SetActive(true);
    }

    public virtual void Hide()
    {
        _root.SetActive(false);
    }

    protected void SetName(string name) => _inventoryGui.m_recipeName.text = Localization.instance.Localize(name);
    protected void SetDescription(string text) => _inventoryGui.m_recipeDecription.text = Localization.instance.Localize(text);
    protected void SetIcon(Sprite icon) => _inventoryGui.m_recipeIcon.sprite = icon;

    public static void HideAll()
    {
        for (int i = 0; i < _views.Count; ++i)
        {
            View view = _views[i];
            view.Hide();
        }
    }
}