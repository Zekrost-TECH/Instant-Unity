using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    public const int REWARDED_AD_CRONOS_MIN = 10;
    public const int REWARDED_AD_CRONOS_MAX = 20;

    // IDs de PRUEBA oficiales de Google. No están ligados a ninguna cuenta, así que no
    // generan tráfico inválido. Sustitúyelos por los tuyos antes de publicar.
    private const string TEST_REWARDED_ANDROID = "ca-app-pub-3940256099942544/5224354917";
    private const string TEST_REWARDED_IOS = "ca-app-pub-3940256099942544/1712485313";

    [Header("AdMob")]
    [Tooltip("Con esto activo se usan los IDs de prueba de Google. Desactívalo cuando tengas los tuyos.")]
    public bool useTestAds = true;
    [Tooltip("Ad Unit ID del recompensado en Android (ca-app-pub-XXXX/YYYY).")]
    public string androidRewardedAdUnitId = "";
    [Tooltip("Ad Unit ID del recompensado en iOS.")]
    public string iosRewardedAdUnitId = "";

    [Header("Editor")]
    [Tooltip("En el Editor simula el anuncio en vez de pedirlo a AdMob: iterar es mucho más rápido.")]
    public bool useStubInEditor = true;
    [Tooltip("Segundos que simula durar el anuncio del stub.")]
    public float adDurationStub = 1f;
    [Tooltip("Segundos a esperar la resolución del anuncio (close/fail) antes de fallar por seguridad.")]
    public float showTimeoutSeconds = 60f;

    /// <summary>
    /// Ciclo de vida de un anuncio recompensado. Sólo se muestra en Ready y nunca se
    /// lanza un Show mientras otro sigue en pantalla (Showing).
    /// </summary>
    public enum AdFlowState { Idle, Loading, Ready, Showing }

    public AdFlowState FlowState { get; private set; } = AdFlowState.Idle;
    public bool IsAdReady => FlowState == AdFlowState.Ready;
    public event Action<bool> OnAdReadyChanged;

    private Action onRewardGrantedCallback;
    private Action onFailedCallback;
    private bool grantCronosOnReward;
    private bool rewardEarnedThisAd;
    private bool resolvePending;
    private bool lastNotifiedReady;
    private float showWatchdogTimer = -1f;

    // ── Dispatcher al hilo main ─────────────────────────────────────────────
    // El SDK de AdMob invoca varios callbacks (Load, Show, cierre, fallo) desde un
    // hilo de trabajo. Tocar Unity desde ahí lanza "get_activeSelf can be called
    // only on the main thread" y aborta la cadena del revivir a mitad de camino,
    // dejando la partida pegada en el panel de Game Over. Todo callback del SDK
    // se reenvía al hilo main por aquí.
    private SynchronizationContext mainContext;
    private readonly Queue<Action> mainThreadQueue = new Queue<Action>();
    private readonly object queueLock = new object();

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
    private RewardedAd rewardedAd;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Solo el componente: los managers comparten el GameObject "Managers" de 1_Game,
            // y Destroy(gameObject) se llevaria por delante a todos los demas.
            Destroy(this);
            return;
        }
        Instance = this;
        mainContext = SynchronizationContext.Current;
    }

    private void OnDestroy()
    {
        CancelInvoke();
        onRewardGrantedCallback = null;
        onFailedCallback = null;
        resolvePending = false;
        lock (queueLock) mainThreadQueue.Clear();
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        DestroyRewardedAd();
#endif
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (UseStub)
        {
            NotifyAdReady(true);   // el stub siempre está listo
            return;
        }

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        MobileAdsEventExecutor.Initialize();
        MobileAds.Initialize(status =>
        {
            RunOnMainThread(() =>
            {
                Debug.Log("[AdsManager] AdMob inicializado.");
                LoadRewardedAd();
            });
        });
#endif
    }

    private void Update()
    {
        DrainMainThreadQueue();

        if (showWatchdogTimer <= 0f) return;
        showWatchdogTimer -= Time.unscaledDeltaTime;
        if (showWatchdogTimer > 0f) return;

        showWatchdogTimer = -1f;
        Debug.LogWarning("[AdsManager] Watchdog: el anuncio no se resolvió a tiempo; se aborta la solicitud.");
        Resolve(success: false);
    }

    /// <summary>
    /// Ejecuta <paramref name="action"/> en el hilo main. Lo normal es tener
    /// SynchronizationContext (jugador y editor en play mode); si faltara (arranques
    /// muy tempranos) se encola y Update lo drena en el mismo frame.
    /// </summary>
    private void RunOnMainThread(Action action)
    {
        if (action == null) return;

        if (mainContext != null)
        {
            mainContext.Post(_ => SafeExecute(action), null);
            return;
        }

        lock (queueLock) mainThreadQueue.Enqueue(action);
    }

    private void SafeExecute(Action action)
    {
        try { action(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    private void DrainMainThreadQueue()
    {
        lock (queueLock)
        {
            if (mainThreadQueue.Count == 0) return;

            while (mainThreadQueue.Count > 0)
            {
                Action action = mainThreadQueue.Dequeue();
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    /// <summary>
    /// Crea el AdsManager si no existe. No está puesto en ninguna escena, así que sin
    /// esto AdsManager.Instance es null y el botón de revivir no hacía nada.
    /// </summary>
    public static AdsManager Ensure()
    {
        if (Instance != null) return Instance;

        AdsManager existing = FindAnyObjectByType<AdsManager>();
        if (existing != null) return existing;

        GameObject go = new GameObject("AdsManager");
        AdsManager manager = go.AddComponent<AdsManager>();
        DontDestroyOnLoad(go);
        return manager;
    }

    private bool UseStub
    {
#if UNITY_EDITOR
        get { return useStubInEditor; }
#else
        get { return false; }
#endif
    }

    private string RewardedAdUnitId
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return useTestAds || string.IsNullOrEmpty(iosRewardedAdUnitId) ? TEST_REWARDED_IOS : iosRewardedAdUnitId;
#else
            return useTestAds || string.IsNullOrEmpty(androidRewardedAdUnitId) ? TEST_REWARDED_ANDROID : androidRewardedAdUnitId;
#endif
        }
    }

    // ── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Reproduce el anuncio recompensado. La recompensa la decide quien llama:
    /// <paramref name="onRewardGranted"/> sólo se invoca si el jugador vio el anuncio
    /// hasta el final (callback nativo de recompensa) y éste se cerró.
    /// Devuelve false si no se pudo mostrar (se invoca <paramref name="onFailed"/>).
    /// </summary>
    public bool ShowRewardedAd(Action onRewardGranted, bool grantCronos = false, Action onFailed = null)
    {
        if (FlowState == AdFlowState.Showing)
        {
            Debug.LogWarning("[AdsManager] Ya hay un anuncio mostrándose; solicitud rechazada.");
            onFailed?.Invoke();
            return false;
        }

        onRewardGrantedCallback = onRewardGranted;
        onFailedCallback = onFailed;
        grantCronosOnReward = grantCronos;
        rewardEarnedThisAd = false;
        resolvePending = true;
        showWatchdogTimer = -1f;

        if (UseStub)
        {
            Debug.Log("[AdsManager] Mostrando anuncio recompensado (stub de Editor).");
            FlowState = AdFlowState.Showing;
            CancelInvoke(nameof(GrantStubReward));
            Invoke(nameof(GrantStubReward), adDurationStub);
            return true;
        }

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            Debug.LogWarning("[AdsManager] El anuncio no está cargado todavía.");
            LoadRewardedAd();
            Resolve(success: false);
            return false;
        }

        FlowState = AdFlowState.Showing;
        showWatchdogTimer = showTimeoutSeconds;

        rewardedAd.Show(reward =>
        {
            // El callback del SDK puede llegar desde un hilo de trabajo: se marca en main thread.
            RunOnMainThread(() =>
            {
                // Sólo marca: la recompensa se entrega al cerrarse el anuncio, para no
                // reanudar la partida con el anuncio aún en pantalla.
                rewardEarnedThisAd = true;
                Debug.Log($"[AdsManager] Recompensa ganada: {reward.Amount} {reward.Type}");
            });
        });
        return true;
#else
        Resolve(success: false);
        return false;
#endif
    }

    private void GrantStubReward()
    {
        Resolve(success: true);
        // Igual que en el flujo real: recargar para poder volver a revivir en el Editor.
        RunOnMainThread(LoadRewardedAd);
    }

    // ── AdMob ────────────────────────────────────────────────────────────────

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
    private void LoadRewardedAd()
    {
        if (UseStub)
        {
            NotifyAdReady(true);
            return;
        }

        // No recargar mientras se está mostrando o ya cargando.
        if (FlowState == AdFlowState.Showing || FlowState == AdFlowState.Loading) return;

        DestroyRewardedAd();
        NotifyAdReady(false);
        FlowState = AdFlowState.Loading;

        RewardedAd.Load(RewardedAdUnitId, new AdRequest(), (ad, error) =>
        {
            RunOnMainThread(() => HandleLoadResult(ad, error));
        });
    }

    private void HandleLoadResult(RewardedAd ad, AdError error)
    {
        if (error != null || ad == null)
        {
            Debug.LogWarning("[AdsManager] Falló la carga del recompensado: " + (error != null ? error.GetMessage() : "sin instancia"));
            FlowState = AdFlowState.Idle;
            NotifyAdReady(false);
            return;
        }

        rewardedAd = ad;
        RegisterAdEvents(ad);
        NotifyAdReady(true);
    }

    private void RegisterAdEvents(RewardedAd ad)
    {
        // Todos los eventos se reenvían al hilo main: cerrar el anuncio no puede
        // fallar a mitad de camino por culpa de un hilo equivocado.
        ad.OnAdFullScreenContentClosed += () => RunOnMainThread(HandleFullScreenContentClosed);
        ad.OnAdFullScreenContentFailed += adError => RunOnMainThread(() => HandleFullScreenContentFailed(adError));
    }

    private void HandleFullScreenContentClosed()
    {
        // Validación de recompensa: sólo se revive si el SDK confirmó que el jugador
        // vio el anuncio hasta el final Y éste se cerró. Cerrar sin recompensa es fail.
        Resolve(success: rewardEarnedThisAd);

        // La precarga se difiere al hilo main fuera del evento nativo: destruir el
        // RewardedAd dentro de su propio callback de cierre puede romper el SDK.
        RunOnMainThread(LoadRewardedAd);
    }

    private void HandleFullScreenContentFailed(AdError adError)
    {
        Debug.LogWarning("[AdsManager] El anuncio no pudo mostrarse: " + adError.GetMessage());
        Resolve(success: false);
        RunOnMainThread(LoadRewardedAd);
    }

    private void DestroyRewardedAd()
    {
        if (rewardedAd == null) return;
        rewardedAd.Destroy();
        rewardedAd = null;
    }
#endif

    // ── Resolución ───────────────────────────────────────────────────────────

    /// <summary>
    /// Resuelve la solicitud en curso EXACTAMENTE una vez (recompensa o fallo, nunca
    /// ambos, nunca ninguno). Garantiza que la UI que espera el anuncio siempre reciba
    /// una respuesta y no quede bloqueada.
    /// </summary>
    private void Resolve(bool success)
    {
        if (!resolvePending) return;
        resolvePending = false;
        showWatchdogTimer = -1f;

        // El anuncio mostrado se consume: hasta que cargue el siguiente no hay más.
        FlowState = AdFlowState.Idle;
        NotifyAdReady(false);

        if (success) GrantReward();
        else FailAd();
    }

    private void GrantReward()
    {
        if (grantCronosOnReward)
        {
            int reward = UnityEngine.Random.Range(REWARDED_AD_CRONOS_MIN, REWARDED_AD_CRONOS_MAX + 1);
            SaveManager.Instance?.AddCronos(reward);
            Debug.Log($"[AdsManager] Recompensa otorgada: {reward} Cronos.");
        }

        Action callback = onRewardGrantedCallback;
        ClearCallbacks();
        callback?.Invoke();
    }

    private void FailAd()
    {
        Action callback = onFailedCallback;
        ClearCallbacks();
        callback?.Invoke();
    }

    private void ClearCallbacks()
    {
        onRewardGrantedCallback = null;
        onFailedCallback = null;
        rewardEarnedThisAd = false;
        grantCronosOnReward = false;
    }

    /// <summary>
    /// Transiciona el estado y emite OnAdReadyChanged sólo cuando el valor "listo"
    /// cruza de uno a otro, para que los suscriptores (p. ej. el botón de revivir)
    /// no se queden con copias desactualizadas. No machaca Loading ni Showing.
    /// </summary>
    private void NotifyAdReady(bool ready)
    {
        if (ready) FlowState = AdFlowState.Ready;
        else if (FlowState == AdFlowState.Ready) FlowState = AdFlowState.Idle;

        if (lastNotifiedReady == ready) return;
        lastNotifiedReady = ready;
        OnAdReadyChanged?.Invoke(ready);
    }
}
