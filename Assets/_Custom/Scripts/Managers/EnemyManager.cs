using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    public List<EnemyBase> ActiveEnemies { get; private set; } = new List<EnemyBase>();
    public int KillCount { get; private set; } = 0;
    public int EliteKillCount { get; private set; } = 0;
    public int KillsSinceLastUpgrade { get; private set; } = 0;

    private readonly HashSet<EnemyBase> registered = new HashSet<EnemyBase>();
    private bool wasPlaying = false;

    public event Action<EnemyBase, bool> OnEnemyKilled;
    public event Action<int> OnKillCountChanged;
    public event Action OnKillsThresholdReached;

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
        if (Instance == this) Instance = null;
    }

    // ── Tick centralizado ────────────────────────────────────────────────────
    // Un solo FixedUpdate/Update para todos los enemigos en vez de uno por instancia.
    // Con 60+ enemigos, el despacho managed de Unity domina el coste del frame.

    private void FixedUpdate()
    {
        bool playing = GameManager.Instance == null ||
                       GameManager.Instance.CurrentState == GameManager.GameState.Playing;

        if (!playing)
        {
            // Sólo en el flanco: frenar una vez al salir de Playing, no cada paso.
            if (wasPlaying)
            {
                wasPlaying = false;
                for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
                {
                    if (ActiveEnemies[i] != null) ActiveEnemies[i].Halt();
                }
            }
            return;
        }

        wasPlaying = true;

        float deltaTime = Time.fixedDeltaTime;
        for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = ActiveEnemies[i];
            if (enemy != null) enemy.Tick(deltaTime);
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = ActiveEnemies[i];
            if (enemy != null) enemy.TickVisuals(deltaTime);
        }
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null) return;
        if (registered.Add(enemy))
        {
            ActiveEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemy == null) return;
        if (!registered.Remove(enemy)) return;

        // Swap-remove: O(1) en vez del O(n) de List.Remove
        int index = ActiveEnemies.IndexOf(enemy);
        if (index < 0) return;

        int last = ActiveEnemies.Count - 1;
        ActiveEnemies[index] = ActiveEnemies[last];
        ActiveEnemies.RemoveAt(last);
    }

    public void NotifyEnemyDeath(EnemyBase enemy, bool isElite = false)
    {
        KillCount++;
        if (isElite) EliteKillCount++;
        KillsSinceLastUpgrade++;
        OnKillCountChanged?.Invoke(KillCount);
        OnEnemyKilled?.Invoke(enemy, isElite);

        if (KillsSinceLastUpgrade >= 20)
        {
            KillsSinceLastUpgrade = 0;
            OnKillsThresholdReached?.Invoke();
        }
        
        UnregisterEnemy(enemy);
    }

    public void ResetKillCount()
    {
        KillCount = 0;
        EliteKillCount = 0;
        KillsSinceLastUpgrade = 0;
        OnKillCountChanged?.Invoke(KillCount);
    }

    public void ResetKillsSinceLastUpgrade()
    {
        KillsSinceLastUpgrade = 0;
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
