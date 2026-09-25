using UnityEngine;

/// <summary>
/// Saltador (reloj de arena bronce): avanza despacio y cada pocos segundos se teletransporta
/// junto al jugador. Un anillo marca el destino antes del salto.
/// Pregunta: ¿me muevo cuando veo la marca?
/// </summary>
public class EnemyBlinker : EnemyBase
{
    [Header("Blinker Behavior")]
    public float moveSpeed = 1.6f;
    public float blinkInterval = 3.2f;
    [Tooltip("Segundos que se ve la marca del destino antes del salto.")]
    public float warnDuration = 0.6f;
    public float blinkMinDistance = 1.8f;
    public float blinkMaxDistance = 2.8f;
    [Tooltip("Margen respecto al borde de la cámara para no saltar fuera de la arena.")]
    public float screenMargin = 0.8f;
    public Color warnColor = new Color(1f, 0.7f, 0.35f, 1f);

    private float timer;
    private float markerTimer;
    private bool warning;
    private Vector2 destination;
    private Camera cam;

    protected override void OnEnable()
    {
        base.OnEnable();
        timer = blinkInterval;
        warning = false;
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
        rb.linearVelocity = direction * moveSpeed;
        FaceDirection(direction);

        timer -= deltaTime;
        if (!warning && timer <= warnDuration)
        {
            warning = true;
            markerTimer = 0f;
            destination = PickDestination();
        }

        if (warning)
        {
            markerTimer -= deltaTime;
            if (markerTimer <= 0f)
            {
                markerTimer = 0.2f;
                PickupManager.Instance?.SpawnRing(destination, warnColor, 0.7f);
            }
        }

        if (timer > 0f) return;

        timer = blinkInterval;
        warning = false;
        PickupManager.Instance?.SpawnRing(rb.position, warnColor, 0.5f);
        PlaceAt(destination);
        if (visualFeedback != null) visualFeedback.TriggerHitFlash();
    }

    private Vector2 PickDestination()
    {
        Vector2 target = (Vector2)playerTransform.position
            + Random.insideUnitCircle.normalized * Random.Range(blinkMinDistance, blinkMaxDistance);

        if (cam == null) cam = Camera.main;
        if (cam == null) return target;

        float halfHeight = cam.orthographicSize - screenMargin;
        float halfWidth = cam.orthographicSize * cam.aspect - screenMargin;
        Vector2 center = cam.transform.position;
        target.x = Mathf.Clamp(target.x, center.x - halfWidth, center.x + halfWidth);
        target.y = Mathf.Clamp(target.y, center.y - halfHeight, center.y + halfHeight);
        return target;
    }
}
