using System;
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

    public bool IsAdReady { get; private set; } = false;
    public event Action<bool> OnAdReadyChanged;

    private Action onRewardGrantedCallback;
    private Action onFailedCallback;
    private bool grantCronosOnReward;
    private bool rewardEarnedThisAd;

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
    }

    private void OnDestroy()
    {
        CancelInvoke();
        onRewardGrantedCallback = null;
        onFailedCallback = null;
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        DestroyRewardedAd();
#endif
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (UseStub)
        {
            SetAdReady(true);   // el stub siempre está listo
            return;
        }

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        MobileAdsEventExecutor.Initialize();
        MobileAds.Initialize(status =>
        {
            Debug.Log("[AdsManager] AdMob inicializado.");
            LoadRewardedAd();
        });
#endif
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
    /// <paramref name="onRewardGranted"/> sólo se invoca si el usuario lo completa.
    /// </summary>
    /// <param name="grantCronos">
    /// Si es true además regala Cronos. El revivir NO lo usa: su recompensa es la propia
    /// partida, y pagar Cronos encima duplicaría el premio.
    /// </param>
    public void ShowRewardedAd(Action onRewardGranted, bool grantCronos = false, Action onFailed = null)
    {
        onRewardGrantedCallback = onRewardGranted;
        onFailedCallback = onFailed;
        grantCronosOnReward = grantCronos;
        rewardEarnedThisAd = false;

        if (UseStub)
        {
            Debug.Log("[AdsManager] Mostrando anuncio recompensado (stub de Editor).");
            CancelInvoke(nameof(GrantReward));
            Invoke(nameof(GrantReward), adDurationStub);
            return;
        }

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            Debug.LogWarning("[AdsManager] El anuncio no está cargado todavía.");
            LoadRewardedAd();
            FailAd();
            return;
        }

        rewardedAd.Show(reward =>
        {
            // Sólo marca: la recompensa se entrega al cerrarse el anuncio, para no
            // reanudar la partida con el anuncio aún en pantalla.
            rewardEarnedThisAd = true;
            Debug.Log($"[AdsManager] Recompensa ganada: {reward.Amount} {reward.Type}");
        });
#else
        FailAd();
#endif
    }

    // ── AdMob ────────────────────────────────────────────────────────────────

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
    private void LoadRewardedAd()
    {
        DestroyRewardedAd();
        SetAdReady(false);

        RewardedAd.Load(RewardedAdUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdsManager] Falló la carga del recompensado: " + (error != null ? error.GetMessage() : "sin instancia"));
                return;
            }

            rewardedAd = ad;
            SetAdReady(true);
            RegisterAdEvents(ad);
        });
    }

    private void RegisterAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            if (rewardEarnedThisAd) GrantReward();
            else FailAd();

            LoadRewardedAd();   // precargar el siguiente
        };

        ad.OnAdFullScreenContentFailed += adError =>
        {
            Debug.LogWarning("[AdsManager] El anuncio no pudo mostrarse: " + adError.GetMessage());
            FailAd();
            LoadRewardedAd();
        };
    }

    private void DestroyRewardedAd()
    {
        if (rewardedAd == null) return;
        rewardedAd.Destroy();
        rewardedAd = null;
    }
#endif

    // ── Resolución ───────────────────────────────────────────────────────────

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
    }

    private void SetAdReady(bool ready)
    {
        if (IsAdReady == ready) return;

        IsAdReady = ready;
        OnAdReadyChanged?.Invoke(ready);
    }
}
