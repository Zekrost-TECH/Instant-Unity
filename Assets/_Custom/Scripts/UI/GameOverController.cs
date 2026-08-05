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
    public TextMeshProUGUI eliteKillsText;
    public TextMeshProUGUI timeGainedText;
    public TextMeshProUGUI damageTakenText;
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

        AdsManager ads = AdsManager.Ensure();
        if (ads != null)
        {
            ads.OnAdReadyChanged += HandleAdReadyChanged;
            reviveAdReady = ads.IsAdReady;
        }

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

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnAdReadyChanged -= HandleAdReadyChanged;
    }

    private void HandleGameOver(float time, int kills, int cronos, bool newRecord)
    {
        // Cualquier flujo de revivir previo debe quedar cerrado antes de un nuevo game over.
        reviveInProgress = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (timeText != null)
            timeText.text = $"TIME {time:F1}s";

        if (killsText != null)
            killsText.text = $"KILLS {kills}";

        if (eliteKillsText != null)
            eliteKillsText.text = $"ELITES {(EnemyManager.Instance != null ? EnemyManager.Instance.EliteKillCount : 0)}";

        if (timeGainedText != null)
            timeGainedText.text = TimeManager.Instance != null ? $"GAINED +{TimeManager.Instance.TimeGainedThisRun:F1}s" : "GAINED +0.0s";

        if (damageTakenText != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerCombat combat = player != null ? player.GetComponent<PlayerCombat>() : null;
            damageTakenText.text = $"HITS {(combat != null ? combat.DamageTakenCount : 0)}";
        }

        if (cronosText != null)
            cronosText.text = $"Cronos +{cronos}";

        if (bestTimeText != null && SaveManager.Instance != null)
            bestTimeText.text = $"BEST {SaveManager.Instance.BestTime:F1}s";

        if (bestKillsText != null && SaveManager.Instance != null)
            bestKillsText.text = $"BEST KILLS {SaveManager.Instance.BestKills}";

        if (newRecordText != null)
            newRecordText.gameObject.SetActive(newRecord);

        SetButtonsInteractable(true);
    }

    private void OnRestartClicked()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        GameManager.Instance?.RestartGame();
    }

    private void OnReviveClicked()
    {
        // Guardia contra doble pulsación y contra revivir fuera del estado de Game Over
        // (p. ej. si un anuncio de una pulsación anterior aún se estaba resolviendo).
        if (reviveInProgress) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.GameOver) return;

        // El AdsManager no está en ninguna escena: sin esto Instance es null y el
        // botón se limitaba a ocultar el panel dejando la partida muerta.
        AdsManager ads = AdsManager.Ensure();
        if (ads == null || !ads.IsAdReady) return;

        // El panel se queda visible durante el anuncio; sólo se cierra al cobrar la
        // recompensa. Los botones se bloquean para no encadenar pulsaciones.
        reviveInProgress = true;
        SetButtonsInteractable(false);
        if (adPlayingLabel != null) adPlayingLabel.SetActive(true);

        ads.ShowRewardedAd(
            onRewardGranted: HandleReviveGranted,
            grantCronos: false,   // la recompensa es la partida, no monedas
            onFailed: HandleReviveFailed);
    }

    /// <summary>
    /// El jugador vio el anuncio hasta el final: se cierra el panel y se revive.
    /// Este callback llega del AdsManager SIEMPRE en el hilo main.
    /// </summary>
    private void HandleReviveGranted()
    {
        reviveInProgress = false;
        if (adPlayingLabel != null) adPlayingLabel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        SetButtonsInteractable(true);
        GameManager.Instance?.Revive();
    }

    /// <summary>
    /// Anuncio cerrado sin recompensa, fallo de carga o timeout del AdsManager:
    /// la partida sigue muerta pero la UI vuelve a estar operativa.
    /// </summary>
    private void HandleReviveFailed()
    {
        reviveInProgress = false;
        if (adPlayingLabel != null) adPlayingLabel.SetActive(false);
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (restartButton != null) restartButton.interactable = value;
        if (reviveButton != null) reviveButton.interactable = value && reviveAdReady;
        if (exitToMenuButton != null) exitToMenuButton.interactable = value;
    }

    private bool reviveAdReady;
    private bool reviveInProgress;

    private void HandleAdReadyChanged(bool ready)
    {
        reviveAdReady = ready;
        if (gameOverPanel != null && gameOverPanel.activeSelf)
            SetButtonsInteractable(true);
    }

    private void OnExitToMenuClicked()
    {
        SceneManager.LoadScene("0_MainMenu");
    }
}
