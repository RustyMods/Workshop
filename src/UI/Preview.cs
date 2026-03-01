using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public class Preview : View
{
    public static Preview instance;

    private readonly TMPro.TMP_Text description;
    private readonly Image preview;
    public Preview(InventoryGui gui) : base(gui)
    {
        instance = this;
        
        RectTransform recipeDescription = gui.m_recipeDecription.GetComponent<RectTransform>();
        _root = new GameObject("Workshop.Blueprint.Preview");
        RectTransform rect = _root.AddComponent<RectTransform>();
        rect.SetParent(recipeDescription.parent);
        rect.anchoredPosition = new Vector2(recipeDescription.anchoredPosition.x, -204.84f);
        rect.pivot = recipeDescription.pivot;
        rect.sizeDelta = new Vector2(-15f, 398.056f);
        rect.anchorMax = recipeDescription.anchorMax;
        rect.anchorMin = recipeDescription.anchorMin;

        VerticalLayoutGroup layout = _root.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 0f;
        
        GameObject background = new GameObject("background");
        Image bkg = background.AddComponent<Image>();
        bkg.sprite = recipeDescription.parent.GetComponent<Image>().sprite;
        bkg.type = Image.Type.Sliced;
        bkg.color = new Color(0f, 0f, 0f, 0.5f);
        bkg.rectTransform.SetParent(rect);
        LayoutElement bkgElement = background.AddComponent<LayoutElement>();
        bkgElement.minWidth = recipeDescription.rect.width;
        bkgElement.minHeight = 256;
        bkgElement.preferredWidth = recipeDescription.rect.width;
        bkgElement.preferredHeight = 256;
        background.AddComponent<VerticalLayoutGroup>();
        
        GameObject image = new GameObject("preview");
        preview = image.AddComponent<Image>();
        preview.rectTransform.SetParent(bkg.rectTransform);
        preview.preserveAspect = true;

        description = Object.Instantiate(gui.m_recipeDecription.gameObject, _root.transform).GetComponent<TMPro.TMP_Text>();
        description.enabled = true;
        _root.SetActive(false);
    }

    public void Setup(BlueprintSettings settings, Sprite icon)
    {
        StringBuilder sb = new(256);
        sb.AppendLine($"{(string.IsNullOrEmpty(settings.Description) ? settings.Name : settings.Description)}\n");
        sb.AppendLine($"$label_objects: <color=orange>{settings.Pieces.Count}</color>");
        if (settings.Terrains.Count > 0) sb.AppendLine($"$label_terrains: <color=orange>{settings.Terrains.Count}</color>");
        sb.AppendLine($"$label_creator: <color=orange>{settings.Creator}</color>");
        description.text = Localization.instance.Localize(sb.ToString());
        preview.sprite = icon;
        
        Show();
    }
    
}