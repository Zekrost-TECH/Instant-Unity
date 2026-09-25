using UnityEngine;

/// <summary>
/// Escudero (escudo azul): su frente redondo bloquea el ataque automático y gira
/// despacio. Hay que flanquearlo con el dash o usar daño en área (onda, zona,
/// fragmentación). Pregunta: ¿me reposiciono para darle por la espalda?
/// </summary>
public class EnemyShielder : EnemyBase
{
    [Header("Shielder Behavior")]
    public float moveSpeed = 1.8f;
    [Tooltip("Grados por segundo que puede girar: un dash por detrás deja su espalda expuesta.")]
    public float turnSpeed = 90f;
    [Tooltip("Medio ángulo del escudo frontal.")]
    public float shieldHalfAngle = 65f;
    public Color blockColor = new Color(0.6f, 0.8f, 1f, 1f);

    private Vector2 facing = Vector2.up;

    /// <summary>
    /// De frente no se ofrece como objetivo: si no, el ataque automático se quedaría
    /// pegado al escudo y el jugador dejaría de matar al resto.
    /// </summary>
    public override bool IsTargetable => base.IsTargetable && !IsShielding();

    protected override void OnEnable()
    {
        base.OnEnable();
        if (playerTransform != null)
        {
            Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > 0.0001f) facing = toPlayer.normalized;
        }
    }

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            float maxRadians = turnSpeed * Mathf.Deg2Rad * deltaTime;
            facing = Vector3.RotateTowards(facing, toPlayer.normalized, maxRadians, 0f);
        }

        // Camina hacia donde mira: al girar despacio, un rodeo rápido lo deja atrás.
        rb.linearVelocity = facing * moveSpeed;
        FaceDirection(facing);
    }

    public override void OnHit(int damageAmount, bool showBeam)
    {
        // El daño en área siempre entra; el golpe directo de frente rebota.
        if (showBeam && IsShielding())
        {
            if (visualFeedback != null) visualFeedback.TriggerHitFlash();
            PickupManager.Instance?.SpawnRing(rb.position + facing * 0.4f, blockColor, 0.5f);
            return;
        }

        base.OnHit(damageAmount, showBeam);
    }

    private bool IsShielding()
    {
        if (playerTransform == null || rb == null) return false;
        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        return Vector2.Angle(facing, toPlayer) <= shieldHalfAngle;
    }
}
