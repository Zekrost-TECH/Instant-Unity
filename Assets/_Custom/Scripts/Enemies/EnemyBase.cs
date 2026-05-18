using UnityEngine;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    [Tooltip("Vida máxima del enemigo.")]
    public int maxHealth = 2;
    [Tooltip("Tiempo (segundos) que se le suma al jugador al morir.")]
    public float timeRewardOnDeath = 0.5f;
    [Tooltip("Tiempo (segundos) que se le resta al jugador al tocarlo.")]
    public float timeDamageToPlayer = 5f;
    [Tooltip("Si es true, este enemigo cuenta como élite al morir.")]
    public bool isElite = false;
    [Tooltip("Si es true, este enemigo morirá instantáneamente al tocar al jugador (tipo kamikaze).")]
    public bool dieOnContactWithPlayer = false;

    [Header("Game Feel")]
    public MMF_Player damageFeedback;

    protected int currentHealth;
    protected Rigidbody2D rb;
    protected Transform playerTransform;

    private static Transform cachedPlayerTransform;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>
    /// OnEnable se llama cada vez que el ObjectPool activa el objeto.
    /// Es el equivalente al "constructor" del pool: reinicia el estado del enemigo.
    /// </summary>
    protected virtual void OnEnable()
    {
        currentHealth = maxHealth;

        // Cacheamos al jugador usando una referencia estática para evitar FindGameObjectWithTag repetitivos
        if (cachedPlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) cachedPlayerTransform = playerObj.transform;
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

    protected virtual void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        UpdateMovement();
    }

    // ── Interfaz pública ─────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por PlayerCombat cuando el ataque automático alcanza a este enemigo.
    /// </summary>
    public virtual void OnHit(int damageAmount)
    {
        currentHealth -= damageAmount;
        
        if (damageFeedback != null) damageFeedback.PlayFeedbacks();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ── Comportamiento a implementar en subclases ────────────────────────────

    /// <summary>
    /// Cada tipo de enemigo define aquí su lógica de movimiento.
    /// Se llama desde FixedUpdate mientras el juego está en Playing.
    /// </summary>
    protected abstract void UpdateMovement();

    // ── Muerte ───────────────────────────────────────────────────────────────

    protected virtual void Die(bool giveReward = true, bool isKill = true)
    {
        // 1. Suma tiempo al jugador
        if (giveReward && TimeManager.Instance != null)
            TimeManager.Instance.AddTime(timeRewardOnDeath);

        // 2. Notifica al EnemyManager
        if (EnemyManager.Instance != null)
        {
            if (isKill)
                EnemyManager.Instance.NotifyEnemyDeath(this, isElite);
            else
                EnemyManager.Instance.UnregisterEnemy(this);
        }

        // 3. Devuelve el objeto al pool — NUNCA se llama a Destroy()
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.ReleaseEnemy(this);
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
        if (!other.CompareTag("Player")) return;

        PlayerCombat playerCombat = other.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            // Registramos si logramos hacer daño real al jugador (retorna falso si es invulnerable)
            bool damageDealt = playerCombat.TakeDamageFromEnemy(timeDamageToPlayer);

            if (dieOnContactWithPlayer && damageDealt)
            {
                // Si muere por chocar al jugador e infligir daño, no le da tiempo al jugador ni cuenta como baja
                Die(giveReward: false, isKill: false);
            }
        }
    }
}
