using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuUI : MonoBehaviour
{
    [Header("Top Display")]
    public TextMeshProUGUI bestTimeText;
    public TextMeshProUGUI bestKillsText;
    public TextMeshProUGUI walletCronosText;

    [Header("Main Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject scoreboardPanel;
    public GameObject upgradesPanel;
    public GameObject shopPanel;

    [Header("Settings Panel Elements")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public GameObject creditsSection;

    [Header("Upgrades Panel Elements")]
    public TextMeshProUGUI upgradeTimeLevelText;
    public TextMeshProUGUI upgradeTimeCostText;
    public Button upgradeTimeButton;

    public TextMeshProUGUI upgradeRangeLevelText;
    public TextMeshProUGUI upgradeRangeCostText;
    public Button upgradeRangeButton;

    public TextMeshProUGUI upgradeDashLevelText;
    public TextMeshProUGUI upgradeDashCostText;
    public Button upgradeDashButton;

    [Header("Shop Panel Elements")]
    // Skins Buttons / Texts
    public TextMeshProUGUI goldSkinBtnText;
    public Button goldSkinButton;
    
    public TextMeshProUGUI purpleSkinBtnText;
    public Button purpleSkinButton;
    
    public TextMeshProUGUI redSkinBtnText;
    public Button redSkinButton;
    
    public TextMeshProUGUI greenSkinBtnText;
    public Button greenSkinButton;

    public Button cyanSkinButton;
    public TextMeshProUGUI cyanSkinBtnText;

    [Header("Scoreboard List")]
    public Transform scoreboardContentParent;
    public GameObject scoreboardItemPrefab;

    private void Start()
    {
        // Asegurarse de que SaveManager y AudioManager existan
        // Si no existen en la escena (testeo directo), creamos dummies
        if (SaveManager.Instance == null)
        {
            GameObject smObj = new GameObject("SaveManager");
            smObj.AddComponent<SaveManager>();
        }

        // Configurar paneles
        CloseAllPanels();
        mainPanel.SetActive(true);

        // Inicializar sliders de volumen
        if (musicSlider != null && sfxSlider != null)
        {
            musicSlider.value = SaveManager.Instance.MusicVolume;
            sfxSlider.value = SaveManager.Instance.SFXVolume;

            musicSlider.onValueChanged.AddListener(OnVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        UpdateAllUI();
    }

    private void UpdateAllUI()
    {
        if (SaveManager.Instance == null) return;

        // Displays Principales
        if (bestTimeText != null)
            bestTimeText.text = $"{SaveManager.Instance.BestTime:F1}s";
        
        if (bestKillsText != null)
            bestKillsText.text = $"Kills: {SaveManager.Instance.BestKills}";

        if (walletCronosText != null)
            walletCronosText.text = SaveManager.Instance.Cronos.ToString();

        UpdateUpgradesUI();
        UpdateShopUI();
    }

    // --- ACCIONES PRINCIPALES ---

    public void ActionPlay()
    {
        // Cargar escena del juego
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

    // --- MANEJO DE PANELES ---

    public void CloseAllPanels()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if (upgradesPanel != null) upgradesPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
        if (creditsSection != null) creditsSection.SetActive(false);
    }

    public void ToggleCredits()
    {
        if (creditsSection != null)
        {
            creditsSection.SetActive(!creditsSection.activeSelf);
        }
    }

    public void OpenScoreboard()
    {
        CloseAllPanels();
        scoreboardPanel.SetActive(true);
        PopulateScoreboard();
    }

    public void OpenUpgrades()
    {
        CloseAllPanels();
        upgradesPanel.SetActive(true);
        UpdateUpgradesUI();
    }

    public void OpenShop()
    {
        CloseAllPanels();
        shopPanel.SetActive(true);
        UpdateShopUI();
    }

    public void BackToMain()
    {
        CloseAllPanels();
        UpdateAllUI();
    }

    // --- CONFIGURACIÓN ---

    private void OnVolumeChanged(float val)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetVolume(musicSlider.value, sfxSlider.value);
        }
    }

    // --- SCOREBOARD / RECORDS ---

    private void PopulateScoreboard()
    {
        // Limpiar lista anterior
        if (scoreboardContentParent != null)
        {
            foreach (Transform child in scoreboardContentParent)
            {
                Destroy(child.gameObject);
            }
        }

        // Crear una lista ficticia premium para el reto (Scoreboard Online/Mock)
        List<ScoreEntry> scores = new List<ScoreEntry>()
        {
            new ScoreEntry("Zekrost_TECH", 245.5f, 412),
            new ScoreEntry("AlphaTime", 188.2f, 320),
            new ScoreEntry("ChronosKing", 132.8f, 215),
            new ScoreEntry("T-Dimension", 98.4f, 150),
            new ScoreEntry("Tú (Récord)", SaveManager.Instance.BestTime, SaveManager.Instance.BestKills)
        };

        // Ordenar por tiempo de supervivencia de forma descendente
        scores.Sort((a, b) => b.time.CompareTo(a.time));

        // Instanciar elementos si tenemos prefab
        if (scoreboardItemPrefab != null && scoreboardContentParent != null)
        {
            int rank = 1;
            foreach (var entry in scores)
            {
                GameObject item = Instantiate(scoreboardItemPrefab, scoreboardContentParent);
                // Buscar componentes de texto en el prefab (pueden ser hijos)
                TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 4)
                {
                    texts[0].text = $"#{rank}";
                    texts[1].text = entry.name;
                    texts[2].text = $"{entry.time:F1}s";
                    texts[3].text = $"{entry.kills} Kills";
                }
                rank++;
            }
        }
    }

    private struct ScoreEntry
    {
        public string name;
        public float time;
        public int kills;

        public ScoreEntry(string n, float t, int k)
        {
            name = n;
            time = t;
            kills = k;
        }
    }

    // --- UPGRADES PERMANENTES (BOTÓN CRONOS) ---

    private void UpdateUpgradesUI()
    {
        if (SaveManager.Instance == null) return;

        // Upgrade Starting Time
        int timeLvl = SaveManager.Instance.StartingTimeLevel;
        if (upgradeTimeLevelText != null) upgradeTimeLevelText.text = $"Lvl {timeLvl}/5\n(+{timeLvl * 4}s iniciales)";
        if (timeLvl >= 5)
        {
            if (upgradeTimeCostText != null) upgradeTimeCostText.text = "MÁXIMO";
            if (upgradeTimeButton != null) upgradeTimeButton.interactable = false;
        }
        else
        {
            int cost = (timeLvl + 1) * 150;
            if (upgradeTimeCostText != null) upgradeTimeCostText.text = $"{cost} CRONOS";
            if (upgradeTimeButton != null) upgradeTimeButton.interactable = SaveManager.Instance.Cronos >= cost;
        }

        // Upgrade Attack Range
        int rangeLvl = SaveManager.Instance.AttackRangeLevel;
        if (upgradeRangeLevelText != null) upgradeRangeLevelText.text = $"Lvl {rangeLvl}/5\n(+{rangeLvl * 5}% Rango)";
        if (rangeLvl >= 5)
        {
            if (upgradeRangeCostText != null) upgradeRangeCostText.text = "MÁXIMO";
            if (upgradeRangeButton != null) upgradeRangeButton.interactable = false;
        }
        else
        {
            int cost = (rangeLvl + 1) * 150;
            if (upgradeRangeCostText != null) upgradeRangeCostText.text = $"{cost} CRONOS";
            if (upgradeRangeButton != null) upgradeRangeButton.interactable = SaveManager.Instance.Cronos >= cost;
        }

        // Upgrade Dash Cooldown
        int dashLvl = SaveManager.Instance.DashCooldownLevel;
        if (upgradeDashLevelText != null) upgradeDashLevelText.text = $"Lvl {dashLvl}/5\n(-{dashLvl * 8}% Cooldown)";
        if (dashLvl >= 5)
        {
            if (upgradeDashCostText != null) upgradeDashCostText.text = "MÁXIMO";
            if (upgradeDashButton != null) upgradeDashButton.interactable = false;
        }
        else
        {
            int cost = (dashLvl + 1) * 150;
            if (upgradeDashCostText != null) upgradeDashCostText.text = $"{cost} CRONOS";
            if (upgradeDashButton != null) upgradeDashButton.interactable = SaveManager.Instance.Cronos >= cost;
        }
    }

    public void ActionUpgradeTime()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.UpgradeStartingTime())
        {
            UpdateAllUI();
        }
    }

    public void ActionUpgradeRange()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.UpgradeAttackRange())
        {
            UpdateAllUI();
        }
    }

    public void ActionUpgradeDash()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.UpgradeDashCooldown())
        {
            UpdateAllUI();
        }
    }

    // --- TIENDA DE SKINS Y CRONOS ---

    private void UpdateShopUI()
    {
        if (SaveManager.Instance == null) return;

        UpdateSkinButton(cyanSkinButton, cyanSkinBtnText, "Cyan", 0);
        UpdateSkinButton(goldSkinButton, goldSkinBtnText, "Gold", 500);
        UpdateSkinButton(purpleSkinButton, purpleSkinBtnText, "Purple", 300);
        UpdateSkinButton(redSkinButton, redSkinBtnText, "Red", 200);
        UpdateSkinButton(greenSkinButton, greenSkinBtnText, "Green", 100);
    }

    private void UpdateSkinButton(Button btn, TextMeshProUGUI text, string skinName, int cost)
    {
        if (btn == null || text == null) return;

        bool isUnlocked = SaveManager.Instance.IsSkinUnlocked(skinName);
        bool isEquipped = SaveManager.Instance.EquippedSkin == skinName;

        if (isEquipped)
        {
            text.text = "EQUIPADA";
            btn.interactable = false;
        }
        else if (isUnlocked)
        {
            text.text = "EQUIPAR";
            btn.interactable = true;
        }
        else
        {
            text.text = $"{cost} C";
            btn.interactable = SaveManager.Instance.Cronos >= cost;
        }
    }

    public void ActionSkinClick(string skinName)
    {
        if (SaveManager.Instance == null) return;

        int cost = 0;
        switch (skinName)
        {
            case "Green": cost = 100; break;
            case "Red": cost = 200; break;
            case "Purple": cost = 300; break;
            case "Gold": cost = 500; break;
        }

        if (SaveManager.Instance.IsSkinUnlocked(skinName))
        {
            SaveManager.Instance.EquipSkin(skinName);
        }
        else
        {
            if (SaveManager.Instance.UnlockSkin(skinName, cost))
            {
                SaveManager.Instance.EquipSkin(skinName);
            }
        }

        UpdateAllUI();
    }

    // Compra de monedas (Tienda de Cronos - Virtual Mock)
    public void ActionAddCronos(int amount)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AddCronos(amount);
            UpdateAllUI();
        }
    }
}
