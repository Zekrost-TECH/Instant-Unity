using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class MainMenuResponsiveLayout : MonoBehaviour
{
    [SerializeField] private Vector2 designResolution = new Vector2(1920f, 1080f);
    [SerializeField] private Color modalBackdropColor = new Color(0.025f, 0.03f, 0.05f, 0.98f);

    private RectTransform canvasRect;
    private RectTransform mainViewport;
    private RectTransform mainContent;
    private GameObject modalBackdrop;
    private MainMenuUI menu;
    private readonly List<RectTransform> modalContents = new List<RectTransform>(4);

    private void Awake()
    {
        canvasRect = transform as RectTransform;
        menu = GetComponent<MainMenuUI>();

        ConfigureCanvasScaler();
        CreateMainContent();
        PrepareModalContent(menu != null && menu.skinShop != null ? GetPanel(menu.skinShop.shopPanel) : null);
        PrepareModalContent(menu != null && menu.settingsUI != null ? GetPanel(menu.settingsUI.settingsPanel) : null);
        PrepareModalContent(menu != null && menu.recordsUI != null ? GetPanel(menu.recordsUI.recordsPanel) : null);
        PrepareModalContent(menu != null && menu.progressionUI != null ? GetPanel(menu.progressionUI.progressionPanel) : null);
        CreateModalBackdrop();
    }

    private void Start()
    {
        RefreshLayout();
        SyncModalBackdrop();
    }

    private void LateUpdate()
    {
        RefreshLayout();
        SyncModalBackdrop();
    }

    private void ConfigureCanvasScaler()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = designResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
    }

    private void CreateMainContent()
    {
        Transform existingViewport = transform.Find("ResponsiveContentViewport");
        if (existingViewport != null)
        {
            mainViewport = existingViewport as RectTransform;
            mainContent = mainViewport != null ? mainViewport.Find("ResponsiveContent") as RectTransform : null;
        }

        if (mainViewport == null)
        {
            GameObject viewportObject = new GameObject("ResponsiveContentViewport", typeof(RectTransform));
            mainViewport = viewportObject.transform as RectTransform;
            mainViewport.SetParent(transform, false);
            SetFullRect(mainViewport);
            viewportObject.AddComponent<SafeAreaFitter>();
        }

        if (mainContent == null)
        {
            GameObject contentObject = new GameObject("ResponsiveContent", typeof(RectTransform));
            mainContent = contentObject.transform as RectTransform;
            mainContent.SetParent(mainViewport, false);
            SetFullRect(mainContent);
        }

        List<Transform> childrenToMove = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == mainViewport || child == modalBackdrop || child.name == "ResponsiveContentViewport" || child.name == "ModalBackdrop") continue;
            if (child.name == "BackGround") continue;
            if (IsModalWrapper(child.name)) continue;
            childrenToMove.Add(child);
        }

        for (int i = 0; i < childrenToMove.Count; i++)
            childrenToMove[i].SetParent(mainContent, false);

        mainViewport.SetSiblingIndex(1);
    }

    private void PrepareModalContent(RectTransform panel)
    {
        if (panel == null) return;

        Transform existing = panel.Find("ResponsiveContent");
        RectTransform content = existing as RectTransform;
        if (content == null)
        {
            GameObject contentObject = new GameObject("ResponsiveContent", typeof(RectTransform));
            content = contentObject.transform as RectTransform;
            content.SetParent(panel, false);
            SetFullRect(content);

            List<Transform> childrenToMove = new List<Transform>();
            for (int i = 0; i < panel.childCount; i++)
            {
                Transform child = panel.GetChild(i);
                if (child.name == "PageBackground" || child == content) continue;
                childrenToMove.Add(child);
            }

            for (int i = 0; i < childrenToMove.Count; i++)
                childrenToMove[i].SetParent(content, false);

            content.SetSiblingIndex(1);
        }

        if (!modalContents.Contains(content))
            modalContents.Add(content);
    }

    private void CreateModalBackdrop()
    {
        Transform existing = transform.Find("ModalBackdrop");
        if (existing != null)
        {
            modalBackdrop = existing.gameObject;
        }
        else
        {
            modalBackdrop = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform backdropRect = modalBackdrop.transform as RectTransform;
            backdropRect.SetParent(transform, false);
            SetFullRect(backdropRect);

            Image image = modalBackdrop.GetComponent<Image>();
            image.color = modalBackdropColor;
            image.raycastTarget = true;
        }

        int firstModalIndex = transform.childCount;
        string[] modalNames = { "SkinShop", "Settings", "Records", "PermanentProgression" };
        for (int i = 0; i < modalNames.Length; i++)
        {
            Transform modal = transform.Find(modalNames[i]);
            if (modal != null)
                firstModalIndex = Mathf.Min(firstModalIndex, modal.GetSiblingIndex());
        }

        modalBackdrop.transform.SetSiblingIndex(firstModalIndex);
        modalBackdrop.SetActive(false);
    }

    private void RefreshLayout()
    {
        if (canvasRect == null) return;

        float mainScale = CalculateFitScale(mainViewport != null ? mainViewport.rect.size : canvasRect.rect.size);
        if (mainContent != null)
            mainContent.localScale = Vector3.one * mainScale;

        for (int i = 0; i < modalContents.Count; i++)
        {
            RectTransform content = modalContents[i];
            if (content == null || content.parent == null) continue;
            content.localScale = Vector3.one * CalculateFitScale((content.parent as RectTransform).rect.size);
        }
    }

    private float CalculateFitScale(Vector2 availableSize)
    {
        if (availableSize.x <= 0f || availableSize.y <= 0f) return 1f;

        float widthScale = availableSize.x / designResolution.x;
        float heightScale = availableSize.y / designResolution.y;
        return Mathf.Clamp01(Mathf.Min(widthScale, heightScale));
    }

    private void SyncModalBackdrop()
    {
        if (modalBackdrop == null || menu == null) return;

        bool modalOpen =
            (menu.skinShop != null && menu.skinShop.IsOpen) ||
            (menu.settingsUI != null && menu.settingsUI.IsOpen) ||
            (menu.recordsUI != null && menu.recordsUI.IsOpen) ||
            (menu.progressionUI != null && menu.progressionUI.IsOpen);

        if (modalBackdrop.activeSelf != modalOpen)
            modalBackdrop.SetActive(modalOpen);
    }

    private static void SetFullRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static RectTransform GetPanel(GameObject panel)
    {
        return panel != null ? panel.transform as RectTransform : null;
    }

    private static bool IsModalWrapper(string objectName)
    {
        return objectName == "SkinShop" ||
               objectName == "Settings" ||
               objectName == "Records" ||
               objectName == "PermanentProgression";
    }
}
