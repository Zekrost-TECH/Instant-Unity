using UnityEngine;
using UnityEngine.Pool;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Settings")]
    [Tooltip("El prefab del enemigo Fodder a spawnear.")]
    public EnemyBase fodderPrefab;
    [Tooltip("Radio de spawn alrededor del jugador.")]
    public float spawnRadius = 15f;
    [Tooltip("Transform del jugador para usar como centro del spawn.")]
    public Transform playerTransform;

    private ObjectPool<EnemyBase> fodderPool;
    private float gameTime = 0f;
    private float spawnTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        fodderPool = new ObjectPool<EnemyBase>(
            createFunc: () => Instantiate(fodderPrefab),
            actionOnGet: enemy => enemy.gameObject.SetActive(true),
            actionOnRelease: enemy => enemy.gameObject.SetActive(false),
            actionOnDestroy: enemy => Destroy(enemy.gameObject),
            collectionCheck: false,
            defaultCapacity: 30,
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

        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = GetSpawnRate(gameTime);
        }
    }

    private float GetSpawnRate(float t)
    {
        float rate = 1.0f - (Mathf.Floor(t / 30f) * 0.12f);
        return Mathf.Max(rate, 0.25f);
    }

    private void SpawnEnemy()
    {
        if (fodderPrefab == null) return;

        EnemyBase enemy = fodderPool.Get();
        enemy.transform.position = GetSpawnPosition();
    }

    private Vector3 GetSpawnPosition()
    {
        if (playerTransform == null) return Vector3.zero;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        return playerTransform.position + (Vector3)(randomDir * spawnRadius);
    }

    public void ReleaseEnemy(EnemyBase enemy)
    {
        fodderPool.Release(enemy);
    }
}
