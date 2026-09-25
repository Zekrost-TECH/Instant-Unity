using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Settings")]
    public float spawnRadius = 15f;
    public Transform playerTransform;

    [Header("Performance Budget")]
    [Tooltip("Tope de enemigos vivos al empezar la partida. Sube hasta maxActiveEnemies al llegar el Overtime.")]
    public int startActiveEnemies = 18;
    [Tooltip("Tope de enemigos vivos al llegar el Overtime. El spawn siempre lo alcanza: es lo que decide cuánto se llena la pantalla.")]
    public int maxActiveEnemies = 35;
    [Tooltip("Los enemigos que se alejen más de este radio del jugador vuelven al pool.")]
    public float cullRadius = 26f;
    [Tooltip("Cada cuánto (segundos) se barre la lista buscando enemigos fuera del radio.")]
    public float cullInterval = 0.5f;
    [Tooltip("Instancias precreadas por pool al arrancar, para no pagar Instantiate en mitad de la partida.")]
    public int prewarmPerPool = 12;
    [Tooltip("Ignora las colisiones enemigo-enemigo. Sus colliders son triggers, así que sólo generan callbacks inútiles.")]
    public bool disableEnemyToEnemyCollisions = true;
    [Tooltip("Nombre de la layer de enemigos usada para la optimización anterior.")]
    public string enemyLayerName = "Enemigos";

    [Header("Diagnóstico")]
    [Tooltip("Si no hay enemigos vivos, escribe en consola qué puerta los está bloqueando. Desactívalo cuando ya no lo necesites.")]
    public bool logSpawnDiagnostics = false;

    [Header("Enemy Prefabs")]
    public EnemyBase fodderPrefab;
    public EnemyBase fastPrefab;
    public EnemyBase tankPrefab;
    public EnemyBase shooterPrefab;
    public EnemyBase elitePrefab;
    [Tooltip("Jefe del Overtime (Chrono Warden): aparece al subir cada nivel.")]
    public EnemyBase bossPrefab;
    
    [Header("Projectile Prefabs")]
    public EnemyProjectile projectilePrefab;

    /// <summary>Enemigo que se suma a la mezcla a partir de cierto minuto de partida.</summary>
    [System.Serializable]
    public class ProgressionEnemy
    {
        public string displayName;
        public EnemyBase prefab;
        [Tooltip("Segundos de partida (GameTime) a partir de los que puede aparecer.")]
        public float unlockTime = 60f;
        [Tooltip("Peso relativo frente a los demás tipos de progresión ya desbloqueados.")]
        public float weight = 1f;
        [Tooltip("Cuántos aparecen juntos (p. ej. el enjambre).")]
        public int groupSize = 1;

        [System.NonSerialized] public ObjectPool<EnemyBase> pool;
        [System.NonSerialized] public bool debuted;
    }

    [Header("Enemigos de progresión")]
    [Tooltip("Tipos que se van desbloqueando para que la partida no se sienta repetitiva.")]
    public List<ProgressionEnemy> progressionEnemies = new List<ProgressionEnemy>();
    [Tooltip("Segundos de partida en los que empieza a subir la probabilidad de spawn de progresión.")]
    public float progressionStartTime = 45f;
    [Tooltip("Segundos que tarda la probabilidad en llegar a su máximo.")]
    public float progressionRampDuration = 180f;
    [Range(0f, 1f)] public float progressionMaxChance = 0.55f;
    [Tooltip("Instancias precreadas por cada tipo de progresión.")]
    public int prewarmPerProgressionPool = 3;

    [Header("Overtime (sin techo de dificultad)")]
    [Tooltip("Segundos de partida en los que empieza el Overtime (GDD: desde 3:00 la muerte es inevitable).")]
    public float overtimeStart = 180f;
    [Tooltip("Cada cuántos segundos sube un nivel.")]
    public float overtimeStepSeconds = 60f;
    // Drenaje, ingresos y vida crecen de forma EXPONENCIAL: con escalado lineal una build
    // al máximo (daño 9, alcance 6.5) seguía ganando más tiempo del que perdía en el nivel 6.
    [Tooltip("Multiplicador del drenaje del reloj por nivel (1.35 = x1.35, x1.82, x2.46...).")]
    public float overtimeDrainGrowth = 1.35f;
    [Tooltip("Multiplicador de TODO el tiempo ganado por nivel (0.7 = x0.7, x0.49, x0.34...).")]
    public float overtimeIncomeDecay = 0.7f;
    [Tooltip("Multiplicador de la vida de los enemigos que aparecen, por nivel (redondeado).")]
    public float overtimeHealthGrowth = 1.35f;
    [Tooltip("Velocidad extra de los enemigos por nivel.")]
    public float overtimeSpeedPerLevel = 0.06f;
    public float overtimeMaxSpeedMultiplier = 1.5f;
    [Tooltip("Tiempo extra que quita cada golpe, por nivel.")]
    public float overtimeHitPenaltyPerLevel = 0.25f;
    [Tooltip("Enemigos vivos extra permitidos por nivel. Poco: la dificultad del Overtime va en vida, velocidad y reloj, no en llenar la pantalla.")]
    public int overtimeExtraEnemiesPerLevel = 2;
    public int overtimeMaxActiveEnemies = 45;

    [Header("Overtime · Jefe y barreras")]
    [Tooltip("Segundos que se suman al tiempo máximo (y al reloj) al derrotar a cada jefe.")]
    public float barrierMaxTimeBonus = 5f;
    [Tooltip("Drenaje extra mientras haya un jefe vivo: romper la barrera rápido corta la hemorragia.")]
    public float bossDrainBonus = 1.2f;
    [Tooltip("Multiplicador del ritmo de spawn mientras haya un jefe vivo.")]
    public float bossSpawnRateBonus = 1.35f;
    [Tooltip("Segundos (tiempo real) con el juego congelado mientras la pantalla se agrieta, antes de estallar.")]
    public float barrierCrackDuration = 0.45f;
    [Tooltip("Tras estallar, las ventanas de mejora esperan esto para no tapar el cristal ni el cartel.")]
    public float barrierUpgradeHold = 1.3f;

    private ObjectPool<EnemyBase> fodderPool;
    private ObjectPool<EnemyBase> fastPool;
    private ObjectPool<EnemyBase> tankPool;
    private ObjectPool<EnemyBase> shooterPool;
    private ObjectPool<EnemyBase> elitePool;
    private ObjectPool<EnemyBase> bossPool;
    private EnemyBoss currentBoss;
    private int pendingBosses;
    private Coroutine barrierRoutine;
    private ObjectPool<EnemyProjectile> projectilePool;

    private const float ELITE_INTERVAL = 45f;
    private const string DontDestroyOnLoadScene = "DontDestroyOnLoad";

    private float gameTime = 0f;
    private float spawnTimer = 0f;
    private float eliteTimer = 0f;

    private Transform poolContainer;
    private readonly List<EnemyProjectile> activeProjectiles = new List<EnemyProjectile>(32);
    private readonly List<EnemyBase> clearBuffer = new List<EnemyBase>(64);
    private float cullTimer = 0f;
    private float diagnosticsTimer = 0f;
    private int culledLastSweep = 0;

    public float GameTime => gameTime;

    /// <summary>0 antes del Overtime; 1 al empezar y +1 por cada overtimeStepSeconds.</summary>
    public int OvertimeLevel => gameTime < overtimeStart ? 0 : 1 + Mathf.FloorToInt((gameTime - overtimeStart) / Mathf.Max(1f, overtimeStepSeconds));
    public float OvertimeDrainMultiplier => Mathf.Pow(overtimeDrainGrowth, OvertimeLevel) * (IsBossActive ? bossDrainBonus : 1f);

    public bool IsBossActive => currentBoss != null;
    /// <summary>Jefes de niveles ya alcanzados que esperan a que caiga el actual.</summary>
    public int PendingBossCount => pendingBosses;
    public int BarriersBroken { get; private set; }

    public EnemyBoss CurrentBoss => currentBoss;
    public float OvertimeIncomeMultiplier => Mathf.Pow(overtimeIncomeDecay, OvertimeLevel);
    public float OvertimeHealthMultiplier => Mathf.Pow(overtimeHealthGrowth, OvertimeLevel);
    public float OvertimeSpeedMultiplier => Mathf.Min(overtimeMaxSpeedMultiplier, 1f + overtimeSpeedPerLevel * OvertimeLevel);
    public float OvertimeHitPenaltyMultiplier => 1f + overtimeHitPenaltyPerLevel * OvertimeLevel;
    private int ActiveEnemyCap
    {
        get
        {
            // Antes del Overtime el tope crece con la partida: la presión también sube en número.
            if (OvertimeLevel == 0)
                return Mathf.RoundToInt(Mathf.Lerp(startActiveEnemies, maxActiveEnemies, gameTime / Mathf.Max(1f, overtimeStart)));
            return Mathf.Min(Mathf.Max(maxActiveEnemies, overtimeMaxActiveEnemies), maxActiveEnemies + overtimeExtraEnemiesPerLevel * OvertimeLevel);
        }
    }
    private int lastOvertimeLevel;

    /// <summary>Sube el nivel de Overtime (el HUD lo anuncia).</summary>
    public event System.Action<int> OnOvertimeLevelChanged;

    public event System.Action<EnemyBoss> OnBossSpawned;

    /// <summary>Barrera rota: posición del jefe y nuevo tiempo máximo. La pantalla empieza a agrietarse.</summary>
    public event System.Action<Vector3, float> OnBarrierBroken;

    /// <summary>La pantalla estalla: enemigos y proyectiles ya están limpios.</summary>
    public event System.Action OnBarrierShattered;

    public event System.Action OnEliteSpawned;

    /// <summary>Primera aparición de un tipo de progresión: nombre y color para el aviso del HUD.</summary>
    public event System.Action<string, Color> OnEnemyDebut;

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

        poolContainer = new GameObject("EnemyPool").transform;
        poolContainer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        fodderPool = CreateEnemyPool(fodderPrefab);
        fastPool = CreateEnemyPool(fastPrefab);
        tankPool = CreateEnemyPool(tankPrefab);
        shooterPool = CreateEnemyPool(shooterPrefab);
        elitePool = CreateEnemyPool(elitePrefab);
        bossPool = CreateEnemyPool(bossPrefab);

        for (int i = 0; i < progressionEnemies.Count; i++)
        {
            if (progressionEnemies[i] != null)
                progressionEnemies[i].pool = CreateEnemyPool(progressionEnemies[i].prefab);
        }

        if (projectilePrefab != null)
        {
            projectilePool = new ObjectPool<EnemyProjectile>(
                createFunc: () => Instantiate(projectilePrefab, poolContainer),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => { if (p != null) p.gameObject.SetActive(false); },
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 100
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        activeProjectiles.Clear();
        fodderPool?.Clear();
        fastPool?.Clear();
        tankPool?.Clear();
        shooterPool?.Clear();
        elitePool?.Clear();
        bossPool?.Clear();
        for (int i = 0; i < progressionEnemies.Count; i++) progressionEnemies[i]?.pool?.Clear();
        projectilePool?.Clear();
        if (poolContainer != null) Destroy(poolContainer.gameObject);
        Instance = null;
    }

    private ObjectPool<EnemyBase> CreateEnemyPool(EnemyBase prefab)
    {
        if (prefab == null) return null;

        // Cada instancia recuerda su pool: devolverla ya no depende de su clase (el
        // enjambre usa EnemyFodder con otro prefab, el divisor saca copias de su pool).
        ObjectPool<EnemyBase> pool = null;
        pool = new ObjectPool<EnemyBase>(
            createFunc: () =>
            {
                EnemyBase enemy = Instantiate(prefab, poolContainer);
                enemy.OwnerPool = pool;
                return enemy;
            },
            actionOnGet: enemy => enemy.gameObject.SetActive(true),
            actionOnRelease: enemy => { if (enemy != null) enemy.gameObject.SetActive(false); },
            actionOnDestroy: enemy => { if (enemy != null) Destroy(enemy.gameObject); },
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 100
        );
        return pool;
    }

    /// <summary>
    /// El SpawnManager sobrevive a los cambios de escena, así que su referencia al Player
    /// queda apuntando al de la partida anterior (destruido). Hay que re-adquirirla o los
    /// enemigos spawnean en el origen. Un Transform destruido compara == null en Unity.
    /// </summary>
    private void EnsurePlayerTransform()
    {
        if (playerTransform != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Start()
    {
        EnsurePlayerTransform();

        // Si el manager persiste entre escenas, los objetos pooleados deben persistir también.
        if (poolContainer != null && gameObject.scene.name == DontDestroyOnLoadScene)
        {
            DontDestroyOnLoad(poolContainer.gameObject);
        }

        // Los colliders de enemigo son triggers, así que un solapamiento enemigo-enemigo
        // no produce ningún efecto de juego: sólo un par de callbacks OnTriggerStay2D por
        // paso de física. Amontonados alrededor del jugador eso crece de forma cuadrática.
        if (disableEnemyToEnemyCollisions)
        {
            int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
            if (enemyLayer >= 0)
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            else
                Debug.LogWarning($"[SpawnManager] No existe la layer '{enemyLayerName}'; no se pudo desactivar la colisión enemigo-enemigo.");
        }

        PrewarmPools();
    }

    private void PrewarmPools()
    {
        if (prewarmPerPool <= 0) return;

        Prewarm(fodderPool, prewarmPerPool);
        Prewarm(fastPool, prewarmPerPool);
        Prewarm(tankPool, prewarmPerPool);
        Prewarm(shooterPool, prewarmPerPool);
        Prewarm(elitePool, Mathf.Min(prewarmPerPool, 4));
        Prewarm(bossPool, 1);
        for (int i = 0; i < progressionEnemies.Count; i++)
        {
            if (progressionEnemies[i] != null) Prewarm(progressionEnemies[i].pool, prewarmPerProgressionPool);
        }

        if (projectilePool != null)
        {
            for (int i = 0; i < prewarmPerPool; i++)
            {
                EnemyProjectile projectile = projectilePool.Get();
                projectilePool.Release(projectile);
            }
        }
    }

    private void Prewarm(ObjectPool<EnemyBase> pool, int count)
    {
        if (pool == null || count <= 0) return;

        clearBuffer.Clear();
        for (int i = 0; i < count; i++)
        {
            clearBuffer.Add(pool.Get());
        }
        for (int i = 0; i < clearBuffer.Count; i++)
        {
            pool.Release(clearBuffer[i]);
        }
        clearBuffer.Clear();
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        GameManager.GameState state = GameManager.Instance.CurrentState;
        if (state != GameManager.GameState.Playing && state != GameManager.GameState.Upgrade)
            return;

        // Barato cuando ya es válida; cubre el Player nuevo tras recargar la escena.
        EnsurePlayerTransform();

        if (state != GameManager.GameState.Playing)
            return;

        float dt = Time.deltaTime;
        gameTime += dt;
        spawnTimer -= dt;
        eliteTimer += dt;

        if (logSpawnDiagnostics) RunDiagnostics(dt);

        cullTimer -= dt;
        if (cullTimer <= 0f)
        {
            cullTimer = cullInterval;
            CullDistantEnemies();
        }

        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = GetSpawnRate(gameTime) / (IsBossActive ? bossSpawnRateBonus : 1f);
        }

        if (eliteTimer >= ELITE_INTERVAL)
        {
            eliteTimer = 0f;
            SpawnElite();
        }

        CheckProgressionDebuts();

        int overtime = OvertimeLevel;
        if (overtime != lastOvertimeLevel)
        {
            lastOvertimeLevel = overtime;
            OnOvertimeLevelChanged?.Invoke(overtime);
            if (overtime > 0) SpawnBoss();
        }
    }

    /// <summary>
    /// Cada tipo de progresión se presenta solo al desbloquearse: aparece un grupo
    /// garantizado y el HUD anuncia su nombre, para que el jugador sepa qué es.
    /// </summary>
    private void CheckProgressionDebuts()
    {
        for (int i = 0; i < progressionEnemies.Count; i++)
        {
            ProgressionEnemy entry = progressionEnemies[i];
            if (entry == null || entry.debuted || entry.pool == null || gameTime < entry.unlockTime) continue;

            entry.debuted = true;
            SpawnGroup(entry, entry.groupSize);
            OnEnemyDebut?.Invoke(entry.displayName, entry.prefab.baseColor);
        }
    }

    /// <summary>
    /// Probabilidad de que un spawn sea de progresión: 0 hasta progressionStartTime y
    /// sube hasta progressionMaxChance. Elige por peso entre los ya desbloqueados.
    /// </summary>
    private ProgressionEnemy PickProgressionEnemy(float t)
    {
        float chance = progressionMaxChance * Mathf.Clamp01((t - progressionStartTime) / Mathf.Max(1f, progressionRampDuration));
        if (chance <= 0f || Random.value >= chance) return null;

        float totalWeight = 0f;
        for (int i = 0; i < progressionEnemies.Count; i++)
        {
            ProgressionEnemy entry = progressionEnemies[i];
            if (entry != null && entry.pool != null && t >= entry.unlockTime) totalWeight += entry.weight;
        }
        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;
        ProgressionEnemy picked = null;
        for (int i = 0; i < progressionEnemies.Count; i++)
        {
            ProgressionEnemy entry = progressionEnemies[i];
            if (entry == null || entry.pool == null || t < entry.unlockTime) continue;

            picked = entry;
            roll -= entry.weight;
            if (roll <= 0f) break;
        }
        return picked;
    }

    private int SpawnGroup(ProgressionEnemy entry, int count)
    {
        Vector3 center = GetSpawnPosition();
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = count > 1 ? (Vector3)(Random.insideUnitCircle * 0.9f) : Vector3.zero;
            entry.pool.Get().PlaceAt(center + offset);
        }
        return count;
    }

    /// <summary>
    /// Cada nivel de Overtime trae un jefe, pero sólo hay uno a la vez: si el actual sigue
    /// vivo o su barrera aún se está rompiendo, el nuevo espera en cola. Sale con la vida
    /// del nivel en que aparece, así que tardar en romper barreras las encarece.
    /// </summary>
    private void SpawnBoss()
    {
        if (bossPool == null) return;
        if (currentBoss != null || barrierRoutine != null)
        {
            pendingBosses++;
            return;
        }

        EnemyBoss boss = bossPool.Get() as EnemyBoss;
        if (boss == null) return;

        boss.PlaceAt(GetSpawnPosition());
        currentBoss = boss;
        AudioManager.Instance?.SetOvertimeTension(true);
        OnBossSpawned?.Invoke(boss);
    }

    /// <summary>El jefe murió de verdad: se rompe la barrera y sube el tiempo máximo.</summary>
    public void NotifyBossDefeated(EnemyBoss boss, Vector3 position)
    {
        if (boss != currentBoss) return;
        currentBoss = null;

        BarriersBroken++;
        TimeManager.Instance?.RaiseMaxTime(barrierMaxTimeBonus);
        // Con otro jefe en cola la tensión sigue: apagarla y encenderla en 2s sería un vaivén.
        if (pendingBosses == 0) AudioManager.Instance?.SetOvertimeTension(false);

        float newMax = TimeManager.Instance != null ? TimeManager.Instance.MaxTime : TimeManager.TIME_MAX;
        OnBarrierBroken?.Invoke(position, newMax);

        barrierRoutine = StartCoroutine(ShatterBarrier(position));
    }

    /// <summary>
    /// La imagen se congela y se agrieta; al estallar se lleva a todos los enemigos y
    /// proyectiles. Las mejoras que dispare esa limpieza esperan a que se lea el cartel.
    /// </summary>
    private IEnumerator ShatterBarrier(Vector3 position)
    {
        UpgradeManager.Instance?.HoldUpgrades(true);
        AudioManager.Instance?.PlayBarrierCrackSFX();
        HapticManager.Instance?.TriggerEliteKill();
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(barrierCrackDuration);

        Time.timeScale = 1f;
        EnemyManager.Instance?.KillAllEnemies();
        ClearProjectiles();

        // Onda dorada doble donde cayó: se lee como una barrera que se rompe.
        Color gold = new Color(1f, 0.8f, 0.25f, 1f);
        PickupManager.Instance?.SpawnRing(position, gold, 3f);
        PickupManager.Instance?.SpawnRing(position, Color.white, 5.5f);
        ParticleManager.Instance?.SpawnDeathParticles(position, gold, 30);
        AudioManager.Instance?.PlayBarrierBrokenSFX();
        HapticManager.Instance?.TriggerDeath();
        OnBarrierShattered?.Invoke();

        yield return new WaitForSecondsRealtime(barrierUpgradeHold);

        barrierRoutine = null;
        UpgradeManager.Instance?.HoldUpgrades(false);

        // Siguiente barrera de la cola, ya con el cartel leído.
        if (pendingBosses > 0)
        {
            pendingBosses--;
            SpawnBoss();
        }
    }

    /// <summary>
    /// Esbirro de rebaño en una posición concreta (lo usa el Invocador). Respeta el tope
    /// de enemigos vivos. Devuelve null si no hay hueco.
    /// </summary>
    public EnemyBase SpawnMinion(Vector3 position)
    {
        if (fodderPool == null || ActiveEnemyCount >= ActiveEnemyCap) return null;

        EnemyBase minion = fodderPool.Get();
        minion.PlaceAt(position);
        return minion;
    }

    private int ActiveEnemyCount => EnemyManager.Instance != null ? EnemyManager.Instance.ActiveEnemies.Count : 0;

    /// <summary>
    /// "No aparecen enemigos" tiene media docena de causas posibles y todas fallan en
    /// silencio. Esto dice cuál es en vez de obligar a adivinar.
    /// </summary>
    private void RunDiagnostics(float dt)
    {
        diagnosticsTimer -= dt;
        if (diagnosticsTimer > 0f) return;
        diagnosticsTimer = 1f;

        int active = ActiveEnemyCount;
        if (active > 0 && culledLastSweep == 0) return;

        if (EnemyManager.Instance == null)
        {
            Debug.LogError("[SpawnManager] No hay EnemyManager en la escena: nadie recorre los enemigos, así que no se mueven ni se cuentan.");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[SpawnManager] playerTransform es null (¿el Player tiene el tag 'Player'?). Los enemigos spawnean en el origen.");
            return;
        }

        if (fodderPool == null && fastPool == null && tankPool == null && shooterPool == null)
        {
            Debug.LogError("[SpawnManager] Ningún pool creado: faltan prefabs de enemigo en el Inspector.");
            return;
        }

        if (ActiveEnemyCap - active <= 0)
        {
            Debug.LogWarning($"[SpawnManager] Presupuesto agotado: {active}/{ActiveEnemyCap} enemigos vivos. Sube maxActiveEnemies.");
            return;
        }

        if (culledLastSweep > 0)
        {
            Debug.LogWarning($"[SpawnManager] El culling recicló {culledLastSweep} enemigos (radio {cullRadius}, spawn {spawnRadius}). " +
                             "Si esto se repite cada barrido, los enemigos están apareciendo lejos del jugador: sube cullRadius o revisa PlaceAt.");
            culledLastSweep = 0;
            return;
        }

        Debug.Log($"[SpawnManager] Playing, {active} enemigos vivos, presupuesto {ActiveEnemyCap - active}, spawnTimer {spawnTimer:F2}.");
    }

    /// <summary>
    /// Recicla los enemigos que se quedaron muy atrás. Sin esto la lista de activos
    /// sólo crece: los lentos (Tank, Shooter) nunca alcanzan al jugador y se acumulan
    /// durante toda la partida.
    /// </summary>
    private void CullDistantEnemies()
    {
        if (EnemyManager.Instance == null || playerTransform == null) return;

        Vector2 playerPosition = playerTransform.position;
        float cullRadiusSqr = cullRadius * cullRadius;

        clearBuffer.Clear();
        List<EnemyBase> activeEnemies = EnemyManager.Instance.ActiveEnemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyBase enemy = activeEnemies[i];
            if (enemy == null) continue;

            // El élite nunca se recicla: el jugador corre más que él, y perderlo
            // significaría perder el upgrade raro que tiene garantizado.
            if (enemy.isElite || enemy is EnemyBoss) continue;

            if (((Vector2)enemy.transform.position - playerPosition).sqrMagnitude > cullRadiusSqr)
                clearBuffer.Add(enemy);
        }

        culledLastSweep = clearBuffer.Count;
        for (int i = 0; i < clearBuffer.Count; i++)
        {
            clearBuffer[i].Recycle();
        }
        clearBuffer.Clear();
    }

    public void ResetGameTime()
    {
        gameTime = 0f;
        spawnTimer = 0f;
        eliteTimer = 0f;
        cullTimer = 0f;
        lastOvertimeLevel = 0;
        currentBoss = null;
        pendingBosses = 0;
        BarriersBroken = 0;
        AudioManager.Instance?.SetOvertimeTension(false);
        if (barrierRoutine != null)
        {
            StopCoroutine(barrierRoutine);
            barrierRoutine = null;
            Time.timeScale = 1f;
        }
        for (int i = 0; i < progressionEnemies.Count; i++)
        {
            if (progressionEnemies[i] != null) progressionEnemies[i].debuted = false;
        }
    }

    public void ClearAllEnemies()
    {
        if (EnemyManager.Instance != null)
        {
            // Copia para iterar de forma segura mientras los OnDisable modifican la lista original.
            // El buffer se reutiliza para no asignar en cada reinicio.
            clearBuffer.Clear();
            clearBuffer.AddRange(EnemyManager.Instance.ActiveEnemies);

            for (int i = 0; i < clearBuffer.Count; i++)
            {
                EnemyBase enemy = clearBuffer[i];
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    ReleaseEnemy(enemy);
                }
            }
            clearBuffer.Clear();
        }

        ClearProjectiles();
    }

    private void ClearProjectiles()
    {
        // Se recorre la lista de proyectiles activos en vez de un FindObjectsByType (escaneo completo de la escena)
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            EnemyProjectile projectile = activeProjectiles[i];
            if (projectile != null && projectile.gameObject.activeInHierarchy)
            {
                ReleaseProjectile(projectile);
            }
        }
        activeProjectiles.Clear();
    }

    private float GetSpawnRate(float t)
    {
        // Iniciamos en un tempo más pausado y bajamos hasta un ritmo limpio pero tenso (0.5s)
        float rate = 1.2f - (t * 0.004f);
        return Mathf.Max(rate, 0.45f);
    }

    private void SpawnEnemy()
    {
        // Spawnea cantidades más moderadas de enemigos (1 a 3 como máximo)
        // 0-45s -> 1 enemigo. 45s-90s -> 1-2 enemigos. 90s+ -> 2-3 enemigos a la vez.
        int baseCount = 1 + Mathf.FloorToInt(gameTime / 45f);
        int spawnCount = Random.Range(baseCount, baseCount + 2);
        spawnCount = Mathf.Clamp(spawnCount, 1, 3); // Clampeado entre 1 y 3 para evitar amontonamiento y lag

        // Techo duro de enemigos vivos: el ritmo de spawn crece con el tiempo pero el
        // de bajas no, así que sin tope la escena se satura sola.
        int budget = ActiveEnemyCap - ActiveEnemyCount;
        if (budget <= 0) return;
        if (spawnCount > budget) spawnCount = budget;

        for (int i = 0; i < spawnCount; i++)
        {
            ProgressionEnemy progression = PickProgressionEnemy(gameTime);
            if (progression != null)
            {
                // Los grupos (enjambre) cuentan contra el presupuesto de este spawn.
                int group = Mathf.Clamp(progression.groupSize, 1, Mathf.Max(1, budget - i));
                SpawnGroup(progression, group);
                i += group - 1;
                continue;
            }

            ObjectPool<EnemyBase> poolToUse = DetermineEnemyPool(gameTime);
            if (poolToUse == null) continue;

            EnemyBase enemy = poolToUse.Get();
            enemy.PlaceAt(GetSpawnPosition());
        }
    }

    private void SpawnElite()
    {
        if (elitePool == null) return;

        // El élite ignora el tope de enemigos vivos: es el que dispara el upgrade raro,
        // y con la arena llena en late game se saltaba entero durante 45 segundos.
        EnemyBase enemy = elitePool.Get();
        enemy.PlaceAt(GetSpawnPosition());
        OnEliteSpawned?.Invoke();
    }

    private ObjectPool<EnemyBase> DetermineEnemyPool(float t)
    {
        float rand = Random.value;

        if (t < 30f)
        {
            return fodderPool;
        }
        else if (t < 60f)
        {
            if (rand < 0.7f) return fodderPool;
            return fastPool;
        }
        else if (t < 90f)
        {
            if (rand < 0.5f) return fodderPool;
            if (rand < 0.8f) return fastPool;
            return tankPool;
        }
        else
        {
            if (rand < 0.3f) return fodderPool;
            if (rand < 0.5f) return fastPool;
            if (rand < 0.8f) return tankPool;
            return shooterPool;
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (playerTransform == null) return Vector3.zero;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        return playerTransform.position + (Vector3)(randomDir * spawnRadius);
    }

    public void ReleaseEnemy(EnemyBase enemy)
    {
        if (enemy == null) return;

        if (enemy.OwnerPool != null) enemy.OwnerPool.Release(enemy);
        else enemy.gameObject.SetActive(false);
    }

    public EnemyProjectile GetProjectile()
    {
        if (projectilePool == null) return null;

        EnemyProjectile projectile = projectilePool.Get();
        if (projectile != null) activeProjectiles.Add(projectile);
        return projectile;
    }

    public void ReleaseProjectile(EnemyProjectile projectile)
    {
        if (projectile == null || projectilePool == null) return;

        int index = activeProjectiles.IndexOf(projectile);
        if (index >= 0)
        {
            int last = activeProjectiles.Count - 1;
            activeProjectiles[index] = activeProjectiles[last];
            activeProjectiles.RemoveAt(last);
        }

        projectilePool.Release(projectile);
    }
}
