using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float timeDamageToPlayer = 6f;
    public float lifetime = 4f;

    private Rigidbody2D rb;
    private float spawnTime;
    private bool released;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
        released = false;
    }

    /// <summary>
    /// Coloca y lanza. Con Auto Sync Transforms desactivado hay que mover también el
    /// Rigidbody2D, o el proyectil sale desde su posición anterior del pool.
    /// </summary>
    public void Launch(Vector3 position, Vector2 direction)
    {
        transform.position = position;
        if (rb != null) rb.position = position;

        transform.up = direction;
        if (rb != null) rb.linearVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero; 
            return;
        }

        if (rb.linearVelocity == Vector2.zero && transform.up != Vector3.zero)
        {
            rb.linearVelocity = transform.up * speed;
        }

        if (Time.time - spawnTime > lifetime)
        {
            ReleaseToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCombat playerCombat = other.GetComponent<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.TakeDamageFromEnemy(timeDamageToPlayer);
            }
            ReleaseToPool();
        }
    }

    private void ReleaseToPool()
    {
        // Guardia contra doble Release: el pool usa collectionCheck:false, así que una
        // segunda devolución metería la misma instancia dos veces en el pool.
        if (released) return;
        released = true;

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.ReleaseProjectile(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
