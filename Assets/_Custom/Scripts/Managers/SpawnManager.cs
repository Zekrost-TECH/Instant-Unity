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
    [Tooltip("Máximo de enemigos vivos a la vez. Al alcanzarlo se deja de spawnear.")]
    public int maxActiveEnemies = 55;
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
    
    [Header("Projectile Prefabs")]
    public EnemyProjectile projectilePrefab;

    private ObjectPool<EnemyBase> fodderPool;
    private ObjectPool<EnemyBase> fastPool;
    private ObjectPool<EnemyBase> tankPool;
    private ObjectPool<EnemyBase> shooterPool;
    private ObjectPool<EnemyBase> elitePool;
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
        projectilePool?.Clear();
        if (poolContainer != null) Destroy(poolContainer.gameObject);
        Instance = null;
    }

    private ObjectPool<EnemyBase> CreateEnemyPool(EnemyBase prefab)
    {
        if (prefab == null) return null;
        return new ObjectPool<EnemyBase>(
            createFunc: () => Instantiate(prefab, poolContainer),
            actionOnGet: enemy => enemy.gameObject.SetActive(true),
            actionOnRelease: enemy => { if (enemy != null) enemy.gameObject.SetActive(false); },
            actionOnDestroy: enemy => { if (enemy != null) Destroy(enemy.gameObject); },
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 100
        );
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
            spawnTimer = GetSpawnRate(gameTime);
        }

        if (eliteTimer >= ELITE_INTERVAL)
        {
            eliteTimer = 0f;
            SpawnElite();
        }
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

        if (maxActiveEnemies - active <= 0)
        {
            Debug.LogWarning($"[SpawnManager] Presupuesto agotado: {active}/{maxActiveEnemies} enemigos vivos. Sube maxActiveEnemies.");
            return;
        }

        if (culledLastSweep > 0)
        {
            Debug.LogWarning($"[SpawnManager] El culling recicló {culledLastSweep} enemigos (radio {cullRadius}, spawn {spawnRadius}). " +
                             "Si esto se repite cada barrido, los enemigos están apareciendo lejos del jugador: sube cullRadius o revisa PlaceAt.");
            culledLastSweep = 0;
            return;
        }

        Debug.Log($"[SpawnManager] Playing, {active} enemigos vivos, presupuesto {maxActiveEnemies - active}, spawnTimer {spawnTimer:F2}.");
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
            if (enemy.isElite) continue;

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
        int budget = maxActiveEnemies - ActiveEnemyCount;
        if (budget <= 0) return;
        if (spawnCount > budget) spawnCount = budget;

        for (int i = 0; i < spawnCount; i++)
        {
            ObjectPool<EnemyBase> poolToUse = DetermineEnemyPool(gameTime);
            if (poolToUse == null) continue;

            EnemyBase enemy = poolToUse.Get();
            enemy.PlaceAt(GetSpawnPosition());
        }
    }

    private void SpawnElite()
    {
        if (elitePool == null) return;
        if (ActiveEnemyCount >= maxActiveEnemies) return;

        // El élite siempre tiene hueco: es el que dispara el upgrade raro.
        EnemyBase enemy = elitePool.Get();
        enemy.PlaceAt(GetSpawnPosition());
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
        if (enemy is EnemyFast && fastPool != null) fastPool.Release(enemy);
        else if (enemy is EnemyTank && tankPool != null) tankPool.Release(enemy);
        else if (enemy is EnemyShooter && shooterPool != null) shooterPool.Release(enemy);
        else if (enemy is EnemyElite && elitePool != null) elitePool.Release(enemy);
        else if (fodderPool != null) fodderPool.Release(enemy);
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
