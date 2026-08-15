using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeResponsiveLayout : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float cardHeightRatio = 1.5f;
    [SerializeField] private float horizontalMargin = 80f;
    [SerializeField] private float bottomMargin = 42f;
    [SerializeField] private float maxCardWidth = 560f;
    [SerializeField] private float maxCardHeight = 760f;

    private RectTransform panel;
    private RectTransform cardsContainer;
    private RectTransform titleRect;
    private RectTransform progressRect;
    private TextMeshProUGUI titleText;
    private GridLayoutGroup grid;
    private HorizontalLayoutGroup horizontalLayout;
    private List<UpgradeCardUI> activeCards;
    private Canvas canvas;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private Vector2 lastPanelSize;

    public void Configure(GameObject panelObject, Transform cards, TextMeshProUGUI title, GameObject progressBarObject)
    {
        panel = panelObject != null ? panelObject.transform as RectTransform : null;
        cardsContainer = cards as RectTransform;
        titleText = title;
        titleRect = title != null ? title.rectTransform : null;
        progressRect = progressBarObject != null ? progressBarObject.transform as RectTransform : null;
        canvas = panel != null ? panel.GetComponentInParent<Canvas>()?.rootCanvas : null;

        if (cardsContainer == null) return;

        horizontalLayout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayout != null) horizontalLayout.enabled = false;

        grid = cardsContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = cardsContainer.gameObject.AddComponent<GridLayoutGroup>();
        grid.enabled = false;
    }

    public void Refresh(List<UpgradeCardUI> cards)
    {
        activeCards = cards;
        ApplyLayout(force: true);
    }

    private void LateUpdate()
    {
        if (panel == null || !panel.gameObject.activeInHierarchy) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = Screen.safeArea;
        Vector2 panelSize = panel.rect.size;
        if (safeArea == lastSafeArea &&
            Screen.width == lastScreenSize.x &&
            Screen.height == lastScreenSize.y &&
            panelSize == lastPanelSize)
            return;

        ApplyLayout(force: false);
    }

    private void ApplyLayout(bool force)
    {
        if (panel == null || cardsContainer == null || grid == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = Screen.safeArea;
        Vector2 safeMin;
        Vector2 safeMax;
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, safeArea.min, eventCamera, out safeMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, safeArea.max, eventCamera, out safeMax))
        {
            safeMin = panel.rect.min;
            safeMax = panel.rect.max;
        }

        float safeWidth = Mathf.Max(1f, safeMax.x - safeMin.x);
        float safeHeight = Mathf.Max(1f, safeMax.y - safeMin.y);
        Vector2 safeCenter = (safeMin + safeMax) * 0.5f;
        float spacing = Mathf.Clamp(Mathf.Min(safeWidth, safeHeight) * 0.025f, 18f, 42f);
        float titleHeight = Mathf.Clamp(safeHeight * 0.075f, 52f, 82f);
        float topInset = Mathf.Clamp(safeHeight * 0.13f, 96f, 156f);
        float titleY = safeMax.y - topInset - titleHeight * 0.5f;
        float progressHeight = 22f;
        float progressY = titleY - titleHeight * 0.5f - 26f - progressHeight * 0.5f;
        float cardsTop = progressY - progressHeight * 0.5f - 34f;
        float cardsBottom = safeMin.y + bottomMargin;
        float availableWidth = Mathf.Max(1f, safeWidth - horizontalMargin * 2f);
        float availableHeight = Mathf.Max(1f, cardsTop - cardsBottom);
        int cardCount = activeCards != null ? Mathf.Max(1, activeCards.Count) : 3;

        int bestColumns = 1;
        float bestCardWidth = 1f;
        float bestCardHeight = 1f;
        float bestArea = -1f;
        int maxColumns = Mathf.Min(3, cardCount);

        for (int columns = 1; columns <= maxColumns; columns++)
        {
            int rows = Mathf.CeilToInt(cardCount / (float)columns);
            float width = (availableWidth - spacing * (columns - 1)) / columns;
            float height = (availableHeight - spacing * (rows - 1)) / rows;
            float cardWidth = Mathf.Min(width, height / cardHeightRatio, maxCardWidth);
            float cardHeight = Mathf.Min(cardWidth * cardHeightRatio, maxCardHeight);
            float area = cardWidth * cardHeight;

            if (cardWidth <= 0f || cardHeight <= 0f) continue;
            if (area > bestArea || (Mathf.Approximately(area, bestArea) && columns > bestColumns))
            {
                bestColumns = columns;
                bestCardWidth = cardWidth;
                bestCardHeight = cardHeight;
                bestArea = area;
            }
        }

        int rowCount = Mathf.CeilToInt(cardCount / (float)bestColumns);
        float containerWidth = bestColumns * bestCardWidth + spacing * (bestColumns - 1);
        float containerHeight = rowCount * bestCardHeight + spacing * (rowCount - 1);

        SetCenteredRect(cardsContainer, safeCenter + new Vector2(0f, (cardsTop + cardsBottom) * 0.5f - safeCenter.y), new Vector2(containerWidth, containerHeight));
        if (titleRect != null)
        {
            SetCenteredRect(titleRect, new Vector2(safeCenter.x, titleY), new Vector2(Mathf.Min(safeWidth - 80f, 720f), titleHeight));
            if (titleText != null)
            {
                titleText.enableAutoSizing = true;
                titleText.fontSizeMin = Mathf.Clamp(18f / Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f), 28f, 52f);
                titleText.fontSizeMax = titleText.fontSizeMin * 1.25f;
                titleText.maxVisibleLines = 1;
            }
        }

        if (progressRect != null)
            SetCenteredRect(progressRect, new Vector2(safeCenter.x, progressY), new Vector2(Mathf.Min(safeWidth * 0.34f, 520f), progressHeight));

        if (activeCards != null)
        {
            float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i] != null)
                {
                    activeCards[i].ApplyResponsiveSizing(new Vector2(bestCardWidth, bestCardHeight), canvasScale);

                    int column = i % bestColumns;
                    int row = i / bestColumns;
                    float x = (column - (bestColumns - 1) * 0.5f) * (bestCardWidth + spacing);
                    float y = ((rowCount - 1) * 0.5f - row) * (bestCardHeight + spacing);
                    SetCenteredRect(activeCards[i].transform as RectTransform, new Vector2(x, y), new Vector2(bestCardWidth, bestCardHeight));
                }
            }
        }

        Canvas.ForceUpdateCanvases();

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastPanelSize = panel.rect.size;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
