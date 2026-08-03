using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject deathParticlePrefab;
    public GameObject timeGainParticlePrefab;
    public GameObject dashTrailPrefab;

    [Header("Death Particles")]
    public float deathSpeedMin = 3.5f;
    public float deathSpeedMax = 7.5f;
    public float deathGravityScale = 0.25f;
    public float deathLifetime = 0.4f;
    public int maxActiveDeathParticles = 160;
    public float deathScaleMin = 0.65f;
    public float deathScaleMax = 1.35f;
    public float deathSpinMin = -540f;
    public float deathSpinMax = 540f;

    [Header("Time Gain Particles")]
    public float timeGainSpeedMin = 1.5f;
    public float timeGainSpeedMax = 3.5f;
    public float timeGainLifetime = 0.6f;

    private ObjectPool<PooledParticle> deathPool;
    private ObjectPool<PooledParticle> timeGainPool;
    private ObjectPool<PooledParticle> dashTrailPool;

    private readonly List<ActiveParticle> active = new List<ActiveParticle>(256);
    private Transform container;
    private int activeDeathParticleCount;

    private const string DontDestroyOnLoadScene = "DontDestroyOnLoad";

    private static readonly Color TimeGainColor = new Color(0f, 1f, 0.53f);   // #00FF88
    private static readonly Color DashTrailColor = new Color(0.27f, 0.53f, 1f, 0.27f); // #4488FF44

    /// <summary>Instancia del pool con sus componentes ya resueltos (cero GetComponent en runtime).</summary>
    private class PooledParticle
    {
        public GameObject go;
        public Transform tr;
        public SpriteRenderer sr;
        public Rigidbody2D rb;
    }

    private struct ActiveParticle
    {
        public PooledParticle particle;
        public ObjectPool<PooledParticle> pool;
        public Color baseColor;
        public float elapsed;
        public float duration;
        public Vector3 startScale;
        public bool shrink;
    }

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

        container = new GameObject("ParticlePool").transform;
        container.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        deathPool = CreatePool(deathParticlePrefab, 60);
        timeGainPool = CreatePool(timeGainParticlePrefab, 40);
        dashTrailPool = CreatePool(dashTrailPrefab, 20);
    }

    private void Start()
    {
        // Si el manager sobrevive a los cambios de escena (BootstrapInitializer), el
        // contenedor debe sobrevivir también o el pool guardaría objetos destruidos.
        if (container != null && gameObject.scene.name == DontDestroyOnLoadScene)
        {
            DontDestroyOnLoad(container.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        active.Clear();
        activeDeathParticleCount = 0;
        deathPool?.Clear();
        timeGainPool?.Clear();
        dashTrailPool?.Clear();
        if (container != null) Destroy(container.gameObject);
        Instance = null;
    }

    private ObjectPool<PooledParticle> CreatePool(GameObject prefab, int capacity)
    {
        if (prefab == null) return null;

        return new ObjectPool<PooledParticle>(
            createFunc: () =>
            {
                GameObject instance = Instantiate(prefab, container);
                return new PooledParticle
                {
                    go = instance,
                    tr = instance.transform,
                    sr = instance.GetComponent<SpriteRenderer>(),
                    rb = instance.GetComponent<Rigidbody2D>()
                };
            },
            actionOnGet: p => p.go.SetActive(true),
            actionOnRelease: p =>
            {
                if (p.rb != null)
                {
                    p.rb.linearVelocity = Vector2.zero;
                    p.rb.angularVelocity = 0f;
                }
                if (p.go != null) p.go.SetActive(false);
            },
            actionOnDestroy: p => { if (p.go != null) Destroy(p.go); },
            collectionCheck: false,
            defaultCapacity: capacity,
            maxSize: capacity * 2
        );
    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            ActiveParticle entry = active[i];
            entry.elapsed += Time.unscaledDeltaTime;

            PooledParticle p = entry.particle;
            if (p.go == null)
            {
                if (entry.pool == deathPool)
                    activeDeathParticleCount = Mathf.Max(0, activeDeathParticleCount - 1);
                active.RemoveAt(i);
                continue;
            }

            float t = Mathf.Clamp01(entry.elapsed / entry.duration);
            if (p.sr != null)
            {
                Color c = entry.baseColor;
                c.a = Mathf.Lerp(entry.baseColor.a, 0f, t * t);
                p.sr.color = c;
            }

            if (entry.shrink)
            {
                float scaleT = Mathf.SmoothStep(0f, 1f, t);
                p.tr.localScale = Vector3.Lerp(entry.startScale, Vector3.zero, scaleT);
            }

            if (t < 1f)
            {
                active[i] = entry;
                continue;
            }

            active.RemoveAt(i);
            if (entry.pool == deathPool)
                activeDeathParticleCount = Mathf.Max(0, activeDeathParticleCount - 1);
            entry.pool.Release(p);
        }
    }

    public void SpawnDeathParticles(Vector3 position, Color color, int count = 12)
    {
        if (deathPool == null) return;

        int available = Mathf.Max(0, maxActiveDeathParticles - activeDeathParticleCount);
        int particlesToSpawn = Mathf.Min(count, available);

        for (int i = 0; i < particlesToSpawn; i++)
        {
            PooledParticle p = deathPool.Get();
            p.tr.position = position;
            p.tr.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float scale = Random.Range(deathScaleMin, deathScaleMax);
            p.tr.localScale = Vector3.one * scale;

            if (p.sr != null) p.sr.color = color;

            if (p.rb != null)
            {
                p.rb.position = position;
                p.rb.SetRotation(p.tr.eulerAngles.z);
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
                p.rb.linearVelocity = dir * Random.Range(deathSpeedMin, deathSpeedMax);
                p.rb.gravityScale = deathGravityScale;
                p.rb.angularVelocity = Random.Range(deathSpinMin, deathSpinMax);
            }

            Track(p, deathPool, color, deathLifetime, true);
            activeDeathParticleCount++;
        }
    }

    public void SpawnTimeGainParticles(Vector3 position, int count = 5)
    {
        if (timeGainPool == null) return;

        for (int i = 0; i < count; i++)
        {
            PooledParticle p = timeGainPool.Get();
            p.tr.position = position;

            if (p.sr != null) p.sr.color = TimeGainColor;

            if (p.rb != null)
            {
                p.rb.position = position;
                p.rb.SetRotation(0f);
                p.rb.linearVelocity = Vector2.up * Random.Range(timeGainSpeedMin, timeGainSpeedMax);
                p.rb.gravityScale = 0f;
            }

            Track(p, timeGainPool, TimeGainColor, timeGainLifetime);
        }
    }

    public GameObject SpawnDashTrail(Vector3 position, Vector2 direction, float duration = 0.15f)
    {
        if (dashTrailPool == null) return null;

        PooledParticle p = dashTrailPool.Get();
        p.tr.position = position;
        if (direction != Vector2.zero) p.tr.up = direction;

        if (p.rb != null)
        {
            p.rb.position = position;
            p.rb.SetRotation(p.tr.eulerAngles.z);
        }

        if (p.sr != null) p.sr.color = DashTrailColor;

        Track(p, dashTrailPool, DashTrailColor, duration);
        return p.go;
    }

    private void Track(PooledParticle particle, ObjectPool<PooledParticle> pool, Color baseColor, float duration, bool shrink = false)
    {
        active.Add(new ActiveParticle
        {
            particle = particle,
            pool = pool,
            baseColor = baseColor,
            elapsed = 0f,
            duration = Mathf.Max(0.001f, duration),
            startScale = particle.tr.localScale,
            shrink = shrink
        });
    }
}
