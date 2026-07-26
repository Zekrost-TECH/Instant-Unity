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

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        float distanceSqr = toPlayer.sqrMagnitude;
        Vector2 directionToPlayer = distanceSqr > 0.000001f ? toPlayer / Mathf.Sqrt(distanceSqr) : Vector2.zero;

        // Comparaciones al cuadrado: evitan dos raíces cuadradas por enemigo y paso de física
        if (distanceSqr > stoppingDistance * stoppingDistance)
        {
            rb.linearVelocity = directionToPlayer * moveSpeed;
        }
        else if (distanceSqr < retreatDistance * retreatDistance)
        {
            rb.linearVelocity = -directionToPlayer * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        FaceDirection(directionToPlayer);
        UpdateShooting(directionToPlayer, deltaTime);
    }

    private void UpdateShooting(Vector2 directionToPlayer, float deltaTime)
    {
        // Antes usaba Time.deltaTime dentro de un paso de física: el cooldown
        // se desfasaba con el framerate.
        shootTimer -= deltaTime;
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
                Vector3 muzzle = (Vector3)rb.position + (Vector3)directionToPlayer * 0.5f;
                projectile.Launch(muzzle, directionToPlayer);
            }
        }
    }
}
