using UnityEngine;

/// <summary>
/// Bombardero (octágono limón): se acerca y al estar cerca se arma, parpadea y estalla.
/// La explosión quita tiempo al jugador y también daña a los demás enemigos.
/// Matarlo antes desactiva la bomba. Pregunta: ¿lo mato lejos o lo uso de arma?
/// </summary>
public class EnemyBomber : EnemyBase
{
    [Header("Bomber Behavior")]
    public float moveSpeed = 2.8f;
    [Tooltip("Distancia al jugador a la que se arma.")]
    public float armDistance = 2.2f;
    public float fuseDuration = 0.9f;
    public float blastRadius = 2f;
    [Tooltip("Daño a los otros enemigos atrapados en la explosión.")]
    public int blastEnemyDamage = 1;
    public Color blastColor = new Color(1f, 0.88f, 0.3f, 1f);

    private bool armed;
    private float fuseTimer;
    private float blinkTimer;

    protected override void OnEnable()
    {
        base.OnEnable();
        armed = false;
    }

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        Vector2 direction = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.up;
        FaceDirection(direction);

        if (!armed)
        {
            rb.linearVelocity = direction * moveSpeed;
            if (toPlayer.sqrMagnitude < armDistance * armDistance)
            {
                armed = true;
                fuseTimer = fuseDuration;
                blinkTimer = 0f;
            }
            return;
        }

        // Armado: casi quieto y parpadeando cada vez más rápido.
        rb.linearVelocity = direction * (moveSpeed * 0.25f);
        fuseTimer -= deltaTime;
        blinkTimer -= deltaTime;
        if (blinkTimer <= 0f)
        {
            blinkTimer = Mathf.Lerp(0.07f, 0.25f, fuseTimer / fuseDuration);
            if (visualFeedback != null) visualFeedback.TriggerHitFlash();
        }

        if (fuseTimer <= 0f) Explode();
    }

    private void Explode()
    {
        Vector2 center = rb.position;
        PickupManager.Instance?.SpawnRing(center, blastColor, blastRadius);
        ParticleManager.Instance?.SpawnDeathParticles(center, blastColor, 14);

        if (playerTransform != null && ((Vector2)playerTransform.position - center).sqrMagnitude <= blastRadius * blastRadius)
            DamagePlayer(timeDamageToPlayer);

        // Primero se retira (sin recompensa) para no quedar atrapado en su propia onda.
        Die(giveReward: false, isKill: false);
        EnemyManager.Instance?.DamageEnemiesInRadius(center, blastRadius, blastEnemyDamage);
    }
}
