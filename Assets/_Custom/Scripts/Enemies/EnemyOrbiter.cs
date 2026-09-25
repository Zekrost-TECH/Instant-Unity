using UnityEngine;

/// <summary>
/// Satélite (anillo turquesa): gira alrededor del jugador justo fuera de su alcance y
/// cada pocos segundos pica en línea recta. Pregunta: ¿lo cazo en el picado?
/// </summary>
public class EnemyOrbiter : EnemyBase
{
    [Header("Orbiter Behavior")]
    public float approachSpeed = 3f;
    [Tooltip("Radio de la órbita. Algo mayor que el alcance base del jugador (3).")]
    public float orbitRadius = 4.2f;
    public float orbitSpeed = 3.6f;
    [Tooltip("Segundos entre picados.")]
    public float diveInterval = 3.5f;
    public float diveSpeed = 7.5f;
    public float diveDuration = 0.5f;
    [Tooltip("Parpadeo de aviso antes de picar.")]
    public float diveWarning = 0.4f;

    private float diveTimer;
    private float diveRemaining;
    private float orbitSign;
    private bool warned;
    private Vector2 diveDirection;

    protected override void OnEnable()
    {
        base.OnEnable();
        diveTimer = diveInterval * Random.Range(0.6f, 1f);
        diveRemaining = 0f;
        warned = false;
        orbitSign = Random.value < 0.5f ? -1f : 1f;
    }

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (diveRemaining > 0f)
        {
            diveRemaining -= deltaTime;
            rb.linearVelocity = diveDirection * diveSpeed;
            FaceDirection(diveDirection);
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.001f ? toPlayer / distance : Vector2.up;
        FaceDirection(direction);

        if (distance > orbitRadius + 2f)
        {
            rb.linearVelocity = direction * approachSpeed;
            return;
        }

        // Tangente para girar + corrección radial suave para mantener el radio.
        Vector2 tangent = new Vector2(-direction.y, direction.x) * orbitSign;
        float radialError = Mathf.Clamp(distance - orbitRadius, -1f, 1f);
        rb.linearVelocity = tangent * orbitSpeed + direction * (radialError * approachSpeed);

        diveTimer -= deltaTime;
        if (!warned && diveTimer <= diveWarning)
        {
            warned = true;
            if (visualFeedback != null) visualFeedback.TriggerHitFlash();
        }

        if (diveTimer <= 0f)
        {
            diveTimer = diveInterval;
            diveRemaining = diveDuration;
            diveDirection = direction;
            warned = false;
        }
    }
}
