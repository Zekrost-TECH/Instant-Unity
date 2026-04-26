using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    public List<EnemyBase> ActiveEnemies { get; private set; } = new List<EnemyBase>();
    public int KillCount { get; private set; } = 0;

    public event Action<EnemyBase, bool> OnEnemyKilled;
    public event Action<int> OnKillCountChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (!ActiveEnemies.Contains(enemy))
        {
            ActiveEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (ActiveEnemies.Contains(enemy))
        {
            ActiveEnemies.Remove(enemy);
        }
    }

    public void NotifyEnemyDeath(EnemyBase enemy, bool isElite = false)
    {
        KillCount++;
        OnKillCountChanged?.Invoke(KillCount);
        OnEnemyKilled?.Invoke(enemy, isElite);
        
        UnregisterEnemy(enemy);
    }

    public EnemyBase GetNearestEnemy(Vector3 position, float range)
    {
        EnemyBase nearest = null;
        float minDistanceSqr = range * range;

        foreach (var enemy in ActiveEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            float distSqr = (enemy.transform.position - position).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
