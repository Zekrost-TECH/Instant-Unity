using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverController : MonoBehaviour
{
    public static bool OpenShopOnLoad = false;
    [Header("Game Over Panel")]
    public GameObject gameOverPanel;

    [Header("Stats Texts")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI cronosText;
    public TextMeshProUGUI bestTimeText;
    public TextMeshProUGUI bestKillsText;
    public TextMeshProUGUI newRecordText;

    [Header("Buttons")]
    public Button watchAdButton;
    public Button restartButton;
    public Button shopButton;

    private float lastRunTime;
    private int lastRunKills;
    private int lastRunCronos;
    private bool rewardedAdShown = false;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAdClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (shopButton != null)
            shopButton.onClick.AddListener(OnShopClicked);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }

        if (watchAdButton != null)
            watchAdButton.onClick.RemoveListener(OnWatchAdClicked);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (shopButton != null)
            shopButton.onClick.RemoveListener(OnShopClicked);
    }

    private void HandleGameOver(float time, int kills, int cronos)
    {
        lastRunTime = time;
        lastRunKills = kills;
        lastRunCronos = cronos;
        rewardedAdShown = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (timeText != null)
            timeText.text = $"{time:F1}s";

        if (killsText != null)
            killsText.text = kills.ToString();

        if (cronosText != null)
            cronosText.text = $"+{cronos} ⟳";

        if (bestTimeText != null && SaveManager.Instance != null)
            bestTimeText.text = $"Récord: {SaveManager.Instance.BestTime:F1}s";

        if (bestKillsText != null && SaveManager.Instance != null)
            bestKillsText.text = $"Récord: {SaveManager.Instance.BestKills}";

        bool newRecord = false;
        if (SaveManager.Instance != null)
        {
            newRecord = time > SaveManager.Instance.BestTime || kills > SaveManager.Instance.BestKills;
        }

        if (newRecordText != null)
            newRecordText.gameObject.SetActive(newRecord);

        UpdateWatchAdButton();
    }

    private void UpdateWatchAdButton()
    {
        if (watchAdButton == null) return;
        watchAdButton.interactable = !rewardedAdShown && (AdsManager.Instance?.IsAdReady ?? false);
    }

    private void OnWatchAdClicked()
    {
        if (rewardedAdShown) return;

        AdsManager.Instance?.ShowRewardedAd(() =>
        {
            rewardedAdShown = true;
            UpdateWatchAdButton();
        });
    }

    private void OnRestartClicked()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        GameManager.Instance?.RestartGame();
    }

    private void OnShopClicked()
    {
        OpenShopOnLoad = true;
        SceneManager.LoadScene("0_MainMenu");
    }
}
