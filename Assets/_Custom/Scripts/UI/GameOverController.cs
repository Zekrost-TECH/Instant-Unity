using System.Collections;
using System.Globalization;
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
    [Tooltip("Texto bajo el título cuando no hay récord (\"TIME'S UP\").")]
    public TextMeshProUGUI subtitleText;

    [Header("Buttons")]
    public Button restartButton;
    public Button reviveButton;
    public Button exitToMenuButton;
    [Tooltip("Anuncio recompensado que da Cronos (GDD). Uno por pantalla de Game Over.")]
    public Button cronosAdButton;
    [Tooltip("Vuelve al menú con la tienda abierta.")]
    public Button shopButton;

    [Tooltip("Objeto opcional tipo 'Viendo anuncio...' que se muestra mientras dura el anuncio.")]
    public GameObject adPlayingLabel;

    [Header("Entrada animada")]
    public CanvasGroup headerGroup;
    public CanvasGroup statsGroup;
    public CanvasGroup cronosGroup;
    [Tooltip("Botones en el orden en que aparecen. Se animan por alfa y posición: su Animator ya controla la escala.")]
    public CanvasGroup[] buttonGroups;
    [Range(0f, 1f)] public float overlayAlpha = 0.86f;
    public Color recordColor = new Color(1f, 0.8f, 0.2f, 1f);

    private const float OverlayDuration = 0.25f;
    private const float HeaderStart = 0.05f;
    private const float StatsStart = 0.3f;
    private const float CountStart = 0.4f;
    private const float CountDuration = 0.7f;
    private const float CronosStart = 0.85f;
    private const float ButtonsStart = 0.95f;
    private const float ButtonStagger = 0.07f;

    private Image overlayImage;
    private Color overlayColor;
    private Vector2 statsBasePosition;
    private Vector2[] buttonBasePositions;
    private Color bestTimeColor;
    private Color bestKillsColor;
    private Coroutine introRoutine;
    private bool introPlaying;

    private void Awake()
    {
        if (gameOverPanel != null)
        {
            overlayImage = gameOverPanel.GetComponent<Image>();
            if (overlayImage != null) overlayColor = overlayImage.color;
        }

        if (statsGroup != null)
            statsBasePosition = ((RectTransform)statsGroup.transform).anchoredPosition;

        if (buttonGroups != null)
        {
            buttonBasePositions = new Vector2[buttonGroups.Length];
            for (int i = 0; i < buttonGroups.Length; i++)
            {
                if (buttonGroups[i] != null)
                    buttonBasePositions[i] = ((RectTransform)buttonGroups[i].transform).anchoredPosition;
            }
        }

        if (bestTimeText != null) bestTimeColor = bestTimeText.color;
        if (bestKillsText != null) bestKillsColor = bestKillsText.color;
    }

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

        if (cronosAdButton != null)
            cronosAdButton.onClick.AddListener(OnCronosAdClicked);

        if (shopButton != null)
            shopButton.onClick.AddListener(OnShopClicked);
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

        if (cronosAdButton != null)
            cronosAdButton.onClick.RemoveListener(OnCronosAdClicked);

        if (shopButton != null)
            shopButton.onClick.RemoveListener(OnShopClicked);

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnAdReadyChanged -= HandleAdReadyChanged;
    }

    private void Update()
    {
        // Latido suave del cartel de récord mientras el panel está abierto.
        if (newRecordText == null || !newRecordText.gameObject.activeInHierarchy) return;
        float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);
        newRecordText.rectTransform.localScale = Vector3.one * pulse;
    }

    private void HandleGameOver(float time, int kills, int cronos, bool newRecord)
    {
        // Cualquier flujo de anuncio previo debe quedar cerrado antes de un nuevo game over.
        adInProgress = false;
        cronosAdClaimed = false;
        runPayout = cronos;

        // Si la precarga falló antes (sin red, sin inventario), se vuelve a pedir ya:
        // la intro dura lo suficiente para que el anuncio llegue a tiempo.
        AdsManager.Ensure()?.EnsureLoaded();

        if (eliteKillsText != null)
            eliteKillsText.text = $"ELITES {(EnemyManager.Instance != null ? EnemyManager.Instance.EliteKillCount : 0)}";

        if (timeGainedText != null)
            timeGainedText.text = TimeManager.Instance != null ? "GAINED +" + Seconds(TimeManager.Instance.TimeGainedThisRun) : "GAINED +0.0s";

        if (damageTakenText != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerCombat combat = player != null ? player.GetComponent<PlayerCombat>() : null;
            damageTakenText.text = $"HITS {(combat != null ? combat.DamageTakenCount : 0)}";
        }

        UpdateCronosText();

        // Récord batido: el número correspondiente pasa a dorado (GDD).
        bool timeRecord = SaveManager.Instance != null && newRecord && Mathf.Approximately(SaveManager.Instance.BestTime, time);
        bool killsRecord = SaveManager.Instance != null && newRecord && SaveManager.Instance.BestKills == kills;

        if (bestTimeText != null && SaveManager.Instance != null)
        {
            bestTimeText.text = "BEST " + Seconds(SaveManager.Instance.BestTime);
            bestTimeText.color = timeRecord ? recordColor : bestTimeColor;
        }

        if (bestKillsText != null && SaveManager.Instance != null)
        {
            bestKillsText.text = $"BEST {SaveManager.Instance.BestKills}";
            bestKillsText.color = killsRecord ? recordColor : bestKillsColor;
        }

        if (newRecordText != null)
            newRecordText.gameObject.SetActive(newRecord);

        if (subtitleText != null)
            subtitleText.gameObject.SetActive(!newRecord);

        if (adPlayingLabel != null)
            adPlayingLabel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(PlayIntro(time, kills));
    }

    /// <summary>
    /// Entrada en cascada: fondo, título con golpe, tarjeta de stats que sube, contadores,
    /// Cronos y botones escalonados. Un toque la completa al instante. Los botones no
    /// aceptan pulsaciones hasta que termina, para no pulsar uno sin querer.
    /// </summary>
    private IEnumerator PlayIntro(float time, int kills)
    {
        introPlaying = true;
        SetButtonsInteractable(false);

        int buttonCount = buttonGroups != null ? buttonGroups.Length : 0;
        float end = ButtonsStart + Mathf.Max(0, buttonCount - 1) * ButtonStagger + 0.3f;
        end = Mathf.Max(end, CountStart + CountDuration);

        float elapsed = 0f;
        while (true)
        {
            bool skip = UnityEngine.InputSystem.Pointer.current != null
                && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame
                && elapsed > 0.1f;
            elapsed = skip ? end : elapsed + Time.unscaledDeltaTime;

            ApplyIntro(elapsed, time, kills);
            if (elapsed >= end) break;
            yield return null;
        }

        introPlaying = false;
        introRoutine = null;
        SetButtonsInteractable(true);
    }

    private void ApplyIntro(float elapsed, float time, int kills)
    {
        if (overlayImage != null)
        {
            Color c = overlayColor;
            c.a = overlayAlpha * Progress(elapsed, 0f, OverlayDuration);
            overlayImage.color = c;
        }

        if (headerGroup != null)
        {
            float p = Progress(elapsed, HeaderStart, 0.4f);
            headerGroup.alpha = Mathf.Clamp01(p * 2f);
            headerGroup.transform.localScale = Vector3.one * Mathf.LerpUnclamped(1.8f, 1f, EaseOutBack(p));
        }

        if (statsGroup != null)
        {
            float p = Progress(elapsed, StatsStart, 0.35f);
            statsGroup.alpha = p;
            ((RectTransform)statsGroup.transform).anchoredPosition = statsBasePosition + Vector2.down * (60f * (1f - EaseOutCubic(p)));
        }

        float count = EaseOutCubic(Progress(elapsed, CountStart, CountDuration));
        if (timeText != null) timeText.text = Seconds(time * count);
        if (killsText != null) killsText.text = Mathf.RoundToInt(kills * count).ToString();

        if (cronosGroup != null)
        {
            float p = Progress(elapsed, CronosStart, 0.35f);
            cronosGroup.alpha = Mathf.Clamp01(p * 2f);
            cronosGroup.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.4f, 1f, EaseOutBack(p));
        }

        if (buttonGroups == null) return;
        for (int i = 0; i < buttonGroups.Length; i++)
        {
            if (buttonGroups[i] == null) continue;

            float p = Progress(elapsed, ButtonsStart + i * ButtonStagger, 0.3f);
            buttonGroups[i].alpha = p;
            ((RectTransform)buttonGroups[i].transform).anchoredPosition = buttonBasePositions[i] + Vector2.down * (40f * (1f - EaseOutBack(p)));
        }
    }

    // Punto decimal siempre, como el reloj del HUD (F1 a secas usa la coma del sistema).
    private static string Seconds(float value)
    {
        return value.ToString("F1", CultureInfo.InvariantCulture) + "s";
    }

    private static float Progress(float elapsed, float start, float duration)
    {
        return Mathf.Clamp01((elapsed - start) / duration);
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private void UpdateCronosText()
    {
        if (cronosText != null)
            cronosText.text = $"+{runPayout} CRONOS";
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
        if (adInProgress) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.GameOver) return;

        // El AdsManager no está en ninguna escena: sin esto Instance es null y el
        // botón se limitaba a ocultar el panel dejando la partida muerta.
        AdsManager ads = AdsManager.Ensure();
        if (ads == null || !ads.IsAdReady) return;

        // El panel se queda visible durante el anuncio; sólo se cierra al cobrar la
        // recompensa. Los botones se bloquean para no encadenar pulsaciones.
        adInProgress = true;
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
        adInProgress = false;
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
        adInProgress = false;
        if (adPlayingLabel != null) adPlayingLabel.SetActive(false);
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        SetInteractable(restartButton, value);
        SetInteractable(reviveButton, value && reviveAdReady);
        SetInteractable(exitToMenuButton, value);
        SetInteractable(cronosAdButton, value && reviveAdReady && !cronosAdClaimed);
        SetInteractable(shopButton, value);
    }

    private void SetInteractable(Button button, bool interactable)
    {
        if (button == null) return;
        button.interactable = interactable;

        // El Animator de los botones no tiene estado visual "Disabled": sin esto, sin
        // anuncio cargado el botón de revivir parecía activo y no respondía.
        // Durante la intro el alfa lo controla la animación de entrada.
        if (introPlaying) return;
        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = interactable ? 1f : 0.4f;
    }

    private bool reviveAdReady;
    private bool adInProgress;
    private bool cronosAdClaimed;
    private int runPayout;
    private int cronosBeforeAd;

    private void OnCronosAdClicked()
    {
        if (adInProgress || cronosAdClaimed) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.GameOver) return;

        AdsManager ads = AdsManager.Ensure();
        if (ads == null || !ads.IsAdReady) return;

        adInProgress = true;
        cronosBeforeAd = SaveManager.Instance != null ? SaveManager.Instance.Cronos : 0;
        SetButtonsInteractable(false);
        if (adPlayingLabel != null) adPlayingLabel.SetActive(true);

        // AdsManager suma los Cronos (10-20) al SaveManager antes de llamar al callback.
        ads.ShowRewardedAd(
            onRewardGranted: HandleCronosGranted,
            grantCronos: true,
            onFailed: HandleReviveFailed);
    }

    private void HandleCronosGranted()
    {
        adInProgress = false;
        cronosAdClaimed = true;
        if (adPlayingLabel != null) adPlayingLabel.SetActive(false);

        int gained = SaveManager.Instance != null ? SaveManager.Instance.Cronos - cronosBeforeAd : 0;
        runPayout += Mathf.Max(0, gained);
        UpdateCronosText();

        SetButtonsInteractable(true);
    }

    private void OnShopClicked()
    {
        MainMenuUI.RequestShopOnLoad();
        SceneManager.LoadScene("0_MainMenu");
    }

    private void HandleAdReadyChanged(bool ready)
    {
        reviveAdReady = ready;
        if (gameOverPanel != null && gameOverPanel.activeSelf && !introPlaying && !adInProgress)
            SetButtonsInteractable(true);
    }

    private void OnExitToMenuClicked()
    {
        SceneManager.LoadScene("0_MainMenu");
    }
}
