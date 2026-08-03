using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public const float UPGRADE_DRAIN_MULTIPLIER = 0.2f;
    public const float UPGRADE_WINDOW_DURATION = 8f;

    [Header("Pools")]
    public List<UpgradeData> commonUpgrades;
    public List<UpgradeData> rareUpgrades;

    private List<string> acquiredUpgrades = new List<string>();
    private int totalKills = 0;
    private bool isWindowOpen = false;
    private float upgradeTimer;
    private bool pendingCommonUpgrade;
    private bool pendingRareUpgrade;

    private readonly List<UpgradeData> selectedBuffer = new List<UpgradeData>(4);
    private readonly List<UpgradeData> commonPoolBuffer = new List<UpgradeData>(16);
    private readonly List<UpgradeData> rarePoolBuffer = new List<UpgradeData>(16);
    private readonly List<UpgradeData> currentOptions = new List<UpgradeData>(4);

    public event Action<List<UpgradeData>> OnUpgradeWindowOpened;
    public event Action OnUpgradeWindowClosed;
    public event Action<float> OnUpgradeTimerChanged;

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

    private void Start()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled += HandleEnemyKilled;
            EnemyManager.Instance.OnKillsThresholdReached += HandleKillsThreshold;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        // Con el drenaje parcial activo el reloj puede llegar a 0 con la ventana abierta:
        // hay que cerrarla sin devolver el juego a Playing.
        if (state == GameManager.GameState.GameOver && isWindowOpen)
        {
            CloseUpgradeWindow(resumeGame: false);
        }
    }

    private void OnDestroy()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled -= HandleEnemyKilled;
            EnemyManager.Instance.OnKillsThresholdReached -= HandleKillsThreshold;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (Instance == this) Instance = null;
    }

    private void HandleEnemyKilled(EnemyBase enemy, bool isElite)
    {
        totalKills++;

        if (!isElite) return;

        if (isWindowOpen)
        {
            pendingRareUpgrade = true;
            return;
        }

        TriggerRareUpgrade();
    }

    private void HandleKillsThreshold()
    {
        if (isWindowOpen)
        {
            pendingCommonUpgrade = true;
            return;
        }

        TriggerCommonUpgrade();
    }

    private void Update()
    {
        if (!isWindowOpen) return;

        upgradeTimer -= Time.unscaledDeltaTime;
        OnUpgradeTimerChanged?.Invoke(Mathf.Clamp01(upgradeTimer / UPGRADE_WINDOW_DURATION));

        if (upgradeTimer > 0f) return;

        if (currentOptions.Count > 0)
            ApplyUpgrade(currentOptions[0]);
        else
            CloseUpgradeWindow();
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
        upgradeTimer = UPGRADE_WINDOW_DURATION;
        currentOptions.Clear();
        currentOptions.AddRange(options);
        GameManager.Instance?.ChangeState(GameManager.GameState.Upgrade);

        // Pausa parcial: el reloj drena al 20% de velocidad
        TimeManager.Instance?.SetDrainMultiplier(UPGRADE_DRAIN_MULTIPLIER);
        AudioManager.Instance?.FadeMusicTo(0.3f, 0.3f);

        OnUpgradeTimerChanged?.Invoke(1f);
        OnUpgradeWindowOpened?.Invoke(options);
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        if (!isWindowOpen || upgrade == null) return;

        acquiredUpgrades.Add(upgrade.id);
        UpgradeEffects.ApplyUpgrade(upgrade);
        CloseUpgradeWindow();
    }

    public void ResetUpgrades()
    {
        if (isWindowOpen)
        {
            CloseUpgradeWindow(resumeGame: false);
        }
        acquiredUpgrades.Clear();
        totalKills = 0;
        pendingCommonUpgrade = false;
        pendingRareUpgrade = false;
        currentOptions.Clear();
    }

    private void CloseUpgradeWindow(bool resumeGame = true)
    {
        if (!isWindowOpen) return;

        isWindowOpen = false;
        upgradeTimer = 0f;

        TimeManager.Instance?.SetDrainMultiplier(1f);
        AudioManager.Instance?.FadeMusicTo(1f, 0.3f);

        if (resumeGame) GameManager.Instance?.ResumeGame();
        OnUpgradeWindowClosed?.Invoke();
        OnUpgradeTimerChanged?.Invoke(0f);
        currentOptions.Clear();

        if (resumeGame)
        {
            if (pendingRareUpgrade)
            {
                pendingRareUpgrade = false;
                TriggerRareUpgrade();
            }
            else if (pendingCommonUpgrade)
            {
                pendingCommonUpgrade = false;
                TriggerCommonUpgrade();
            }
        }
    }

    private List<UpgradeData> GetRandomUpgrades(int count, bool rare)
    {
        // Los tres buffers se reutilizan entre ventanas en vez de crear listas nuevas cada vez.
        selectedBuffer.Clear();
        Refill(commonPoolBuffer, commonUpgrades);
        Refill(rarePoolBuffer, rareUpgrades);

        if (rare)
        {
            DrawFrom(rarePoolBuffer, count);
            // Si faltan raras, rellenar con comunes
            DrawFrom(commonPoolBuffer, count - selectedBuffer.Count);
        }
        else
        {
            // Probabilidad de rara empieza en 5% y sube 2% por cada 10 kills totales
            float rareChance = 0.05f + ((totalKills / 10) * 0.02f);
            rareChance = Mathf.Clamp(rareChance, 0.05f, 0.60f);

            for (int i = 0; i < count; i++)
            {
                bool rollRare = UnityEngine.Random.value <= rareChance;

                if (rollRare && rarePoolBuffer.Count > 0)
                    DrawFrom(rarePoolBuffer, 1);
                else if (commonPoolBuffer.Count > 0)
                    DrawFrom(commonPoolBuffer, 1);
            }
        }

        return selectedBuffer;
    }

    private static void Refill(List<UpgradeData> buffer, List<UpgradeData> source)
    {
        buffer.Clear();
        if (source == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null) buffer.Add(source[i]);
        }
    }

    private void DrawFrom(List<UpgradeData> pool, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) return;

            int idx = UnityEngine.Random.Range(0, pool.Count);
            selectedBuffer.Add(pool[idx]);

            pool[idx] = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
        }
    }
}
