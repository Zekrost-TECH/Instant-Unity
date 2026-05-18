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
    private int upgradesAcquired = 0;
    private int totalKills = 0;

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
        totalKills++;

        if (isElite)
        {
            // Matar un élite fuerza una selección garantizada con cartas raras (o mayormente raras)
            TriggerEliteUpgrade();
        }
        else
        {
            killsSinceLastUpgrade++;
            
            // Fórmula: empieza en 10 bajas para la primera mejora, y escala en +5 por cada mejora adquirida.
            // 1ª -> 10 bajas, 2ª -> 15 bajas, 3ª -> 20 bajas, etc.
            int requiredKills = 10 + (upgradesAcquired * 5);

            if (killsSinceLastUpgrade >= requiredKills)
            {
                killsSinceLastUpgrade = 0;
                TriggerDynamicUpgrade();
            }
        }
    }

    private void TriggerDynamicUpgrade()
    {
        OpenUpgradeWindow(GetDynamicUpgrades(3));
    }

    private void TriggerEliteUpgrade()
    {
        // Forzamos recompensas raras si mata un élite
        OpenUpgradeWindow(GetRandomUpgradesFromPool(3, rareUpgrades));
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
        upgradesAcquired++;
        UpgradeEffects.ApplyUpgrade(upgrade);

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetDrainMultiplier(1.0f);
    }

    private List<UpgradeData> GetDynamicUpgrades(int count)
    {
        List<UpgradeData> selected = new List<UpgradeData>();
        
        // Probabilidad de rara empieza en 5% y sube 2% por cada 10 kills totales
        // Por ejemplo, a las 100 kills, la probabilidad sube a 25%
        float rareChance = 0.05f + ((totalKills / 10) * 0.02f);
        rareChance = Mathf.Clamp(rareChance, 0.05f, 0.60f); // Tope de 60% chance de rara en late game para balance

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
