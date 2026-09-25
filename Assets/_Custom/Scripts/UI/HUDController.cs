using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI timeText;
    public Image timeBar;
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI runCronosText;
    public GameObject hudRoot;
    [Tooltip("Línea bajo la barra del reloj: aviso de élite y Reloj voraz activo.")]
    public TextMeshProUGUI statusText;
    [Tooltip("Borde rojo a pantalla completa que late con el reloj en peligro. El sprite se genera solo.")]
    public Image dangerVignette;

    [Header("Colors")]
    public Color calmColor = Color.white;
    public Color warningColor = new Color(1f, 0.67f, 0f); // #FFAA00
    public Color dangerColor = new Color(1f, 0.2f, 0.2f); // #FF3333
    public Color calmBarColor = new Color(0.25f, 0.85f, 1f);
    public Color gainFlashColor = new Color(0f, 1f, 0.53f); // #00FF88
    public Color lossFlashColor = new Color(1f, 0.2f, 0.2f);
    public Color voraciousColor = new Color(1f, 0.84f, 0f);
    public Color eliteAlertColor = new Color(1f, 0.67f, 0f);
    public Color overtimeColor = new Color(1f, 0.3f, 0.35f);

    [Header("Juice")]
    [Tooltip("Amplitud del latido del reloj con 5-15s.")]
    public float warningPulse = 0.05f;
    [Tooltip("Amplitud del latido del reloj con 5s o menos.")]
    public float dangerPulse = 0.14f;
    [Tooltip("Duración del destello verde/rojo al ganar o perder tiempo.")]
    public float flashDuration = 0.35f;
    [Tooltip("Escala extra del reloj en el instante del destello.")]
    public float flashPunch = 0.18f;
    [Tooltip("Escala extra del contador de kills al sumar una baja.")]
    public float killPunch = 0.25f;
    public float eliteAlertDuration = 2.5f;
    [Tooltip("El proyecto está en espacio de color lineal: valores bajos ya se ven intensos.")]
    [Range(0f, 1f)] public float vignetteMaxAlpha = 0.18f;

    [Header("Overtime · Jefe")]
    [Tooltip("Barra con la vida del jefe (la barrera).")]
    public GameObject bossBarRoot;
    public Image bossBarFill;
    public TextMeshProUGUI bossBarLabel;
    [Tooltip("Cartel central que aparece al derrotar al jefe.")]
    public RectTransform barrierBrokenRoot;
    public TextMeshProUGUI barrierBrokenTitle;
    public TextMeshProUGUI barrierBrokenSubtitle;
    public Color barrierColor = new Color(1f, 0.8f, 0.25f);
    [Tooltip("Intensidad de los bordes rojos que laten mientras hay un jefe vivo.")]
    [Range(0f, 1f)] public float overtimeVignetteAlpha = 0.16f;
    public float barrierBannerDuration = 2.5f;

    private const string KillsLabel = "KILLS {0}";
    private const string CronosLabel = "CRONOS {0}";
    private const string VoraciousLabel = "VORACIOUS x1.5  {0}s";
    private const string OvertimeLabel = "OVERTIME {0}";

    private int lastDisplayedTenths = int.MinValue;
    private int lastDisplayedRunCronos = int.MinValue;
    private int lastVoraciousSeconds = int.MinValue;
    private float lastBarFill = -1f;

    private TimeManager.TimeColorState colorState = TimeManager.TimeColorState.Calm;
    private float flashTimer;
    private Color flashColor;
    private float killPunchTimer;
    private float alertTimer;
    private string alertText;
    private Color alertColor;
    private float vignetteAlpha;
    private bool subscribedToSpawn;
    private float barrierTimer;
    private float barrierFlashTimer;
    private bool barrierBannerPending;
    private float shownBossFill = -1f;

    private const float BarrierFlashDuration = 0.8f;

    private void Awake()
    {
        if (dangerVignette != null)
        {
            if (dangerVignette.sprite == null) dangerVignette.sprite = CreateVignetteSprite();
            dangerVignette.raycastTarget = false;
            SetVignetteAlpha(0f);
        }

        if (statusText != null) statusText.gameObject.SetActive(false);
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
        if (barrierBrokenRoot != null) barrierBrokenRoot.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged += UpdateTimer;
            TimeManager.Instance.OnTimeColorChanged += UpdateTimeColor;
            TimeManager.Instance.OnTimeAdjusted += HandleTimeAdjusted;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnKillCountChanged += UpdateKillCount;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.OnEliteSpawned += HandleEliteSpawned;
            SpawnManager.Instance.OnEnemyDebut += HandleEnemyDebut;
            SpawnManager.Instance.OnOvertimeLevelChanged += HandleOvertimeLevelChanged;
            SpawnManager.Instance.OnBarrierBroken += HandleBarrierBroken;
            SpawnManager.Instance.OnBarrierShattered += HandleBarrierShattered;
            subscribedToSpawn = true;
        }

        UpdateTimeColor(TimeManager.TimeColorState.Calm);

        if (GameManager.Instance != null)
            HandleGameStateChanged(GameManager.Instance.CurrentState);

        if (TimeManager.Instance != null)
            UpdateTimer(TimeManager.Instance.CurrentTime);

        if (EnemyManager.Instance != null)
            UpdateKillCount(EnemyManager.Instance.KillCount);

        killPunchTimer = 0f;
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged -= UpdateTimer;
            TimeManager.Instance.OnTimeColorChanged -= UpdateTimeColor;
            TimeManager.Instance.OnTimeAdjusted -= HandleTimeAdjusted;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnKillCountChanged -= UpdateKillCount;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (subscribedToSpawn && SpawnManager.Instance != null)
        {
            SpawnManager.Instance.OnEliteSpawned -= HandleEliteSpawned;
            SpawnManager.Instance.OnEnemyDebut -= HandleEnemyDebut;
            SpawnManager.Instance.OnOvertimeLevelChanged -= HandleOvertimeLevelChanged;
            SpawnManager.Instance.OnBarrierBroken -= HandleBarrierBroken;
            SpawnManager.Instance.OnBarrierShattered -= HandleBarrierShattered;
        }
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (hudRoot != null)
        {
            hudRoot.SetActive(IsRunActive(state));
        }
    }

    // Upgrade y Paused (tooltips) siguen siendo partida en curso: el HUD no debe
    // desaparecer ni poner los Cronos a 0 mientras están abiertos.
    private static bool IsRunActive(GameManager.GameState state)
    {
        return state == GameManager.GameState.Playing
            || state == GameManager.GameState.Upgrade
            || state == GameManager.GameState.Paused;
    }

    // ── Animación ────────────────────────────────────────────────────────────
    // Tiempo real: el hit-stop cambia timeScale y el HUD no debe tartamudear con él.

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        AnimateClock(deltaTime, time);
        AnimateKills(deltaTime);
        UpdateStatusLine(deltaTime, time);
        UpdateVignette(deltaTime, time);
        UpdateBossBar(deltaTime, time);
        AnimateBarrierBanner(deltaTime);
    }

    private void AnimateClock(float deltaTime, float time)
    {
        if (timeText == null) return;

        // Latido: suave en alerta, fuerte y más rápido en peligro (GDD).
        float pulse = 0f;
        if (colorState == TimeManager.TimeColorState.Danger)
            pulse = dangerPulse * Mathf.Abs(Mathf.Sin(time * Mathf.PI * 2f));
        else if (colorState == TimeManager.TimeColorState.Warning)
            pulse = warningPulse * Mathf.Abs(Mathf.Sin(time * Mathf.PI));

        Color baseColor = GetStateColor(colorState, calmColor);
        float flash = 0f;
        if (flashTimer > 0f)
        {
            flashTimer = Mathf.Max(0f, flashTimer - deltaTime);
            flash = flashTimer / flashDuration;
        }

        timeText.color = Color.Lerp(baseColor, flashColor, flash);
        timeText.rectTransform.localScale = Vector3.one * (1f + pulse + flashPunch * flash);

        if (timeBar != null)
        {
            bool voracious = UpgradeManager.Instance != null && UpgradeManager.Instance.VoraciousRemaining > 0f;
            Color barColor = voracious ? voraciousColor : GetStateColor(colorState, calmBarColor);
            timeBar.color = Color.Lerp(barColor, flashColor, flash);
        }
    }

    private void AnimateKills(float deltaTime)
    {
        if (killCountText == null || killPunchTimer <= 0f) return;

        killPunchTimer = Mathf.Max(0f, killPunchTimer - deltaTime);
        float t = killPunchTimer / 0.2f;
        killCountText.rectTransform.localScale = Vector3.one * (1f + killPunch * t);
    }

    private void UpdateStatusLine(float deltaTime, float time)
    {
        if (statusText == null) return;

        if (alertTimer > 0f)
        {
            alertTimer = Mathf.Max(0f, alertTimer - deltaTime);
            SetStatus(true);
            statusText.SetText(alertText);
            Color c = alertColor;
            c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(time * Mathf.PI * 3f));
            statusText.color = c;
            lastVoraciousSeconds = int.MinValue;
            return;
        }

        float voracious = UpgradeManager.Instance != null ? UpgradeManager.Instance.VoraciousRemaining : 0f;
        if (voracious > 0f)
        {
            SetStatus(true);
            int seconds = Mathf.CeilToInt(voracious);
            if (seconds != lastVoraciousSeconds)
            {
                lastVoraciousSeconds = seconds;
                statusText.SetText(VoraciousLabel, seconds);
            }
            statusText.color = voraciousColor;
            return;
        }

        lastVoraciousSeconds = int.MinValue;

        // En Overtime queda fijo y tenue: explica por qué el reloj baja más rápido.
        int overtime = SpawnManager.Instance != null ? SpawnManager.Instance.OvertimeLevel : 0;
        if (overtime > 0)
        {
            SetStatus(true);
            statusText.SetText(OvertimeLabel, overtime);
            Color dim = overtimeColor;
            dim.a = 0.7f;
            statusText.color = dim;
            return;
        }

        SetStatus(false);
    }

    private void SetStatus(bool visible)
    {
        if (statusText.gameObject.activeSelf != visible)
            statusText.gameObject.SetActive(visible);
    }

    private void UpdateVignette(float deltaTime, float time)
    {
        if (dangerVignette == null) return;

        bool runActive = GameManager.Instance != null && IsRunActive(GameManager.Instance.CurrentState);
        float target = 0f;
        if (colorState == TimeManager.TimeColorState.Danger && runActive)
            target = vignetteMaxAlpha * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(time * Mathf.PI * 2f)));

        // Jefe vivo: los bordes laten en rojo con un doble golpe de corazón.
        if (runActive && SpawnManager.Instance != null && SpawnManager.Instance.IsBossActive)
            target = Mathf.Max(target, overtimeVignetteAlpha * Heartbeat(time));

        Color color = dangerColor;
        if (barrierFlashTimer > 0f)
        {
            // Barrera rota: el rojo se vuelve dorado un instante y se apaga.
            barrierFlashTimer = Mathf.Max(0f, barrierFlashTimer - deltaTime);
            float flash = barrierFlashTimer / BarrierFlashDuration;
            color = Color.Lerp(dangerColor, barrierColor, flash);
            target = Mathf.Max(target, vignetteMaxAlpha * 1.4f * flash);
        }

        // El latido necesita subir de golpe: la rampa sólo suaviza la bajada.
        float alpha = target > vignetteAlpha ? target : Mathf.MoveTowards(vignetteAlpha, target, deltaTime * 2f);
        SetVignetteAlpha(alpha, color);
    }

    /// <summary>Doble golpe (lub-dub) a ~72 pulsaciones por minuto, de 0.35 a 1.</summary>
    private static float Heartbeat(float time)
    {
        float phase = Mathf.Repeat(time * 1.2f, 1f);
        float lub = Mathf.Exp(-(phase * phase) / 0.004f);
        float dub = 0.7f * Mathf.Exp(-((phase - 0.2f) * (phase - 0.2f)) / 0.004f);
        return 0.35f + 0.65f * Mathf.Clamp01(lub + dub);
    }

    private void UpdateBossBar(float deltaTime, float time)
    {
        if (bossBarRoot == null) return;

        EnemyBoss boss = SpawnManager.Instance != null ? SpawnManager.Instance.CurrentBoss : null;
        // Sólo en juego activo: con la ventana de mejora abierta chocaba con su título.
        bool visible = boss != null && IsPlaying();
        if (bossBarRoot.activeSelf != visible) bossBarRoot.SetActive(visible);
        if (!visible)
        {
            shownBossFill = -1f;
            return;
        }

        // La barra baja con suavidad hacia la vida real y parpadea al quedar poca.
        float fill = boss.HealthFraction;
        shownBossFill = shownBossFill < 0f ? fill : Mathf.MoveTowards(shownBossFill, fill, deltaTime * 1.5f);
        if (bossBarFill != null)
        {
            bossBarFill.fillAmount = shownBossFill;
            float blink = boss.IsEnraged ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(time * Mathf.PI * 4f)) : 1f;
            bossBarFill.color = Color.Lerp(Color.white, dangerColor, blink);
        }

        if (bossBarLabel != null)
        {
            // Barreras en cola detrás de esta.
            int queued = SpawnManager.Instance.PendingBossCount;
            bossBarLabel.SetText(queued > 0 ? "BARRIER +{0}" : "BARRIER", queued);
        }
    }

    private void HandleBarrierBroken(Vector3 position, float newMaxTime)
    {
        if (barrierBrokenSubtitle != null) barrierBrokenSubtitle.SetText("MAX TIME {0}s", newMaxTime);
    }

    private void HandleBarrierShattered()
    {
        // Entra cuando estalla el cristal. Si hay una ventana de mejora abierta, que lo
        // taparía, se guarda y se muestra al volver al juego.
        barrierBannerPending = true;
    }

    private static bool IsPlaying()
    {
        return GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing;
    }

    /// <summary>Entra de golpe (escala con rebote), se mantiene y se desvanece subiendo.</summary>
    private void AnimateBarrierBanner(float deltaTime)
    {
        if (barrierBannerPending && IsPlaying())
        {
            barrierBannerPending = false;
            barrierTimer = barrierBannerDuration;
            barrierFlashTimer = BarrierFlashDuration;
            if (barrierBrokenRoot != null) barrierBrokenRoot.gameObject.SetActive(true);
        }

        if (barrierBrokenRoot == null || barrierTimer <= 0f) return;

        // Una ventana de mejora abierta lo taparía: se congela oculto y sigue al volver.
        bool playing = IsPlaying();
        if (barrierBrokenRoot.gameObject.activeSelf != playing) barrierBrokenRoot.gameObject.SetActive(playing);
        if (!playing) return;

        barrierTimer = Mathf.Max(0f, barrierTimer - deltaTime);
        float elapsed = barrierBannerDuration - barrierTimer;

        float enter = Mathf.Clamp01(elapsed / 0.3f);
        float back = 1f + 2.70158f * Mathf.Pow(enter - 1f, 3f) + 1.70158f * Mathf.Pow(enter - 1f, 2f);
        barrierBrokenRoot.localScale = Vector3.one * Mathf.LerpUnclamped(2f, 1f, back);

        float fadeOut = Mathf.Clamp01(barrierTimer / 0.5f);
        float alpha = Mathf.Min(Mathf.Clamp01(elapsed / 0.12f), fadeOut);
        barrierBrokenRoot.anchoredPosition = new Vector2(0f, 170f + (1f - fadeOut) * 40f); // por encima del jugador
        if (barrierBrokenTitle != null) barrierBrokenTitle.alpha = alpha;
        if (barrierBrokenSubtitle != null) barrierBrokenSubtitle.alpha = alpha;

        if (barrierTimer <= 0f) barrierBrokenRoot.gameObject.SetActive(false);
    }

    private void SetVignetteAlpha(float alpha)
    {
        SetVignetteAlpha(alpha, dangerColor);
    }

    private void SetVignetteAlpha(float alpha, Color color)
    {
        vignetteAlpha = alpha;
        Color c = color;
        c.a = alpha;
        dangerVignette.color = c;
        dangerVignette.enabled = alpha > 0.001f;
    }

    // ── Eventos ──────────────────────────────────────────────────────────────

    private void UpdateTimer(float time)
    {
        // OnTimeChanged llega cada frame: sólo tocamos los widgets cuando su valor visible cambia.
        int tenths = Mathf.CeilToInt(time * 10f);
        if (timeText != null && tenths != lastDisplayedTenths)
        {
            lastDisplayedTenths = tenths;
            int whole = tenths / 10;
            int frac = tenths % 10;
            timeText.SetText(NumberStrings.Get(whole) + "." + NumberStrings.Get(frac));
        }

        if (timeBar != null)
        {
            float fill = Mathf.Clamp01(time / (TimeManager.Instance != null ? TimeManager.Instance.MaxTime : TimeManager.TIME_MAX));
            if (!Mathf.Approximately(fill, lastBarFill))
            {
                lastBarFill = fill;
                timeBar.fillAmount = fill;
            }
        }

        UpdateRunCronos();
    }

    private void HandleTimeAdjusted(float delta)
    {
        // Las ganancias diminutas (kills seguidas) no deben dejar el reloj siempre verde:
        // el destello se reinicia, no se acumula.
        flashColor = delta > 0f ? gainFlashColor : lossFlashColor;
        flashTimer = flashDuration;
    }

    private void UpdateTimeColor(TimeManager.TimeColorState state)
    {
        colorState = state;
        if (timeText != null && flashTimer <= 0f)
            timeText.color = GetStateColor(state, calmColor);
    }

    private Color GetStateColor(TimeManager.TimeColorState state, Color calm)
    {
        switch (state)
        {
            case TimeManager.TimeColorState.Warning: return warningColor;
            case TimeManager.TimeColorState.Danger: return dangerColor;
            default: return calm;
        }
    }

    private void HandleEliteSpawned()
    {
        ShowAlert("ELITE INCOMING", eliteAlertColor);
    }

    private void HandleOvertimeLevelChanged(int level)
    {
        if (level > 0) ShowAlert(level == 1 ? "OVERTIME!" : "OVERTIME " + level, overtimeColor);
    }

    private void HandleEnemyDebut(string enemyName, Color color)
    {
        ShowAlert("NEW: " + enemyName.ToUpperInvariant(), color);
    }

    /// <summary>Aviso parpadeante bajo el reloj; tiene prioridad sobre el de Reloj voraz.</summary>
    private void ShowAlert(string text, Color color)
    {
        alertText = text;
        alertColor = color;
        alertTimer = eliteAlertDuration;
    }

    private void UpdateKillCount(int kills)
    {
        if (killCountText != null)
            killCountText.SetText(KillsLabel, kills);

        if (kills > 0) killPunchTimer = 0.2f;
    }

    private void UpdateRunCronos()
    {
        if (runCronosText == null) return;

        int runCronos = 0;
        if (GameManager.Instance != null && IsRunActive(GameManager.Instance.CurrentState))
        {
            int kills = EnemyManager.Instance != null ? EnemyManager.Instance.KillCount : 0;
            float time = SpawnManager.Instance != null ? SpawnManager.Instance.GameTime : 0f;
            runCronos = GameManager.CalculateRunCronos(kills, time);
        }

        if (runCronos == lastDisplayedRunCronos) return;

        lastDisplayedRunCronos = runCronos;
        runCronosText.SetText(CronosLabel, runCronos);
    }

    /// <summary>
    /// Degradado de bordes (transparente en el centro) para la viñeta de peligro.
    /// Superelipse: se estira a pantalla completa sin marcar las esquinas.
    /// </summary>
    private static Sprite CreateVignetteSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.name = "HUDDangerVignette";

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                float v = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                float distance = Mathf.Pow(Mathf.Pow(u, 4f) + Mathf.Pow(v, 4f), 0.25f);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.88f, 1.02f, distance));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
