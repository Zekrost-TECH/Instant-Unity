using UnityEngine;

/// <summary>
/// Embestidor (punta de flecha lima): se acerca, se planta estirándose y parpadeando,
/// fija la dirección y embiste en línea recta. Pregunta: ¿esquivo de lado a tiempo?
/// </summary>
public class EnemyCharger : EnemyBase
{
    private enum Phase { Approach, Windup, Charge, Recover }

    [Header("Charger Behavior")]
    public float approachSpeed = 2.4f;
    [Tooltip("Distancia al jugador a la que se planta para embestir.")]
    public float triggerDistance = 5.5f;
    public float windupDuration = 0.65f;
    [Tooltip("Fracción final del aviso con la dirección ya fijada: la ventana real de esquiva.")]
    [Range(0.1f, 0.9f)] public float lockedFraction = 0.4f;
    public float chargeSpeed = 13f;
    public float chargeDuration = 0.45f;
    public float recoverDuration = 0.8f;
    [Tooltip("Cuánto se estira durante el aviso.")]
    public float windupStretch = 0.3f;

    private Phase phase;
    private float timer;
    private float blinkTimer;
    private Vector2 chargeDirection = Vector2.up;
    private Vector3 baseScale;

    protected override void Awake()
    {
        base.Awake();
        baseScale = transform.localScale;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        phase = Phase.Approach;
        timer = 0f;
        transform.localScale = baseScale;
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

        switch (phase)
        {
            case Phase.Approach:
                rb.linearVelocity = direction * approachSpeed;
                FaceDirection(direction);
                if (toPlayer.sqrMagnitude < triggerDistance * triggerDistance)
                {
                    phase = Phase.Windup;
                    timer = windupDuration;
                    blinkTimer = 0f;
                }
                break;

            case Phase.Windup:
                rb.linearVelocity = Vector2.zero;
                float progress = 1f - timer / windupDuration;
                if (progress < 1f - lockedFraction) chargeDirection = direction;
                FaceDirection(chargeDirection);

                // Se estira hacia donde va a embestir y parpadea cada vez más rápido.
                transform.localScale = new Vector3(
                    baseScale.x * (1f - windupStretch * 0.5f * progress),
                    baseScale.y * (1f + windupStretch * progress),
                    baseScale.z);

                blinkTimer -= deltaTime;
                if (blinkTimer <= 0f)
                {
                    blinkTimer = Mathf.Lerp(0.25f, 0.08f, progress);
                    if (visualFeedback != null) visualFeedback.TriggerHitFlash();
                }

                timer -= deltaTime;
                if (timer <= 0f)
                {
                    phase = Phase.Charge;
                    timer = chargeDuration;
                    transform.localScale = baseScale;
                }
                break;

            case Phase.Charge:
                rb.linearVelocity = chargeDirection * chargeSpeed;
                timer -= deltaTime;
                if (timer <= 0f)
                {
                    phase = Phase.Recover;
                    timer = recoverDuration;
                }
                break;

            case Phase.Recover:
                rb.linearVelocity = direction * approachSpeed * 0.3f;
                FaceDirection(direction);
                timer -= deltaTime;
                if (timer <= 0f) phase = Phase.Approach;
                break;
        }
    }
}
