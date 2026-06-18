using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject deathParticlePrefab;
    public GameObject timeGainParticlePrefab;
    public GameObject dashTrailPrefab;

    private ObjectPool<GameObject> deathPool;
    private ObjectPool<GameObject> timeGainPool;
    private ObjectPool<GameObject> dashTrailPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        deathPool = CreatePool(deathParticlePrefab, 30);
        timeGainPool = CreatePool(timeGainParticlePrefab, 20);
        dashTrailPool = CreatePool(dashTrailPrefab, 10);
    }

    private ObjectPool<GameObject> CreatePool(GameObject prefab, int capacity)
    {
        if (prefab == null) return null;
        return new ObjectPool<GameObject>(
            createFunc: () => Instantiate(prefab),
            actionOnGet: p => p.SetActive(true),
            actionOnRelease: p => p.SetActive(false),
            actionOnDestroy: p => Destroy(p),
            collectionCheck: false,
            defaultCapacity: capacity,
            maxSize: capacity * 2
        );
    }

    public void SpawnDeathParticles(Vector3 position, Color color, int count = 12)
    {
        if (deathPool == null) return;

        for (int i = 0; i < count; i++)
        {
            GameObject p = deathPool.Get();
            p.transform.position = position;

            SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;

            Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                rb.linearVelocity = dir * Random.Range(60f, 200f);
                rb.gravityScale = 150f / Physics2D.gravity.magnitude;
            }

            StartCoroutine(ReturnAfterDelay(p, deathPool, 0.5f));
        }
    }

    public void SpawnTimeGainParticles(Vector3 position, int count = 5)
    {
        if (timeGainPool == null) return;

        Color timeColor = new Color(0f, 1f, 0.53f); // #00FF88

        for (int i = 0; i < count; i++)
        {
            GameObject p = timeGainPool.Get();
            p.transform.position = position;

            SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = timeColor;

            Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.up * Random.Range(30f, 80f);
                rb.gravityScale = 0f;
            }

            StartCoroutine(ReturnAfterDelay(p, timeGainPool, 0.6f));
        }
    }

    public GameObject SpawnDashTrail(Vector3 position, Vector2 direction, float duration = 0.15f)
    {
        if (dashTrailPool == null) return null;

        GameObject trail = dashTrailPool.Get();
        trail.transform.position = position;
        trail.transform.up = direction;

        SpriteRenderer sr = trail.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0.27f, 0.53f, 1f, 0.27f); // #4488FF44
        }

        StartCoroutine(ReturnAfterDelay(trail, dashTrailPool, duration));
        return trail;
    }

    private IEnumerator ReturnAfterDelay(GameObject p, ObjectPool<GameObject> pool, float delay)
    {
        SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
        Color baseColor = sr != null ? sr.color : Color.white;
        float elapsed = 0f;

        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Lerp(baseColor.a, 0f, elapsed / delay);
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }
            yield return null;
        }

        pool.Release(p);
    }
}
