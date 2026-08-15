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
    public int UpgradeWindowsOpened { get; private set; } = 0;

    private const int UPGRADE_BASE_THRESHOLD = 10;
    private const int UPGRADE_THRESHOLD_GROWTH = 4;
    private const int UPGRADE_THRESHOLD_CAP = 38;

    private readonly HashSet<EnemyBase> registered = new HashSet<EnemyBase>();
    private readonly List<EnemyBase> nearestBuffer = new List<EnemyBase>(8);
    private readonly List<EnemyBase> killBuffer = new List<EnemyBase>(64);
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

        if (KillsSinceLastUpgrade >= GetUpgradeThreshold())
        {
            KillsSinceLastUpgrade = 0;
            UpgradeWindowsOpened++;
            OnKillsThresholdReached?.Invoke();
        }
        
        UnregisterEnemy(enemy);
    }

    public void ResetKillCount()
    {
        KillCount = 0;
        EliteKillCount = 0;
        KillsSinceLastUpgrade = 0;
        UpgradeWindowsOpened = 0;
        OnKillCountChanged?.Invoke(KillCount);
    }

    /// <summary>
    /// Cuanto más sobrevives, más kills cuesta abrir la siguiente ventana de mejora:
    /// al inicio salen rápido (10 kills) y van espaciándose hasta tope (38 kills).
    /// </summary>
    private int GetUpgradeThreshold()
    {
        return Math.Min(UPGRADE_BASE_THRESHOLD + UpgradeWindowsOpened * UPGRADE_THRESHOLD_GROWTH, UPGRADE_THRESHOLD_CAP);
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

    /// <summary>
    /// Devuelve hasta <paramref name="count"/> enemigos dentro del rango, ordenados por distancia
    /// (para el triple disparo del consumible).
    /// </summary>
    public List<EnemyBase> GetNearestEnemies(Vector3 position, float range, int count)
    {
        nearestBuffer.Clear();
        if (count <= 0) return nearestBuffer;

        float rangeSqr = range * range;
        for (int i = 0; i < ActiveEnemies.Count; i++)
        {
            EnemyBase enemy = ActiveEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            float distSqr = (enemy.transform.position - position).sqrMagnitude;
            if (distSqr > rangeSqr) continue;

            // Inserta ordenado por distancia (inserción simple: count es 2-3)
            int insertIndex = nearestBuffer.Count;
            for (int j = 0; j < nearestBuffer.Count; j++)
            {
                float bufferedSqr = (nearestBuffer[j].transform.position - position).sqrMagnitude;
                if (distSqr < bufferedSqr)
                {
                    insertIndex = j;
                    break;
                }
            }

            if (nearestBuffer.Count < count)
            {
                nearestBuffer.Insert(insertIndex, enemy);
            }
            else if (insertIndex < count)
            {
                nearestBuffer.RemoveAt(count - 1);
                nearestBuffer.Insert(insertIndex, enemy);
            }
        }

        return nearestBuffer;
    }

    /// <summary>
    /// Mata a todos los enemigos vivos (consumible de limpieza). Cuenta bajas
    /// (progreso a mejoras + cronos) pero no otorga tiempo: la presión del reloj se mantiene.
    /// </summary>
    public void KillAllEnemies()
    {
        // Copia para iterar de forma segura mientras los OnDisable modifican la lista original
        killBuffer.Clear();
        killBuffer.AddRange(ActiveEnemies);

        for (int i = 0; i < killBuffer.Count; i++)
        {
            EnemyBase enemy = killBuffer[i];
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.KillByConsumable();
            }
        }
        killBuffer.Clear();
    }

    /// <summary>
    /// Recicla (sin recompensa, baja ni tiempo) los enemigos dentro del radio. Lo usa el
    /// revivir por anuncio para que el jugador no reaparezca dentro del enjambre que lo
    /// mató y caiga muerto a los dos segundos.
    /// </summary>
    public void RecycleEnemiesAround(Vector3 center, float radius)
    {
        float radiusSqr = radius * radius;

        killBuffer.Clear();
        for (int i = 0; i < ActiveEnemies.Count; i++)
        {
            EnemyBase enemy = ActiveEnemies[i];
            if (enemy == null) continue;

            if (((Vector2)enemy.transform.position - (Vector2)center).sqrMagnitude <= radiusSqr)
                killBuffer.Add(enemy);
        }

        for (int i = 0; i < killBuffer.Count; i++)
        {
            if (killBuffer[i] != null) killBuffer[i].Recycle();
        }
        killBuffer.Clear();
    }
}
