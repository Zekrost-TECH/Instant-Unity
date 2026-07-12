using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverController : MonoBehaviour
{
    [Header("Game Over Panel")]
    public GameObject gameOverPanel;

    [Header("Stats Texts (Optional)")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI cronosText;
    public TextMeshProUGUI bestTimeText;
    public TextMeshProUGUI bestKillsText;
    public TextMeshProUGUI newRecordText;

    [Header("Buttons")]
    public Button restartButton;
    public Button reviveButton;
    public Button exitToMenuButton;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (reviveButton != null)
            reviveButton.onClick.AddListener(OnReviveClicked);

        if (exitToMenuButton != null)
            exitToMenuButton.onClick.AddListener(OnExitToMenuClicked);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (reviveButton != null)
            reviveButton.onClick.RemoveListener(OnReviveClicked);

        if (exitToMenuButton != null)
            exitToMenuButton.onClick.RemoveListener(OnExitToMenuClicked);
    }

    private void HandleGameOver(float time, int kills, int cronos)
    {
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
    }

    private void OnRestartClicked()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        GameManager.Instance?.RestartGame();
    }

    private void OnReviveClicked()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        AdsManager.Instance?.ShowRewardedAd(() =>
        {
            GameManager.Instance?.Revive();
        });
    }

    private void OnExitToMenuClicked()
    {
        SceneManager.LoadScene("0_MainMenu");
    }
}
