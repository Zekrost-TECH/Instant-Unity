using UnityEngine;

public class EnemyShooter : EnemyBase
{
    [Header("Shooter Behavior")]
    public float moveSpeed = 1.2f;
    public float shootCooldown = 3f;
    public float stoppingDistance = 5f;
    public float retreatDistance = 3f;

    [Header("Fase 2 (GDD): disparo con predicción")]
    [Tooltip("Segundos de partida a partir de los que apunta a donde ESTARÁ el jugador. Los tiradores entran a los 90s: la fase 2 llega un minuto después.")]
    public float phase2StartTime = 150f;
    [Tooltip("Multiplicador del cooldown de disparo en fase 2.")]
    public float phase2CooldownMultiplier = 0.75f;

    private float shootTimer;
    private Rigidbody2D playerBody;

    private bool InPhase2 => SpawnManager.Instance != null && SpawnManager.Instance.GameTime >= phase2StartTime;

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
            bool phase2 = InPhase2;
            shootTimer = shootCooldown * (phase2 ? phase2CooldownMultiplier : 1f);
            Shoot(directionToPlayer, phase2);
        }
    }

    private void Shoot(Vector2 directionToPlayer, bool leadTarget)
    {
        if (SpawnManager.Instance != null)
        {
            EnemyProjectile projectile = SpawnManager.Instance.GetProjectile();
            if (projectile != null)
            {
                Vector2 muzzle = rb.position + directionToPlayer * 0.5f;
                Vector2 aim = leadTarget ? LeadDirection(muzzle, projectile.speed) : directionToPlayer;
                projectile.Launch(muzzle, aim);
            }
        }
    }

    /// <summary>
    /// Predicción de primer orden: apunta a donde estará el jugador si mantiene su
    /// velocidad. Obliga a cambiar de dirección para esquivar, no basta con correr recto.
    /// </summary>
    private Vector2 LeadDirection(Vector2 muzzle, float projectileSpeed)
    {
        if (playerBody == null) playerBody = playerTransform.GetComponent<Rigidbody2D>();

        Vector2 target = playerTransform.position;
        if (playerBody != null && projectileSpeed > 0f)
        {
            float travelTime = Vector2.Distance(muzzle, target) / projectileSpeed;
            target += playerBody.linearVelocity * travelTime;
        }

        Vector2 aim = target - muzzle;
        return aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.up;
    }
}
