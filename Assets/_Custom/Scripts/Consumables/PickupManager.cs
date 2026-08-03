using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PickupManager : MonoBehaviour
{
    public static PickupManager Instance { get; private set; }

    [Header("Drop Rates")]
    [Tooltip("Probabilidad (0-1) de que un enemigo normal suelte un consumible.")]
    public float normalDropChance = 0.07f;
    [Tooltip("Probabilidad (0-1) de que un élite suelte un consumible.")]
    public float eliteDropChance = 1f;

    [Header("Efectos")]
    [Tooltip("Tiempo (segundos) que suma el consumible de tiempo.")]
    public float timeBonusAmount = 8f;
    [Tooltip("Multiplicador de velocidad del consumible.")]
    public float speedBoostMultiplier = 1.5f;
    [Tooltip("Duración del impulso de velocidad.")]
    public float speedBoostDuration = 6f;
    [Tooltip("Multiplicador de cadencia del consumible (<1 dispara más rápido).")]
    public float attackSpeedMultiplier = 0.5f;
    [Tooltip("Duración del impulso de cadencia.")]
    public float attackSpeedDuration = 6f;
    [Tooltip("Duración del triple disparo.")]
    public float tripleShotDuration = 8f;
    [Tooltip("Duración de la invulnerabilidad.")]
    public float invulnerabilityDuration = 4f;

    [Header("Pool")]
    public int poolCapacity = 16;
    public int poolMaxSize = 48;

    private ObjectPool<PickupBase> pickupPool;
    private Transform container;
    private readonly List<PickupBase> activePickups = new List<PickupBase>(32);

    private PlayerMovement cachedMovement;
    private PlayerCombat cachedCombat;

    private const string DontDestroyOnLoadScene = "DontDestroyOnLoad";

    private static readonly Color TimeColor = new Color(0f, 1f, 0.53f);       // #00FF88
    private static readonly Color SpeedColor = new Color(0f, 0.8f, 1f);        // #00CCFF
    private static readonly Color AttackSpeedColor = new Color(1f, 0.55f, 0f); // #FF8C00
    private static readonly Color TripleColor = new Color(1f, 0.3f, 1f);       // #FF4DFF
    private static readonly Color InvulnColor = new Color(1f, 0.85f, 0.1f);    // #FFD91A
    private static readonly Color ClearColor = new Color(1f, 0.25f, 0.25f);    // #FF4040

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        container = new GameObject("PickupPool").transform;
        container.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        // El prefab se construye en runtime (GeometryRenderer + trigger + imán):
        // así el sistema funciona sin wirear prefabs en el Inspector, como el láser del élite.
        GameObject template = new GameObject("PickupTemplate");
        template.transform.SetParent(container, false);
        template.AddComponent<GeometryRenderer>();

        CircleCollider2D trigger = template.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.4f;

        Rigidbody2D rb = template.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        PickupBase pickup = template.AddComponent<PickupBase>();
        template.SetActive(false);

        pickupPool = new ObjectPool<PickupBase>(
            createFunc: () => Instantiate(pickup, container),
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => { if (p != null) p.gameObject.SetActive(false); },
            actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
            collectionCheck: false,
            defaultCapacity: poolCapacity,
            maxSize: poolMaxSize
        );
    }

    private void Start()
    {
        if (container != null && gameObject.scene.name == DontDestroyOnLoadScene)
        {
            DontDestroyOnLoad(container.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        activePickups.Clear();
        pickupPool?.Clear();
        if (container != null) Destroy(container.gameObject);
        Instance = null;
    }

    /// <summary>
    /// Decide si un enemigo suelta consumible. El élite siempre suelta (eliteDropChance 1).
    /// </summary>
    public void RollDrop(Vector3 position, bool isElite)
    {
        float chance = isElite ? eliteDropChance : normalDropChance;
        if (Random.value > chance) return;

        SpawnPickup(RollType(), position);
    }

    /// <summary>
    /// Pesos: tiempo 25%, velocidad 20%, cadencia 20%, triple 12%, invulnerabilidad 12%, limpieza 11%.
    /// </summary>
    private ConsumableType RollType()
    {
        float roll = Random.value * 100f;
        if (roll < 25f) return ConsumableType.TimeBonus;
        if (roll < 45f) return ConsumableType.SpeedBoost;
        if (roll < 65f) return ConsumableType.AttackSpeedBoost;
        if (roll < 77f) return ConsumableType.TripleShot;
        if (roll < 89f) return ConsumableType.Invulnerability;
        return ConsumableType.ScreenClear;
    }

    private void SpawnPickup(ConsumableType type, Vector3 position)
    {
        if (pickupPool == null) return;

        PickupBase pickup = pickupPool.Get();
        (Color color, float size, GeometryRenderer.ShapeType shape) = GetVisual(type);
        pickup.Setup(type, color, size, shape);
        pickup.transform.position = position;
        activePickups.Add(pickup);
    }

    private (Color color, float size, GeometryRenderer.ShapeType shape) GetVisual(ConsumableType type)
    {
        switch (type)
        {
            case ConsumableType.TimeBonus: return (TimeColor, 0.45f, GeometryRenderer.ShapeType.Circle);
            case ConsumableType.SpeedBoost: return (SpeedColor, 0.5f, GeometryRenderer.ShapeType.Triangle);
            case ConsumableType.AttackSpeedBoost: return (AttackSpeedColor, 0.4f, GeometryRenderer.ShapeType.Square);
            case ConsumableType.TripleShot: return (TripleColor, 0.5f, GeometryRenderer.ShapeType.Hexagon);
            case ConsumableType.Invulnerability: return (InvulnColor, 0.5f, GeometryRenderer.ShapeType.Diamond);
            case ConsumableType.ScreenClear: return (ClearColor, 0.6f, GeometryRenderer.ShapeType.Circle);
            default: return (Color.white, 0.45f, GeometryRenderer.ShapeType.Circle);
        }
    }

    public void Collect(PickupBase pickup)
    {
        if (pickup == null) return;

        if (pickup.Type == ConsumableType.TimeBonus)
        {
            if (ParticleManager.Instance != null)
                ParticleManager.Instance.SpawnTimeGainParticles(pickup.transform.position);
        }

        ApplyEffect(pickup.Type);
        AudioManager.Instance?.PlayPickupSFX();
        HapticManager.Instance?.TriggerPickup();
        Recycle(pickup);
    }

    private void ApplyEffect(ConsumableType type)
    {
        switch (type)
        {
            case ConsumableType.TimeBonus:
                TimeManager.Instance?.AddTime(timeBonusAmount);
                break;

            case ConsumableType.SpeedBoost:
                EnsurePlayerRefs();
                if (cachedMovement != null) cachedMovement.ApplySpeedBoost(speedBoostMultiplier, speedBoostDuration);
                break;

            case ConsumableType.AttackSpeedBoost:
                EnsurePlayerRefs();
                if (cachedCombat != null) cachedCombat.ApplyAttackSpeedBoost(attackSpeedMultiplier, attackSpeedDuration);
                break;

            case ConsumableType.TripleShot:
                EnsurePlayerRefs();
                if (cachedCombat != null) cachedCombat.ApplyTripleShot(tripleShotDuration);
                break;

            case ConsumableType.Invulnerability:
                EnsurePlayerRefs();
                if (cachedMovement != null) cachedMovement.ApplyInvulnerability(invulnerabilityDuration);
                break;

            case ConsumableType.ScreenClear:
                EnemyManager.Instance?.KillAllEnemies();
                break;
        }
    }

    private void EnsurePlayerRefs()
    {
        if (cachedMovement != null && cachedCombat != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        cachedMovement = player.GetComponent<PlayerMovement>();
        cachedCombat = player.GetComponent<PlayerCombat>();
    }

    public void Recycle(PickupBase pickup)
    {
        if (pickup == null || pickupPool == null) return;

        int index = activePickups.IndexOf(pickup);
        if (index >= 0)
        {
            int last = activePickups.Count - 1;
            activePickups[index] = activePickups[last];
            activePickups.RemoveAt(last);
        }

        pickupPool.Release(pickup);
    }

    public void ClearAll()
    {
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            PickupBase pickup = activePickups[i];
            if (pickup != null && pickup.gameObject.activeInHierarchy)
            {
                pickupPool.Release(pickup);
            }
        }
        activePickups.Clear();
    }
}
