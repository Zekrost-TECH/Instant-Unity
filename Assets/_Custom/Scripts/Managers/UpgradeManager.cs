using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public const float UPGRADE_WINDOW_DURATION = 8f;
    public const float UPGRADE_DRAIN_MULTIPLIER = 0.2f;

    [Header("Pools")]
    public List<UpgradeData> commonUpgrades;
    public List<UpgradeData> rareUpgrades;

    private List<string> acquiredUpgrades = new List<string>();
    private int totalKills = 0;
    private bool isWindowOpen = false;
    private Coroutine timeoutCoroutine;

    public event Action<List<UpgradeData>> OnUpgradeWindowOpened;
    public event Action OnUpgradeWindowClosed;
    public event Action<float> OnUpgradeTimerChanged; // 0..1 progreso

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
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled += HandleEnemyKilled;
            EnemyManager.Instance.OnKillsThresholdReached += HandleKillsThreshold;
        }
    }

    private void OnDestroy()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled -= HandleEnemyKilled;
            EnemyManager.Instance.OnKillsThresholdReached -= HandleKillsThreshold;
        }
    }

    private void HandleEnemyKilled(EnemyBase enemy, bool isElite)
    {
        totalKills++;

        if (isElite && !isWindowOpen)
        {
            TriggerRareUpgrade();
        }
    }

    private void HandleKillsThreshold()
    {
        if (!isWindowOpen)
        {
            TriggerCommonUpgrade();
        }
    }

    public void TriggerCommonUpgrade()
    {
        OpenUpgradeWindow(GetRandomUpgrades(3, false));
    }

    public void TriggerRareUpgrade()
    {
        OpenUpgradeWindow(GetRandomUpgrades(3, true));
    }

    private void OpenUpgradeWindow(List<UpgradeData> options)
    {
        if (options == null || options.Count == 0) return;
        if (isWindowOpen) return;

        isWindowOpen = true;
        GameManager.Instance?.ChangeState(GameManager.GameState.Upgrade);

        // Pausa parcial: el reloj drena al 20% de velocidad
        TimeManager.Instance?.SetDrainMultiplier(UPGRADE_DRAIN_MULTIPLIER);
        AudioManager.Instance?.FadeMusicTo(0.3f, 0.3f);

        OnUpgradeWindowOpened?.Invoke(options);

        if (timeoutCoroutine != null)
            StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(UpgradeTimeoutCoroutine());
    }

    private IEnumerator UpgradeTimeoutCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < UPGRADE_WINDOW_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            OnUpgradeTimerChanged?.Invoke(1f - (elapsed / UPGRADE_WINDOW_DURATION));
            yield return null;
        }

        // Tiempo agotado sin elegir
        CloseUpgradeWindow();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance?.upgradeMissedSFX, 0.8f);
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        if (!isWindowOpen) return;

        acquiredUpgrades.Add(upgrade.id);
        UpgradeEffects.ApplyUpgrade(upgrade);
        CloseUpgradeWindow();
    }

    public void ResetUpgrades()
    {
        if (isWindowOpen)
        {
            CloseUpgradeWindow();
        }
        acquiredUpgrades.Clear();
        totalKills = 0;
    }

    private void CloseUpgradeWindow()
    {
        if (!isWindowOpen) return;

        isWindowOpen = false;

        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }

        TimeManager.Instance?.SetDrainMultiplier(1f);
        AudioManager.Instance?.FadeMusicTo(1f, 0.3f);

        GameManager.Instance?.ResumeGame();
        OnUpgradeWindowClosed?.Invoke();
    }

    private List<UpgradeData> GetRandomUpgrades(int count, bool rare)
    {
        List<UpgradeData> selected = new List<UpgradeData>();

        if (rare)
        {
            selected = GetRandomUpgradesFromPool(count, rareUpgrades);
            // Si faltan raras, rellenar con comunes
            if (selected.Count < count)
                selected.AddRange(GetRandomUpgradesFromPool(count - selected.Count, commonUpgrades));
        }
        else
        {
            // Probabilidad de rara empieza en 5% y sube 2% por cada 10 kills totales
            float rareChance = 0.05f + ((totalKills / 10) * 0.02f);
            rareChance = Mathf.Clamp(rareChance, 0.05f, 0.60f);

            List<UpgradeData> tempCommonPool = new List<UpgradeData>(commonUpgrades);
            List<UpgradeData> tempRarePool = new List<UpgradeData>(rareUpgrades);

            for (int i = 0; i < count; i++)
            {
                bool rollRare = UnityEngine.Random.value <= rareChance;

                if (rollRare && tempRarePool.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, tempRarePool.Count);
                    selected.Add(tempRarePool[idx]);
                    tempRarePool.RemoveAt(idx);
                }
                else if (tempCommonPool.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, tempCommonPool.Count);
                    selected.Add(tempCommonPool[idx]);
                    tempCommonPool.RemoveAt(idx);
                }
            }
        }

        return selected;
    }

    private List<UpgradeData> GetRandomUpgradesFromPool(int count, List<UpgradeData> sourcePool)
    {
        List<UpgradeData> pool = new List<UpgradeData>(sourcePool);
        List<UpgradeData> selected = new List<UpgradeData>();

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;
            int idx = UnityEngine.Random.Range(0, pool.Count);
            selected.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return selected;
    }
}
