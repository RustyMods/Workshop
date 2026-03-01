using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public class GridView
{
    private static GameObject root;
    private static GridLayoutGroup layout;
    private static GameObject element;

    private const float sensitivity = 250f;
    private static readonly Vector2 cellSize = new Vector2(60f, 60f);
    private static readonly Vector2 spacing = new Vector2(5f, 40f);

    public GridView(InventoryGui gui)
    {
        RectTransform recipeDescription = gui.m_recipeDecription.GetComponent<RectTransform>();
        root = new GameObject("Workshop.Blueprint.GridView");
        RectTransform rect = root.AddComponent<RectTransform>();
        rect.SetParent(recipeDescription.parent);
        rect.anchoredPosition = new Vector2(recipeDescription.anchoredPosition.x, -204.84f);
        rect.pivot = recipeDescription.pivot;
        rect.sizeDelta = new Vector2(330f, 398.056f);
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
        Scrollbar scrollbar = scroll.AddComponent<Scrollbar>();
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
        bkg.rectTransform.localPosition = new Vector3(-5.5f, 0f, 0f);
        bkg.sprite = gui.m_recipeListRoot.parent.GetComponent<Image>()?.sprite;
        bkg.color = new Color(0f, 0f, 0f, 0.5f);
        bkg.type = Image.Type.Sliced;
        icons.AddComponent<RectMask2D>();
        ScrollRect scrollRect = icons.AddComponent<ScrollRect>();
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
        layout.padding = new RectOffset(0, 0, 0, 60);
        layout.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter fitter = list.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = listRect;

        element = new GameObject("GridViewElement");
        Image img = element.AddComponent<Image>();
        img.preserveAspect = true;
        img.sprite = gui.m_craftingStationIcon.sprite;
        element.AddComponent<Shadow>();
        GameObject text = new GameObject("text");
        RectTransform textRect = text.AddComponent<RectTransform>();
        textRect.SetParent(img.rectTransform);
        textRect.localPosition = new Vector3(0f, -40f, 0f);
        textRect.sizeDelta = cellSize;
        TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = gui.m_recipeDecription.font;
        tmp.richText = true;
        tmp.text = "test x10";
        tmp.fontSize = 14f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 8f;
        tmp.fontSizeMax = 14f;

        Object.DontDestroyOnLoad(element);

        root.SetActive(false);
    }

    public static void EnableGridView(InventoryGui gui)
    {
        gui.m_recipeName.enabled = false;
        gui.m_recipeDecription.enabled = false;
        gui.m_recipeIcon.enabled = false;
        root.SetActive(true);
    }

    public static void DisableGridView(InventoryGui gui)
    {
        gui.m_recipeName.enabled = true;
        gui.m_recipeDecription.enabled = true;
        gui.m_recipeIcon.enabled = true;
        root.SetActive(false);
        ClearList();
    }

    public static void ClearList()
    {
        for (int i = 0; i < layout.transform.childCount; ++i)
        {
            Transform child = layout.transform.GetChild(i);
            Object.Destroy(child.gameObject);
        }
    }

    public static void UpdateGridView(InventoryGui gui, ConstructionWard ward, List<Piece.Requirement> requirements)
    {
        EnableGridView(gui);
        ClearList();
        for (int i = 0; i < requirements.Count; ++i)
        {
            Piece.Requirement requirement = requirements[i];
            GameObject prefab = Object.Instantiate(element, layout.transform);
            Image img = prefab.GetComponent<Image>();
            TMP_Text txt = prefab.GetComponentInChildren<TMP_Text>();
            int current = ward.m_container.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
            bool hasRequirement = current >= requirement.m_amount;
            img.color = hasRequirement ? Color.white : Color.gray;
            img.sprite = requirement.m_resItem.m_itemData.GetIcon();
            txt.text = string.Format("{0}\n<color={1}>{2}</color>/<color=yellow>{3}</color>",
                Localization.instance.Localize(requirement.m_resItem.m_itemData.m_shared.m_name), 
                hasRequirement ? "orange" : "red",
                requirement.m_amount,
                current);
        }
    }
}