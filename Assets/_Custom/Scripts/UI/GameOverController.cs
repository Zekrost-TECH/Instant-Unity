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

    [Tooltip("Objeto opcional tipo 'Viendo anuncio...' que se muestra mientras dura el anuncio.")]
    public GameObject adPlayingLabel;

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

    private void HandleGameOver(float time, int kills, int cronos, bool newRecord)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (timeText != null)
            timeText.text = $"{time:F1}s";

        if (killsText != null)
            killsText.text = kills.ToString();

        if (cronosText != null)
            cronosText.text = $"+{cronos} \u27F3";

        if (bestTimeText != null && SaveManager.Instance != null)
            bestTimeText.text = $"Record: {SaveManager.Instance.BestTime:F1}s";

        if (bestKillsText != null && SaveManager.Instance != null)
            bestKillsText.text = $"Record: {SaveManager.Instance.BestKills}";

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
        // El AdsManager no está en ninguna escena: sin esto Instance es null y el
        // botón se limitaba a ocultar el panel dejando la partida muerta.
        AdsManager ads = AdsManager.Ensure();
        if (ads == null) return;

        // El panel se queda visible durante el anuncio; sólo se cierra al cobrar la
        // recompensa. Los botones se bloquean para no encadenar pulsaciones.
        SetButtonsInteractable(false);
        if (adPlayingLabel != null) adPlayingLabel.SetActive(true);

        ads.ShowRewardedAd(
            onRewardGranted: () =>
            {
                if (adPlayingLabel != null) adPlayingLabel.SetActive(false);
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                SetButtonsInteractable(true);
                GameManager.Instance?.Revive();
            },
            grantCronos: false,   // la recompensa es la partida, no monedas
            onFailed: () =>
            {
                if (adPlayingLabel != null) adPlayingLabel.SetActive(false);
                SetButtonsInteractable(true);
            });
    }

    private void SetButtonsInteractable(bool value)
    {
        if (restartButton != null) restartButton.interactable = value;
        if (reviveButton != null) reviveButton.interactable = value;
        if (exitToMenuButton != null) exitToMenuButton.interactable = value;
    }

    private void OnExitToMenuClicked()
    {
        SceneManager.LoadScene("0_MainMenu");
    }
}
