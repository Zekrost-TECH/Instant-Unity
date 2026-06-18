using System;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    public const int REWARDED_AD_CRONOS_MIN = 10;
    public const int REWARDED_AD_CRONOS_MAX = 20;

    public bool IsAdReady { get; private set; } = false;

    private Action onRewardGrantedCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Stub: simular que el SDK de LevelPlay está inicializado y precargado
        IsAdReady = true;
    }

    public void ShowRewardedAd(Action onRewardGranted)
    {
        if (!IsAdReady)
        {
            Debug.LogWarning("[AdsManager] Anuncio recompensado no está listo.");
            return;
        }

        onRewardGrantedCallback = onRewardGranted;

        // Stub: en una integración real aquí se llamaría al SDK de LevelPlay/Unity Ads.
        // Simulamos que el anuncio se reproduce y otorga la recompensa.
        Debug.Log("[AdsManager] Mostrando anuncio recompensado (stub).");
        Invoke(nameof(GrantReward), 1f);
    }

    private void GrantReward()
    {
        int reward = UnityEngine.Random.Range(REWARDED_AD_CRONOS_MIN, REWARDED_AD_CRONOS_MAX + 1);
        SaveManager.Instance?.AddCronos(reward);
        Debug.Log($"[AdsManager] Recompensa otorgada: {reward} Cronos.");
        onRewardGrantedCallback?.Invoke();
    }
}
