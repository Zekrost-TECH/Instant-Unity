using System.Collections.Generic;
using TMPro;
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
    private ObjectPool<PickupRingFx> ringPool;
    private ObjectPool<PickupFloatingText> floatingTextPool;
    private Transform container;
    private readonly List<PickupBase> activePickups = new List<PickupBase>(32);

    private PlayerMovement cachedMovement;
    private PlayerCombat cachedCombat;

    private const string DontDestroyOnLoadScene = "DontDestroyOnLoad";
    private const string PickupFontPath = "Fonts & Materials/LiberationSans SDF";

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

        // El pickup se construye en runtime con SpriteRenderer + sprite procedural blanco
        // (tinte por color): GeometryRenderer/mesh sin UVs se ve magenta en URP.
        GameObject template = new GameObject("PickupTemplate");
        template.transform.SetParent(container, false);

        SpriteRenderer sr = template.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralSprites.Get("circle");
        sr.sortingOrder = 10;

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

        ringPool = CreateRingPool();
        floatingTextPool = CreateFloatingTextPool();
    }

    private ObjectPool<PickupRingFx> CreateRingPool()
    {
        GameObject ringTemplate = new GameObject("PickupRingTemplate");
        ringTemplate.transform.SetParent(container, false);

        SpriteRenderer sr = ringTemplate.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralSprites.Get("circle");
        sr.sortingOrder = 20;

        PickupRingFx ring = ringTemplate.AddComponent<PickupRingFx>();
        ringTemplate.SetActive(false);

        return new ObjectPool<PickupRingFx>(
            createFunc: () => Instantiate(ring, container),
            actionOnGet: r => r.gameObject.SetActive(true),
            actionOnRelease: r => { if (r != null) r.gameObject.SetActive(false); },
            actionOnDestroy: r => { if (r != null) Destroy(r.gameObject); },
            collectionCheck: false,
            defaultCapacity: poolCapacity,
            maxSize: poolMaxSize
        );
    }

    private ObjectPool<PickupFloatingText> CreateFloatingTextPool()
    {
        GameObject textTemplate = new GameObject("PickupTextTemplate");
        textTemplate.transform.SetParent(container, false);

        TextMeshPro textMesh = textTemplate.AddComponent<TextMeshPro>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(PickupFontPath);
        if (font != null) textMesh.font = font;
        textMesh.fontSize = 3.2f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.enableWordWrapping = false;
        textMesh.alpha = 1f;
        textMesh.GetComponent<MeshRenderer>().sortingOrder = 30;

        PickupFloatingText floatingText = textTemplate.AddComponent<PickupFloatingText>();
        textTemplate.SetActive(false);

        return new ObjectPool<PickupFloatingText>(
            createFunc: () => Instantiate(floatingText, container),
            actionOnGet: t => t.gameObject.SetActive(true),
            actionOnRelease: t => { if (t != null) t.gameObject.SetActive(false); },
            actionOnDestroy: t => { if (t != null) Destroy(t.gameObject); },
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: 24
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
        ringPool?.Clear();
        floatingTextPool?.Clear();
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
        (Sprite sprite, Color color, float size) = GetVisual(type);
        pickup.Setup(type, sprite, color, size);
        pickup.transform.position = position;
        activePickups.Add(pickup);
    }

    private (Sprite sprite, Color color, float size) GetVisual(ConsumableType type)
    {
        switch (type)
        {
            case ConsumableType.TimeBonus: return (ProceduralSprites.Get("circle"), TimeColor, 0.45f);
            case ConsumableType.SpeedBoost: return (ProceduralSprites.Get("triangle"), SpeedColor, 0.5f);
            case ConsumableType.AttackSpeedBoost: return (ProceduralSprites.Get("square"), AttackSpeedColor, 0.4f);
            case ConsumableType.TripleShot: return (ProceduralSprites.Get("hexagon"), TripleColor, 0.5f);
            case ConsumableType.Invulnerability: return (ProceduralSprites.Get("diamond"), InvulnColor, 0.5f);
            case ConsumableType.ScreenClear: return (ProceduralSprites.Get("circle"), ClearColor, 0.6f);
            default: return (ProceduralSprites.Get("circle"), Color.white, 0.45f);
        }
    }

    private string GetLabel(ConsumableType type)
    {
        switch (type)
        {
            case ConsumableType.TimeBonus: return $"+{timeBonusAmount:0}s";
            case ConsumableType.SpeedBoost: return "SPEED UP!";
            case ConsumableType.AttackSpeedBoost: return "FAST ATTACKS!";
            case ConsumableType.TripleShot: return "TRIPLE SHOT!";
            case ConsumableType.Invulnerability: return "INVINCIBLE!";
            case ConsumableType.ScreenClear: return "CLEARED!";
            default: return string.Empty;
        }
    }

    public void Collect(PickupBase pickup)
    {
        if (pickup == null) return;

        Vector3 position = pickup.transform.position;
        Color color = GetVisual(pickup.Type).color;

        // Efectos al recoger: burst de partículas + anillo expansivo + texto flotante
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnDeathParticles(position, color, 10);

        SpawnRing(position, color);
        SpawnFloatingText(position, GetLabel(pickup.Type), color);

        ApplyEffect(pickup.Type);
        AudioManager.Instance?.PlayPickupSFX();
        HapticManager.Instance?.TriggerPickup();
        Recycle(pickup);
    }

    private void SpawnRing(Vector3 position, Color color)
    {
        if (ringPool == null) return;

        PickupRingFx ring = ringPool.Get();
        ring.Setup(color);
        ring.transform.position = position;
    }

    private void SpawnFloatingText(Vector3 position, string message, Color color)
    {
        if (floatingTextPool == null || string.IsNullOrEmpty(message)) return;

        PickupFloatingText floatingText = floatingTextPool.Get();
        floatingText.Setup(message, color);
        floatingText.transform.position = position + new Vector3(0f, 0.6f, 0f);
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

    public void RecycleRing(PickupRingFx ring)
    {
        if (ring == null || ringPool == null) return;
        ringPool.Release(ring);
    }

    public void RecycleFloatingText(PickupFloatingText floatingText)
    {
        if (floatingText == null || floatingTextPool == null) return;
        floatingTextPool.Release(floatingText);
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
