using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PermanentProgressionUI : MonoBehaviour
{
    public static PermanentProgressionUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject progressionPanel;
    public Button closeButton;
    public TextMeshProUGUI walletText;

    [Header("Starting Time")]
    public TextMeshProUGUI startingTimeLevelText;
    public TextMeshProUGUI startingTimeEffectText;
    public Button startingTimeButton;

    [Header("Attack Range")]
    public TextMeshProUGUI attackRangeLevelText;
    public TextMeshProUGUI attackRangeEffectText;
    public Button attackRangeButton;

    [Header("Dash Cooldown")]
    public TextMeshProUGUI dashCooldownLevelText;
    public TextMeshProUGUI dashCooldownEffectText;
    public Button dashCooldownButton;

    private const int MaxLevel = 5;

    public bool IsOpen => progressionPanel != null && progressionPanel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        SaveManager.Ensure();

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (startingTimeButton != null) startingTimeButton.onClick.AddListener(BuyStartingTime);
        if (attackRangeButton != null) attackRangeButton.onClick.AddListener(BuyAttackRange);
        if (dashCooldownButton != null) dashCooldownButton.onClick.AddListener(BuyDashCooldown);
    }

    private void Start()
    {
        if (progressionPanel != null) progressionPanel.SetActive(false);
        Refresh();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (startingTimeButton != null) startingTimeButton.onClick.RemoveListener(BuyStartingTime);
        if (attackRangeButton != null) attackRangeButton.onClick.RemoveListener(BuyAttackRange);
        if (dashCooldownButton != null) dashCooldownButton.onClick.RemoveListener(BuyDashCooldown);

        if (Instance == this) Instance = null;
    }

    public void Open()
    {
        SaveManager.Ensure();
        Refresh();
        if (progressionPanel != null) progressionPanel.SetActive(true);
    }

    public void Close()
    {
        if (progressionPanel != null) progressionPanel.SetActive(false);
    }

    public void BuyStartingTime()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.UpgradeStartingTime())
            Refresh();
    }

    public void BuyAttackRange()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.UpgradeAttackRange())
            Refresh();
    }

    public void BuyDashCooldown()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.UpgradeDashCooldown())
            Refresh();
    }

    public void Refresh()
    {
        if (SaveManager.Instance == null) return;

        int cronos = SaveManager.Instance.Cronos;
        if (walletText != null) walletText.SetText("Cronos: {0}", cronos);

        SetRow(
            startingTimeLevelText,
            startingTimeEffectText,
            startingTimeButton,
            SaveManager.Instance.StartingTimeLevel,
            $"Current: +{SaveManager.Instance.StartingTimeLevel * 2}s | Next: +{(SaveManager.Instance.StartingTimeLevel + 1) * 2}s",
            cronos);

        SetRow(
            attackRangeLevelText,
            attackRangeEffectText,
            attackRangeButton,
            SaveManager.Instance.AttackRangeLevel,
            $"Current: +{SaveManager.Instance.AttackRangeLevel * 7}% | Next: +{(SaveManager.Instance.AttackRangeLevel + 1) * 7}%",
            cronos);

        SetRow(
            dashCooldownLevelText,
            dashCooldownEffectText,
            dashCooldownButton,
            SaveManager.Instance.DashCooldownLevel,
            $"Current: -{SaveManager.Instance.DashCooldownLevel * 6}% | Next: -{(SaveManager.Instance.DashCooldownLevel + 1) * 6}%",
            cronos);
    }

    private void SetRow(TextMeshProUGUI levelText, TextMeshProUGUI effectText, Button button, int level, string effect, int cronos)
    {
        int cost = (level + 1) * 100;
        bool canBuy = level < MaxLevel && cronos >= cost;

        if (levelText != null) levelText.SetText("LEVEL {0}/{1}", level, MaxLevel);
        if (effectText != null) effectText.text = level >= MaxLevel ? "MAX LEVEL" : effect;
        if (button != null) button.interactable = canBuy;
    }
}
