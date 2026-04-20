using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    public int health = 1;
    [Tooltip("Tiempo (segundos) que se le suma al jugador al morir.")]
    public float timeGivenOnDeath = 3f;
    [Tooltip("Tiempo (segundos) que se le resta al reloj del jugador al tocarlo.")]
    public float timeDamageToPlayer = 5f;
    
    protected Rigidbody2D rb;
    protected Transform playerTransform;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    public virtual void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.AddTime(timeGivenOnDeath);
            Debug.Log($"¡Mato 1 Enemigo! +{timeGivenOnDeath}s recuperados");
        }
        
        Destroy(gameObject);
    }

    protected virtual void OnTriggerStay2D(Collider2D collider)
    {
        // Las físicas ahora son Triggers para evitar empujes entre cuerpos
        if (collider.CompareTag("Player"))
        {
            PlayerMovement player = collider.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.TakeDamageFromEnemy(timeDamageToPlayer);
            }
        }
    }
}
