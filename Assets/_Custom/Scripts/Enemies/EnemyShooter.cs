using UnityEngine;

public class EnemyShooter : EnemyBase
{
    [Header("Shooter Behavior")]
    public float moveSpeed = 1.2f;
    public float shootCooldown = 3f;
    public float stoppingDistance = 5f;
    public float retreatDistance = 3f;

    private float shootTimer;

    protected override void OnEnable()
    {
        base.OnEnable();
        shootTimer = shootCooldown;
    }

    protected override void UpdateMovement()
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > stoppingDistance)
        {
            rb.linearVelocity = directionToPlayer * moveSpeed;
        }
        else if (distanceToPlayer < retreatDistance)
        {
            rb.linearVelocity = -directionToPlayer * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (directionToPlayer != Vector2.zero)
        {
            transform.up = directionToPlayer;
        }

        UpdateShooting(directionToPlayer);
    }

    private void UpdateShooting(Vector2 directionToPlayer)
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer = shootCooldown;
            Shoot(directionToPlayer);
        }
    }

    private void Shoot(Vector2 directionToPlayer)
    {
        if (SpawnManager.Instance != null)
        {
            EnemyProjectile projectile = SpawnManager.Instance.GetProjectile();
            if (projectile != null)
            {
                projectile.transform.position = transform.position + (Vector3)directionToPlayer * 0.5f;
                projectile.Launch(directionToPlayer);
            }
        }
    }
}
