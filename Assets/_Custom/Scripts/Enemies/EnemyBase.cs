using UnityEngine;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    [Tooltip("Vida máxima del enemigo.")]
    public int maxHealth = 2;
    [Tooltip("Tiempo (segundos) que se le suma al jugador al morir.")]
    public float timeRewardOnDeath = 0.75f;
    [Tooltip("Tiempo (segundos) que se le resta al jugador al tocarlo.")]
    public float timeDamageToPlayer = 6f;
    [Tooltip("Si es true, este enemigo cuenta como élite al morir.")]
    public bool isElite = false;
    [Tooltip("Si es true, este enemigo morirá instantáneamente al tocar al jugador (tipo kamikaze).")]
    public bool dieOnContactWithPlayer = false;

    [Header("Game Feel")]
    public MMF_Player damageFeedback;
    public Color baseColor = Color.red;
    public int deathParticleCount = 8;

    protected int currentHealth;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected EnemyVisualFeedback visualFeedback;

    private Collider2D ownCollider;
    private bool released;

    private static Transform cachedPlayerTransform;
    private static PlayerCombat cachedPlayerCombat;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Los colliders de enemigo son triggers: nunca reciben respuesta de colisión,
        // así que el solver dinámico es trabajo puro desperdiciado. En Kinematic se
        // mueven igual por linearVelocity pero salen del solver.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        ownCollider = GetComponent<Collider2D>();

        visualFeedback = GetComponent<EnemyVisualFeedback>();
        if (visualFeedback == null)
            visualFeedback = gameObject.AddComponent<EnemyVisualFeedback>();
    }

    /// <summary>
    /// OnEnable se llama cada vez que el ObjectPool activa el objeto.
    /// Es el equivalente al "constructor" del pool: reinicia el estado del enemigo.
    /// </summary>
    protected virtual void OnEnable()
    {
        currentHealth = maxHealth;
        released = false;

        if (visualFeedback != null)
        {
            visualFeedback.SetBaseColor(baseColor);
            visualFeedback.SetEliteGlow(isElite);
        }

        // Cacheamos al jugador usando una referencia estática para evitar FindGameObjectWithTag repetitivos
        if (cachedPlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                cachedPlayerTransform = playerObj.transform;
                cachedPlayerCombat = playerObj.GetComponent<PlayerCombat>();
            }
        }
        playerTransform = cachedPlayerTransform;

        // Registramos en EnemyManager
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(this);
    }

    protected virtual void OnDisable()
    {
        // Desregistramos al ser devuelto al pool
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(this);
    }

    // ── Tick centralizado ────────────────────────────────────────────────────
    // No hay Update/FixedUpdate por enemigo: con 60+ en pantalla, el coste de que
    // Unity despache N callbacks managed supera al del propio movimiento.
    // EnemyManager recorre la lista de activos y llama a estos métodos.

    public void Tick(float deltaTime)
    {
        if (released) return;
        UpdateMovement(deltaTime);
    }

    public void TickVisuals(float deltaTime)
    {
        if (visualFeedback != null) visualFeedback.Tick(deltaTime);
    }

    public void Halt()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Coloca al enemigo al salir del pool. Hay que mover el Rigidbody2D, no sólo el
    /// Transform: el proyecto tiene Auto Sync Transforms desactivado, así que escribir
    /// transform.position deja el cuerpo físico en su posición anterior y el siguiente
    /// paso de física devuelve al enemigo allí.
    /// </summary>
    public void PlaceAt(Vector3 position)
    {
        transform.position = position;

        if (rb == null) return;
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Devuelve el enemigo al pool sin recompensa, sonido ni conteo de baja.</summary>
    public void Recycle()
    {
        if (released) return;
        released = true;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(this);

        if (SpawnManager.Instance != null)
            SpawnManager.Instance.ReleaseEnemy(this);
        else
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Orienta el enemigo hacia una dirección. Escribir transform.up fuerza una
    /// sincronización Transform→Rigidbody en cada paso; SetRotation se queda en física.
    /// </summary>
    protected void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.000001f) return;
        rb.SetRotation(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    // ── Interfaz pública ─────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por PlayerCombat cuando el ataque automático alcanza a este enemigo.
    /// </summary>
    public virtual void OnHit(int damageAmount)
    {
        if (released) return;

        currentHealth -= damageAmount;

        if (damageFeedback != null) damageFeedback.PlayFeedbacks();
        if (visualFeedback != null) visualFeedback.TriggerHitFlash();

        if (playerTransform != null && ownCollider != null && HitVFXManager.Instance != null)
        {
            Vector3 hitPoint = ownCollider.ClosestPoint(playerTransform.position);
            HitVFXManager.Instance.SpawnBeam(playerTransform, hitPoint);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ── Comportamiento a implementar en subclases ────────────────────────────

    /// <summary>
    /// Cada tipo de enemigo define aquí su lógica de movimiento.
    /// Se llama desde EnemyManager.FixedUpdate mientras el juego está en Playing.
    /// </summary>
    protected abstract void UpdateMovement(float deltaTime);

    /// <summary>
    /// Muerte desde fuera (consumible de limpieza de pantalla): cuenta como baja pero
    /// no otorga tiempo ni suelta consumible para no encadenar drops infinitos.
    /// </summary>
    public void KillByConsumable()
    {
        Die(giveReward: false, isKill: true);
    }

    // ── Muerte ───────────────────────────────────────────────────────────────

    protected virtual void Die(bool giveReward = true, bool isKill = true)
    {
        // Guardia contra doble muerte: el pool usa collectionCheck:false, así que un
        // segundo Release duplicaría la instancia dentro del pool.
        if (released) return;
        released = true;

        Vector3 deathPosition = transform.position;
        Color deathColor = visualFeedback != null ? visualFeedback.BaseColor : baseColor;

        // 1. Suma tiempo al jugador
        if (giveReward && TimeManager.Instance != null)
        {
            TimeManager.Instance.AddTime(timeRewardOnDeath);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayTimeGainSFX();
            if (ParticleManager.Instance != null) ParticleManager.Instance.SpawnTimeGainParticles(deathPosition);
        }

        // 1b. Recompensa de consumible: sólo en bajas reales del jugador (no kamikaze)
        if (giveReward && PickupManager.Instance != null)
        {
            PickupManager.Instance.RollDrop(deathPosition, isElite);
        }

        // 2. Notifica al EnemyManager
        if (EnemyManager.Instance != null)
        {
            if (isKill)
                EnemyManager.Instance.NotifyEnemyDeath(this, isElite);
            else
                EnemyManager.Instance.UnregisterEnemy(this);
        }

        // 3. Feedback visual y háptico
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnDeathParticles(deathPosition, deathColor, deathParticleCount);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyDeathSFX(isElite);

        if (isElite && HapticManager.Instance != null)
            HapticManager.Instance.TriggerEliteKill();

        // 4. Devuelve el objeto al pool — NUNCA se llama a Destroy()
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.ReleaseEnemy(this);
        else
            gameObject.SetActive(false);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerContact(other);
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        HandlePlayerContact(other);
    }

    private void HandlePlayerContact(Collider2D other)
    {
        if (released || !other.CompareTag("Player")) return;

        // OnTriggerStay2D dispara cada paso de física por cada enemigo en contacto:
        // resolvemos el PlayerCombat una sola vez y lo reutilizamos.
        PlayerCombat playerCombat = cachedPlayerCombat;
        if (playerCombat == null)
        {
            playerCombat = other.GetComponent<PlayerCombat>();
            cachedPlayerCombat = playerCombat;
        }
        if (playerCombat == null) return;

        // Registramos si logramos hacer daño real al jugador (retorna falso si es invulnerable)
        bool damageDealt = playerCombat.TakeDamageFromEnemy(timeDamageToPlayer);

        if (dieOnContactWithPlayer && damageDealt)
        {
            // Si muere por chocar al jugador e infligir daño, no le da tiempo al jugador ni cuenta como baja
            Die(giveReward: false, isKill: false);
        }
    }
}
