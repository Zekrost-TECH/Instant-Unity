using UnityEngine;

/// <summary>
/// Fantasma (media luna lila): alterna entre visible y en fase. En fase es casi
/// transparente, más rápido, intocable y no hace daño. Pregunta: ¿aguanto su ciclo?
/// </summary>
public class EnemyPhantom : EnemyBase
{
    [Header("Phantom Behavior")]
    public float visibleSpeed = 2.4f;
    public float phasedSpeed = 4.2f;
    public float visibleDuration = 1.8f;
    public float phasedDuration = 1.4f;
    [Range(0f, 1f)] public float phasedAlpha = 0.18f;

    private bool phased;
    private float timer;

    public override bool IsTargetable => base.IsTargetable && !phased;
    protected override bool CanHurtPlayer => !phased;

    protected override void OnEnable()
    {
        base.OnEnable();
        phased = false;
        timer = visibleDuration;
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
        rb.linearVelocity = direction * (phased ? phasedSpeed : visibleSpeed);
        FaceDirection(direction);

        timer -= deltaTime;
        if (timer > 0f) return;

        phased = !phased;
        timer = phased ? phasedDuration : visibleDuration;

        Color color = baseColor;
        color.a = phased ? phasedAlpha : 1f;
        SetTint(color);
        if (!phased && visualFeedback != null) visualFeedback.TriggerHitFlash();
    }

    public override void OnHit(int damageAmount, bool showBeam)
    {
        if (phased) return;
        base.OnHit(damageAmount, showBeam);
    }
}
