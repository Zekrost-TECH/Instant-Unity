using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Pools")]
    public List<UpgradeData> commonUpgrades;
    public List<UpgradeData> rareUpgrades;

    private List<string> acquiredUpgrades = new List<string>();
    private int killsSinceLastUpgrade = 0;
    private const int KILLS_FOR_UPGRADE = 20;

    public event Action<List<UpgradeData>> OnUpgradeWindowOpened;

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
        }
    }

    private void OnDestroy()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled -= HandleEnemyKilled;
        }
    }

    private void HandleEnemyKilled(EnemyBase enemy, bool isElite)
    {
        if (isElite)
        {
            TriggerRareUpgrade();
        }
        else
        {
            killsSinceLastUpgrade++;
            if (killsSinceLastUpgrade >= KILLS_FOR_UPGRADE)
            {
                killsSinceLastUpgrade = 0;
                TriggerCommonUpgrade();
            }
        }
    }

    private void TriggerCommonUpgrade()
    {
        OpenUpgradeWindow(GetRandomUpgrades(3, false));
    }

    private void TriggerRareUpgrade()
    {
        OpenUpgradeWindow(GetRandomUpgrades(3, true));
    }

    private void OpenUpgradeWindow(List<UpgradeData> options)
    {
        if (options == null || options.Count == 0) return;

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetDrainMultiplier(0.2f);
        
        OnUpgradeWindowOpened?.Invoke(options);
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        acquiredUpgrades.Add(upgrade.id);
        UpgradeEffects.ApplyUpgrade(upgrade);

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetDrainMultiplier(1.0f);
    }

    private List<UpgradeData> GetRandomUpgrades(int count, bool rare)
    {
        List<UpgradeData> pool = rare ? new List<UpgradeData>(rareUpgrades) : new List<UpgradeData>(commonUpgrades);
        List<UpgradeData> selected = new List<UpgradeData>();

        if (pool.Count == 0) return selected;

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
