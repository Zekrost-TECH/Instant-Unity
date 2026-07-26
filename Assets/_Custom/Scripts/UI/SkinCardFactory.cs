using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Genera una tarjeta de tienda básica por código. Es el plan B para poder probar la
/// tienda sin haber autorado el prefab: en cuanto asignes `cardPrefab` en el
/// SkinShopUI, esta clase deja de usarse.
/// </summary>
public static class SkinCardFactory
{
    private const float CardWidth = 260f;
    private const float CardHeight = 320f;

    public static SkinCardUI Create(Transform parent, Sprite previewSprite)
    {
        GameObject root = NewUIObject("SkinCard", parent, CardWidth, CardHeight);

        Image background = root.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.15f);

        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = CardWidth;
        layout.preferredHeight = CardHeight;

        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;

        // Preview del personaje
        GameObject previewObj = NewUIObject("Preview", root.transform, 150f, 150f);
        RectTransform previewRect = (RectTransform)previewObj.transform;
        previewRect.anchorMin = new Vector2(0.5f, 1f);
        previewRect.anchorMax = new Vector2(0.5f, 1f);
        previewRect.pivot = new Vector2(0.5f, 1f);
        previewRect.anchoredPosition = new Vector2(0f, -18f);

        Image preview = previewObj.AddComponent<Image>();
        preview.sprite = previewSprite;
        preview.preserveAspect = true;
        preview.raycastTarget = false;

        // Nombre y precio anclados abajo, apilados: anclar el nombre al centro lo
        // metía justo encima del preview.
        TextMeshProUGUI nameText = NewText("Name", root.transform, 40f, 0f, 56f);
        nameText.fontSize = 30f;
        nameText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI statusText = NewText("Status", root.transform, 44f, 0f, 14f);
        statusText.fontSize = 26f;

        SkinCardUI card = root.AddComponent<SkinCardUI>();
        card.background = background;
        card.preview = preview;
        card.nameText = nameText;
        card.statusText = statusText;
        card.selectButton = button;

        return card;
    }

    private static GameObject NewUIObject(string name, Transform parent, float width, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        return go;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, float height, float anchorY, float offsetY)
    {
        GameObject go = NewUIObject(name, parent, CardWidth - 24f, height);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, anchorY);
        rect.anchorMax = new Vector2(0.5f, anchorY);
        rect.pivot = new Vector2(0.5f, anchorY);
        rect.anchoredPosition = new Vector2(0f, offsetY);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
}
