using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float timeDamageToPlayer = 5f;
    public float lifetime = 4f;

    private Rigidbody2D rb;
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
    }

    public void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;
        transform.up = direction;
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
