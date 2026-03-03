using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Workshop;

public class GridView : View
{
    public static GridView instance;
    
    private readonly GameObject element;

    private readonly GridLayoutGroup layout;
    private Scrollbar scrollbar;
    private ScrollRect scrollRect;

    private const float sensitivity = 250f;
    private static readonly Vector2 cellSize = new (64f, 64f);
    private static readonly Vector2 spacing = new (2f, 2f);
    private static readonly Vector2 size = new(335f, 320f);
    private const int minElements = 25;

    public GridView(InventoryGui gui) : base(gui)
    {
        instance = this;

        _useName = true;
        
        RectTransform recipeDescription = gui.m_recipeDecription.GetComponent<RectTransform>();
        _root = new GameObject("Workshop.Blueprint.GridView");
        RectTransform rect = _root.AddComponent<RectTransform>();
        rect.SetParent(recipeDescription.parent);
        rect.anchoredPosition = new Vector2(recipeDescription.anchoredPosition.x, -225.84f);

        rect.pivot = recipeDescription.pivot;
        rect.sizeDelta = size;
        rect.anchorMax = recipeDescription.anchorMax;
        rect.anchorMin = recipeDescription.anchorMin;

        GameObject scroll = new GameObject("scrollbar");
        RectTransform scrollbarRect = scroll.AddComponent<RectTransform>();
        scrollbarRect.SetParent(rect);
        scrollbarRect.localPosition = new Vector3(170f, 0f, 0f);
        scrollbarRect.sizeDelta = rect.sizeDelta with { x = 10f };
        Image scrollImg = scroll.AddComponent<Image>();
        scrollImg.sprite = gui.m_recipeListScroll.GetComponent<Image>()?.sprite;
        scrollImg.type = Image.Type.Sliced;
        scrollImg.color = new Color(0.2f, 0.1f, 0.1f, 1f);
        scrollImg.enabled = false;
        scrollbar = scroll.AddComponent<Scrollbar>();
        scrollbar.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        scrollbar.colors = gui.m_recipeListScroll.colors;
        
        GameObject slidingArea = new GameObject("slidingArea");
        RectTransform slidingAreaRect = slidingArea.AddComponent<RectTransform>();
        slidingAreaRect.SetParent(scrollbarRect);
        slidingAreaRect.sizeDelta = rect.sizeDelta with {x = 10f};
        GameObject handle = new GameObject("handle");
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.SetParent(slidingAreaRect);
        handleRect.sizeDelta = rect.sizeDelta with {x = 10f};
        Image handleImg = handle.AddComponent<Image>();
        handleImg.sprite = (gui.m_recipeListScroll.targetGraphic as Image)?.sprite;
        handleImg.type = Image.Type.Sliced;
        handleImg.color = (gui.m_recipeListScroll.targetGraphic as Image)?.color ?? new Color(1f, 0.5f, 0f, 1f);
        scrollbar.targetGraphic = handleImg;
        
        GameObject icons = new GameObject("icons");
        Image bkg = icons.AddComponent<Image>();
        bkg.rectTransform.SetParent(rect);
        bkg.rectTransform.sizeDelta = rect.sizeDelta;
        bkg.rectTransform.localPosition = new Vector3(-2f, 0f, 0f);
        bkg.sprite = gui.m_recipeListRoot.parent.GetComponent<Image>()?.sprite;
        bkg.color = new Color(0f, 0f, 0f, 0.0f);
        bkg.type = Image.Type.Sliced;
        icons.AddComponent<RectMask2D>();
        scrollRect = icons.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.scrollSensitivity = sensitivity;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        
        GameObject list = new GameObject("grid");
        RectTransform listRect = list.AddComponent<RectTransform>();
        listRect.SetParent(bkg.rectTransform);
        listRect.sizeDelta = rect.sizeDelta;
        listRect.localPosition = Vector3.zero;
        layout = list.AddComponent<GridLayoutGroup>();
        layout.cellSize = cellSize;
        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 0, (int)cellSize.y);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        LayoutElement layoutElement = list.AddComponent<LayoutElement>();
        layoutElement.minHeight = listRect.sizeDelta.y;
        ContentSizeFitter fitter = list.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = listRect;

        GameObject source = _inventoryGui.m_recipeRequirementList[0].gameObject;
        element = Object.Instantiate(source, _root.transform);
        element.name = "b_res_element";
        element.SetActive(false);

        _root.SetActive(false);
    }

    public override void Hide()
    {
        _root.SetActive(false);
        ClearList();
    }

    private void ClearList()
    {
        for (int i = 0; i < layout.transform.childCount; ++i)
        {
            Transform child = layout.transform.GetChild(i);
            Object.Destroy(child.gameObject);
        }
    }

    public void Setup(ConstructionWard ward, List<Piece.Requirement> requirements, bool breakStacks = false)
    {
        ClearList();
        for (int i = 0; i < requirements.Count; ++i)
        {
            Piece.Requirement requirement = requirements[i];
            Sprite icon = requirement.m_resItem.m_itemData.GetIcon();
            string name = requirement.m_resItem.m_itemData.m_shared.m_name;
            int inventoryCount = ward.m_container.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
            int total = requirement.m_amount;

            if (breakStacks && requirement.m_resItem.m_itemData.m_shared.m_maxStackSize < requirement.m_amount)
            {
                int maxStack = requirement.m_resItem.m_itemData.m_shared.m_maxStackSize;
                int remaining = inventoryCount;
                
                while (total > maxStack)
                {
                    AddElement(icon, name, Math.Min(remaining, maxStack), maxStack);
                    remaining = Math.Max(0, remaining - maxStack);
                    total -= maxStack;
                }
                AddElement(icon, name, Math.Min(remaining, total), total);
                
            }
            else
            {
                AddElement(icon, name, inventoryCount, total);
            }
        }
        
        Show();
    }

    private static void AddElement(Sprite sprite, string itemName, int count, int total)
    {
        GameObject prefab = Object.Instantiate(instance.element, instance.layout.transform);
        Image icon = prefab.transform.Find("res_icon").GetComponent<Image>();
        TMP_Text name = prefab.transform.Find("res_name").GetComponent<TMP_Text>();
        TMP_Text amount = prefab.transform.Find("res_amount").GetComponent<TMP_Text>();
        UITooltip tooltip = prefab.GetComponent<UITooltip>();
        bool hasRequirement = total < count;
        icon.color = hasRequirement ? Color.white : Color.gray;
        icon.sprite = sprite;
        name.text = Localization.instance.Localize(itemName);
        tooltip.m_text = name.text;
        amount.text = string.Format("<color={0}>{1}</color> / <color=yellow>{2}</color>",
            hasRequirement ? "yellow" : "red",
            count,
            total);
        prefab.SetActive(true);
    }
}