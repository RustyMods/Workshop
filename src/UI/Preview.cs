using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Workshop;

public class Preview
{
    private static GameObject root;
    private static TMPro.TMP_Text description;
    private static Image preview;

    public Preview(InventoryGui gui)
    {
        RectTransform recipeDescription = gui.m_recipeDecription.GetComponent<RectTransform>();
        root = new GameObject("Workshop.Blueprint.Preview");
        RectTransform rect = root.AddComponent<RectTransform>();
        rect.SetParent(recipeDescription.parent);
        rect.anchoredPosition = new Vector2(recipeDescription.anchoredPosition.x, -204.84f);
        rect.pivot = recipeDescription.pivot;
        rect.sizeDelta = new Vector2(-15f, 398.056f);
        rect.anchorMax = recipeDescription.anchorMax;
        rect.anchorMin = recipeDescription.anchorMin;

        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
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

        description = Object.Instantiate(gui.m_recipeDecription.gameObject, root.transform).GetComponent<TMPro.TMP_Text>();
        description.enabled = true;
        root.SetActive(false);
    }

    public static void EnableBlueprintPreview(InventoryGui gui, bool enable)
    {
        gui.m_recipeDecription.enabled = !enable;
        gui.m_recipeIcon.enabled = !enable;
        gui.m_recipeName.enabled = !enable;
        root.SetActive(enable);
    }
    
    public static void UpdateBlueprintPreview(BlueprintRecipe recipe)
    {
        UpdatePreview(
            recipe.settings.Name, 
            recipe.settings.Description, 
            recipe.settings.Creator, 
            recipe.icon, 
            recipe.settings.Pieces, 
            recipe.settings.Terrains);
    }

    public static void UpdateBlueprintPreview(TempBlueprint temp)
    {        
        UpdatePreview(
            temp.settings.Name, 
            temp.settings.Description, 
            temp.settings.Creator, 
            temp.icon, 
            temp.settings.Pieces, 
            temp.settings.Terrains);
    }

    private static void UpdatePreview(
        string name, 
        string desc,
        string creator, 
        Sprite icon,
        List<PlanPiece> pieces, 
        List<PlanTerrain> terrain)
    {
        StringBuilder sb = new(256);
        sb.AppendLine($"{(string.IsNullOrEmpty(desc) ? name : desc)}\n");
        sb.AppendLine($"$label_objects: <color=orange>{pieces.Count}</color>");
        if (terrain.Count > 0) sb.AppendLine($"$label_terrains: <color=orange>{terrain.Count}</color>");
        sb.AppendLine($"$label_creator: <color=orange>{creator}</color>");
        description.text = Localization.instance.Localize(sb.ToString());
        preview.sprite = icon;
    }
}