using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinShopUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Raíz del panel de tienda. Se activa/desactiva al abrir y cerrar.")]
    public GameObject shopPanel;
    public Button closeButton;

    [Header("Contenido")]
    [Tooltip("Contenedor de las tarjetas. Ponle un Grid Layout Group.")]
    public Transform cardsContainer;
    [Tooltip("Prefab de tarjeta. Si se deja vacío se genera una tarjeta básica en runtime.")]
    public SkinCardUI cardPrefab;
    [Tooltip("Sprite del personaje que se usa como preview cuando la skin no trae icono propio.")]
    public Sprite defaultSkinSprite;

    [Header("Cartera")]
    public TextMeshProUGUI walletText;
    public string walletFormat = "{0}";

    [Header("Feedback")]
    [Tooltip("Texto opcional que avisa cuando no hay Cronos suficientes.")]
    public TextMeshProUGUI messageText;
    public float messageDuration = 1.5f;

    private readonly List<SkinCardUI> cards = new List<SkinCardUI>();
    private float messageTimer = 0f;
    private bool built = false;
    private bool openRequested = false;

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

    private void Awake()
    {
        SaveManager.Ensure();
        SkinManager.Ensure();
    }

    private void Start()
    {
        // Si el componente vive en el propio panel, Start corre justo DESPUÉS de que
        // Open() lo active: cerrar aquí a ciegas lo volvería a ocultar al instante.
        if (shopPanel != null && !openRequested) shopPanel.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (SkinManager.Instance != null)
            SkinManager.Instance.OnCatalogChanged += RefreshAll;
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);

        if (SkinManager.Instance != null)
            SkinManager.Instance.OnCatalogChanged -= RefreshAll;
    }

    private void Update()
    {
        if (messageTimer <= 0f) return;

        messageTimer -= Time.unscaledDeltaTime;
        if (messageTimer <= 0f && messageText != null)
            messageText.gameObject.SetActive(false);
    }

    // ── Abrir / cerrar ───────────────────────────────────────────────────────

    public void Open()
    {
        openRequested = true;

        SaveManager.Ensure();
        SkinManager.Ensure();

        if (shopPanel != null) shopPanel.SetActive(true);

        BuildCards();
        RefreshAll();
    }

    public void Close()
    {
        openRequested = false;
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    // ── Construcción ─────────────────────────────────────────────────────────

    private void BuildCards()
    {
        if (built || cardsContainer == null || SkinManager.Instance == null) return;
        built = true;

        IReadOnlyList<SkinDefinition> skins = SkinManager.Instance.Skins;
        for (int i = 0; i < skins.Count; i++)
        {
            SkinDefinition skin = skins[i];
            if (skin == null) continue;

            SkinCardUI card = cardPrefab != null
                ? Instantiate(cardPrefab, cardsContainer)
                : SkinCardFactory.Create(cardsContainer, defaultSkinSprite);

            card.Setup(skin, defaultSkinSprite, HandleCardSelected);
            cards.Add(card);
        }
    }

    private void RefreshAll()
    {
        SkinManager manager = SkinManager.Instance;
        if (manager == null) return;

        for (int i = 0; i < cards.Count; i++)
        {
            SkinCardUI card = cards[i];
            if (card == null) continue;

            string id = card.SkinId;
            card.Refresh(manager.IsSkinUnlocked(id), manager.IsEquipped(id), manager.CanAfford(id));
        }

        RefreshWallet();
    }

    private void RefreshWallet()
    {
        if (walletText == null) return;

        int cronos = SaveManager.Instance != null ? SaveManager.Instance.Cronos : 0;
        walletText.SetText(walletFormat, cronos);
    }

    // ── Interacción ──────────────────────────────────────────────────────────

    private void HandleCardSelected(SkinDefinition skin)
    {
        SkinManager manager = SkinManager.Instance;
        if (manager == null || skin == null) return;

        bool alreadyOwned = manager.IsSkinUnlocked(skin.id);

        if (!alreadyOwned && !manager.CanAfford(skin.id))
        {
            ShowMessage("Not enough Cronos");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeMissedSFX, 0.8f);
            return;
        }

        if (!manager.PurchaseOrEquip(skin.id))
        {
            ShowMessage("Could not equip");
            return;
        }

        ShowMessage(alreadyOwned ? $"{skin.displayName} equipped" : $"{skin.displayName} unlocked!");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSelectSFX, 0.9f);

        // PurchaseOrEquip ya dispara OnCatalogChanged → RefreshAll
    }

    private void ShowMessage(string text)
    {
        if (messageText == null) return;

        messageText.SetText(text);
        messageText.gameObject.SetActive(true);
        messageTimer = messageDuration;
    }
}
