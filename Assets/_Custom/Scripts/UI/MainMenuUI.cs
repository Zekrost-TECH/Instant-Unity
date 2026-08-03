using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Top Display")]
    public TextMeshProUGUI bestTimeText;
    public TextMeshProUGUI bestKillsText;
    public TextMeshProUGUI walletCronosText;

    [Header("Personaje")]
    [Tooltip("El Image del centro del menú (PlayerIcon). Se tiñe con la skin equipada.")]
    public Image playerIcon;

    [Header("Tienda")]
    public SkinShopUI skinShop;

    [Header("Ajustes")]
    public SettingsUI settingsUI;

    [Header("Records")]
    public RecordsUI recordsUI;

    [Header("Progression")]
    public PermanentProgressionUI progressionUI;

    [Header("Debug")]
    [Tooltip("Cronos que regala DebugAddCronos(). Sólo para probar la tienda.")]
    public int debugCronosAmount = 100;

    private void Awake()
    {
        // La escena 0_MainMenu no lleva managers: sin esto no hay Cronos ni skins.
        SaveManager.Ensure();
        SkinManager.Ensure();
    }

    private void Start()
    {
        if (SkinManager.Instance != null)
        {
            SkinManager.Instance.OnPlayerSkinChanged += ApplyEquippedSkin;
            SkinManager.Instance.OnCatalogChanged += UpdateDisplay;
        }

        ApplyEquippedSkin();
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (SkinManager.Instance != null)
        {
            SkinManager.Instance.OnPlayerSkinChanged -= ApplyEquippedSkin;
            SkinManager.Instance.OnCatalogChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay()
    {
        if (SaveManager.Instance == null) return;

        if (bestTimeText != null)
            bestTimeText.SetText("{0:F1}s", SaveManager.Instance.BestTime);

        if (bestKillsText != null)
            bestKillsText.SetText("Record: {0}", SaveManager.Instance.BestKills);

        if (walletCronosText != null)
            walletCronosText.SetText("{0}", SaveManager.Instance.Cronos);
    }

    /// <summary>Tiñe el icono central con el color de la skin equipada.</summary>
    private void ApplyEquippedSkin()
    {
        if (playerIcon == null || SkinManager.Instance == null) return;

        SkinDefinition equipped = SkinManager.Instance.GetEquippedSkinDefinition();
        if (equipped == null) return;

        if (equipped.icon != null) playerIcon.sprite = equipped.icon;
        playerIcon.color = equipped.color;
    }

    // ── Botones ──────────────────────────────────────────────────────────────

    public void ActionPlay()
    {
        SceneManager.LoadScene("1_Game");
    }

    public void ActionExit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void OpenShop()
    {
        if (skinShop != null) skinShop.Open();
    }

    public void CloseShop()
    {
        if (skinShop != null) skinShop.Close();
    }

    public void OpenRecords()
    {
        if (recordsUI != null) recordsUI.Open();
    }

    public void CloseRecords()
    {
        if (recordsUI != null) recordsUI.Close();
    }

    public void OpenProgression()
    {
        if (progressionUI != null) progressionUI.Open();
    }

    public void CloseProgression()
    {
        if (progressionUI != null) progressionUI.Close();
    }

    public void OpenSettings()
    {
        if (settingsUI != null) settingsUI.Open();
    }

    public void CloseSettings()
    {
        if (settingsUI != null) settingsUI.Close();
    }

    /// <summary>
    /// Botón de debug: regala Cronos para poder probar la compra de skins sin jugar
    /// partidas. Acuérdate de quitarlo del botón antes de publicar.
    /// </summary>
    public void DebugAddCronos()
    {
        if (!Debug.isDebugBuild) return;

        SaveManager.Ensure();
        if (SaveManager.Instance == null) return;

        SaveManager.Instance.AddCronos(debugCronosAmount);
        UpdateDisplay();

        // Si la tienda está abierta, reevaluar precios y botones al momento.
        if (skinShop != null && skinShop.IsOpen) skinShop.Open();

        Debug.Log($"[DEBUG] +{debugCronosAmount} Cronos. Total: {SaveManager.Instance.Cronos}");
    }
}
