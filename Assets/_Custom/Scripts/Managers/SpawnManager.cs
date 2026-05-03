using UnityEngine;
using UnityEngine.Pool;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Settings")]
    public float spawnRadius = 15f;
    public Transform playerTransform;

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

    private float gameTime = 0f;
    private float spawnTimer = 0f;
    private float eliteTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        fodderPool = CreateEnemyPool(fodderPrefab);
        fastPool = CreateEnemyPool(fastPrefab);
        tankPool = CreateEnemyPool(tankPrefab);
        shooterPool = CreateEnemyPool(shooterPrefab);
        elitePool = CreateEnemyPool(elitePrefab);

        if (projectilePrefab != null)
        {
            projectilePool = new ObjectPool<EnemyProjectile>(
                createFunc: () => Instantiate(projectilePrefab),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 100
            );
        }
    }

    private ObjectPool<EnemyBase> CreateEnemyPool(EnemyBase prefab)
    {
        if (prefab == null) return null;
        return new ObjectPool<EnemyBase>(
            createFunc: () => Instantiate(prefab),
            actionOnGet: enemy => enemy.gameObject.SetActive(true),
            actionOnRelease: enemy => enemy.gameObject.SetActive(false),
            actionOnDestroy: enemy => Destroy(enemy.gameObject),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        gameTime += Time.deltaTime;
        spawnTimer -= Time.deltaTime;
        eliteTimer += Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = GetSpawnRate(gameTime);
        }

        if (eliteTimer >= 60f) 
        {
            eliteTimer = 0f;
            SpawnElite();
        }
    }

    private float GetSpawnRate(float t)
    {
        float rate = 1.0f - (Mathf.Floor(t / 30f) * 0.12f);
        return Mathf.Max(rate, 0.25f);
    }

    private void SpawnEnemy()
    {
        ObjectPool<EnemyBase> poolToUse = DetermineEnemyPool(gameTime);
        if (poolToUse == null) return;

        EnemyBase enemy = poolToUse.Get();
        enemy.transform.position = GetSpawnPosition();
    }

    private void SpawnElite()
    {
        if (elitePool == null) return;
        EnemyBase enemy = elitePool.Get();
        enemy.transform.position = GetSpawnPosition();
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
        return projectilePool.Get();
    }

    public void ReleaseProjectile(EnemyProjectile projectile)
    {
        if (projectilePool != null) projectilePool.Release(projectile);
    }
}
