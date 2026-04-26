using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    [Tooltip("Vida máxima del enemigo.")]
    public int maxHealth = 1;
    [Tooltip("Tiempo (segundos) que se le suma al jugador al morir.")]
    public float timeRewardOnDeath = 3f;
    [Tooltip("Tiempo (segundos) que se le resta al jugador al tocarlo.")]
    public float timeDamageToPlayer = 5f;
    [Tooltip("Si es true, este enemigo cuenta como élite al morir.")]
    public bool isElite = false;

    protected int currentHealth;
    protected Rigidbody2D rb;
    protected Transform playerTransform;

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

        // Cacheamos al jugador si aún no lo tenemos
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

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

    protected virtual void Die()
    {
        // 1. Suma tiempo al jugador
        if (TimeManager.Instance != null)
            TimeManager.Instance.AddTime(timeRewardOnDeath);

        // 2. Notifica al EnemyManager (esto dispara OnEnemyKilled, OnKillCountChanged)
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.NotifyEnemyDeath(this, isElite);

        // 3. Devuelve el objeto al pool — NUNCA se llama a Destroy()
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.ReleaseEnemy(this);
    }

    // ── Contacto con el jugador ──────────────────────────────────────────────

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerCombat playerCombat = other.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.TakeDamageFromEnemy();
        }
    }
}
