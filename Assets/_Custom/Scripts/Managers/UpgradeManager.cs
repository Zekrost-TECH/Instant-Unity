using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public const float UPGRADE_DRAIN_MULTIPLIER = 0.2f;
    public const float UPGRADE_WINDOW_DURATION = 8f;
    public const float MAGNET_RADIUS = 7f;
    public const float VORACIOUS_DURATION = 15f;

    private const float CHAIN_WINDOW = 1f;
    private const int CHAIN_MIN_KILLS = 3;

    [Header("Pools")]
    public List<UpgradeData> commonUpgrades;
    public List<UpgradeData> rareUpgrades;

    [Header("Fragmentación")]
    [Tooltip("Radio de la explosión de cada enemigo al morir.")]
    [SerializeField] private float fragmentRadius = 1.6f;
    [SerializeField] private Color fragmentColor = new Color(1f, 0.4f, 0.13f, 1f);
    [SerializeField] private Color chainColor = new Color(0f, 1f, 0.53f, 1f); // #00FF88

    // Modificadores de la partida actual (sinergias que reaccionan a las bajas)
    public float MagnetStrength { get; private set; }
    public float VoraciousRemaining { get; private set; }
    private float voraciousBonus;
    private float chainBonus;
    private int fragmentDamage;
    private readonly Queue<float> recentKillTimes = new Queue<float>(16);
    private struct PendingExplosion
    {
        public Vector3 position;
        public int generation;
    }

    // Generaciones de la cadena: la baja directa explota, lo que esa explosión mata
    // explota una vez más, y ahí se corta.
    private const int FRAGMENT_MAX_GENERATIONS = 2;
    private readonly List<PendingExplosion> pendingExplosions = new List<PendingExplosion>(16);
    private int explosionGeneration = -1;

    private List<string> acquiredUpgrades = new List<string>();
    // Veces que se eligió cada mejora en esta partida (para UpgradeData.maxStacks).
    private readonly Dictionary<UpgradeData, int> stackCounts = new Dictionary<UpgradeData, int>();
    private int totalKills = 0;
    private bool isWindowOpen = false;
    private float upgradeTimer;
    private bool pendingCommonUpgrade;
    private bool pendingRareUpgrade;
    private bool upgradesHeld;

    private readonly List<UpgradeData> selectedBuffer = new List<UpgradeData>(4);
    private readonly List<UpgradeData> commonPoolBuffer = new List<UpgradeData>(16);
    private readonly List<UpgradeData> rarePoolBuffer = new List<UpgradeData>(16);
    private readonly List<UpgradeData> currentOptions = new List<UpgradeData>(4);

    public event Action<List<UpgradeData>> OnUpgradeWindowOpened;
    public event Action OnUpgradeWindowClosed;
    public event Action<float> OnUpgradeTimerChanged;

    public bool IsUpgradeWindowOpen => isWindowOpen;
    public IReadOnlyList<UpgradeData> CurrentOptions => currentOptions;

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

        if (isWindowOpen || upgradesHeld)
        {
            pendingRareUpgrade = true;
            return;
        }

        TriggerRareUpgrade();
    }

    private void HandleKillsThreshold()
    {
        if (isWindowOpen || upgradesHeld)
        {
            pendingCommonUpgrade = true;
            return;
        }

        TriggerCommonUpgrade();
    }

    private void Update()
    {
        TickRunModifiers();

        if (!isWindowOpen) return;

        upgradeTimer -= Time.unscaledDeltaTime;
        OnUpgradeTimerChanged?.Invoke(Mathf.Clamp01(upgradeTimer / UPGRADE_WINDOW_DURATION));

        if (upgradeTimer > 0f) return;

        if (currentOptions.Count > 0)
            ApplyUpgrade(currentOptions[0]);
        else
            CloseUpgradeWindow();
    }

    private void TickRunModifiers()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (VoraciousRemaining > 0f)
            VoraciousRemaining = Mathf.Max(0f, VoraciousRemaining - Time.deltaTime);

        ProcessExplosions();
    }

    /// <summary>
    /// Las explosiones se resuelven un frame después de la muerte: la cadena de
    /// fragmentación se ve propagarse y se evita la recursión Die→explosión→Die.
    /// </summary>
    private void ProcessExplosions()
    {
        int count = pendingExplosions.Count;
        if (count == 0 || EnemyManager.Instance == null) return;

        // Las muertes de estas explosiones añaden más a la lista: quedan para el frame siguiente.
        for (int i = 0; i < count; i++)
        {
            PendingExplosion explosion = pendingExplosions[i];
            explosionGeneration = explosion.generation;
            EnemyManager.Instance.DamageEnemiesInRadius(explosion.position, fragmentRadius, fragmentDamage);
            PickupManager.Instance?.SpawnRing(explosion.position, fragmentColor, fragmentRadius);
        }
        explosionGeneration = -1;
        pendingExplosions.RemoveRange(0, count);
    }

    /// <summary>
    /// Tiempo que da una baja del jugador tras aplicar Reloj voraz y Cadena temporal.
    /// Con Fragmentación, además encola la explosión del enemigo.
    /// </summary>
    public float ResolveKillReward(float baseReward, Vector3 position)
    {
        float reward = baseReward;
        if (VoraciousRemaining > 0f) reward *= 1f + voraciousBonus;

        if (chainBonus > 0f)
        {
            float now = Time.time;
            recentKillTimes.Enqueue(now);
            while (recentKillTimes.Count > 0 && now - recentKillTimes.Peek() > CHAIN_WINDOW)
                recentKillTimes.Dequeue();

            // Al llegar a 3 bajas en 1s se pagan las 3 de golpe; desde ahí, cada una extra.
            int chain = recentKillTimes.Count;
            if (chain == CHAIN_MIN_KILLS)
            {
                reward += chainBonus * CHAIN_MIN_KILLS;
                PickupManager.Instance?.SpawnFloatingText(position, "CHAIN!", chainColor);
            }
            else if (chain > CHAIN_MIN_KILLS)
            {
                reward += chainBonus;
            }
        }

        // Sin límite, cada enemigo de 1 HP muerto por una explosión volvía a explotar:
        // con el grupo apretado (Magnetismo) la cadena vaciaba la arena entera, y las
        // explosiones congeladas bajo la ventana de mejora detonaban todas al cerrarla.
        int generation = explosionGeneration + 1; // baja directa = generación 0
        if (fragmentDamage > 0 && generation < FRAGMENT_MAX_GENERATIONS)
            pendingExplosions.Add(new PendingExplosion { position = position, generation = generation });
        return reward;
    }

    private void ApplyRunModifier(UpgradeData upgrade)
    {
        switch (upgrade.type)
        {
            case UpgradeType.TimeChain:
                chainBonus += upgrade.value;
                break;
            case UpgradeType.Magnetism:
                MagnetStrength += upgrade.value;
                break;
            case UpgradeType.VoraciousClock:
                // Repetirlo renueva los 15s; el bonus no se acumula.
                voraciousBonus = Mathf.Max(voraciousBonus, upgrade.value);
                VoraciousRemaining = VORACIOUS_DURATION;
                break;
            case UpgradeType.Fragmentation:
                fragmentDamage += Mathf.Max(1, Mathf.RoundToInt(upgrade.value));
                break;
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
        upgradeTimer = UPGRADE_WINDOW_DURATION;
        currentOptions.Clear();
        currentOptions.AddRange(options);
        GameManager.Instance?.ChangeState(GameManager.GameState.Upgrade);

        // Pausa parcial: el reloj drena al 20% de velocidad
        TimeManager.Instance?.SetDrainMultiplier(UPGRADE_DRAIN_MULTIPLIER);
        AudioManager.Instance?.FadeMusicTo(0.3f, 0.3f);
        AudioManager.Instance?.PlayUpgradeAvailableSFX();

        OnUpgradeTimerChanged?.Invoke(1f);
        OnUpgradeWindowOpened?.Invoke(options);
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        if (!isWindowOpen || upgrade == null) return;

        acquiredUpgrades.Add(upgrade.id);
        stackCounts[upgrade] = GetStacks(upgrade) + 1;
        UpgradeEffects.ApplyUpgrade(upgrade);
        ApplyRunModifier(upgrade);
        CloseUpgradeWindow();
    }

    public void ResetUpgrades()
    {
        if (isWindowOpen)
        {
            CloseUpgradeWindow(resumeGame: false);
        }
        acquiredUpgrades.Clear();
        stackCounts.Clear();
        totalKills = 0;
        pendingCommonUpgrade = false;
        pendingRareUpgrade = false;
        upgradesHeld = false;
        currentOptions.Clear();

        MagnetStrength = 0f;
        VoraciousRemaining = 0f;
        voraciousBonus = 0f;
        chainBonus = 0f;
        fragmentDamage = 0;
        recentKillTimes.Clear();
        pendingExplosions.Clear();
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

        if (resumeGame) OpenPendingUpgrade();
    }

    /// <summary>
    /// Retiene las ventanas de mejora (rotura de barrera: la limpieza de pantalla suma
    /// bajas de golpe y las cartas taparían el cristal). Al soltar se abre la pendiente.
    /// </summary>
    public void HoldUpgrades(bool hold)
    {
        upgradesHeld = hold;
        if (hold || isWindowOpen) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        OpenPendingUpgrade();
    }

    private void OpenPendingUpgrade()
    {
        if (upgradesHeld) return;

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

    private int GetStacks(UpgradeData upgrade)
    {
        return stackCounts.TryGetValue(upgrade, out int stacks) ? stacks : 0;
    }

    /// <summary>
    /// Las mejoras que llegaron a su tope dejan de ofrecerse. Sin topes, a los pocos
    /// minutos el jugador acumulaba rango, cadencia y ahorro de drenaje sin límite.
    /// </summary>
    private void Refill(List<UpgradeData> buffer, List<UpgradeData> source)
    {
        buffer.Clear();
        if (source == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            UpgradeData upgrade = source[i];
            if (upgrade == null) continue;
            if (upgrade.maxStacks > 0 && GetStacks(upgrade) >= upgrade.maxStacks) continue;
            buffer.Add(upgrade);
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
