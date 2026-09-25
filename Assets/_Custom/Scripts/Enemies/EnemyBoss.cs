using UnityEngine;

/// <summary>
/// Chrono Warden (engranaje carmesí): el jefe del Overtime. Aparece al subir cada nivel,
/// persigue despacio y dispara anillos de proyectiles, más densos bajo el 50% de vida.
/// Derrotarlo rompe la barrera: sube el tiempo máximo (SpawnManager.NotifyBossDefeated).
/// </summary>
public class EnemyBoss : EnemyBase
{
    [Header("Warden Movement")]
    public float moveSpeed = 1.6f;
    [Tooltip("Velocidad al bajar del 50% de vida.")]
    public float enragedSpeed = 2.2f;
    [Tooltip("Giro constante del engranaje (grados/s).")]
    public float spinSpeed = 40f;

    [Header("Warden Bursts")]
    public float burstInterval = 3.5f;
    public int burstProjectiles = 12;
    public float enragedBurstInterval = 2.4f;
    public int enragedBurstProjectiles = 16;
    [Tooltip("Velocidad de los proyectiles del anillo: más lentos que los del tirador para poder cruzar los huecos.")]
    public float projectileSpeed = 6f;
    [Tooltip("Aviso antes de cada anillo: parpadeo y crecimiento.")]
    public float telegraphDuration = 0.5f;
    public float telegraphScale = 1.15f;
    public Color glowColor = new Color(1f, 0.12f, 0.24f, 0.4f);

    [Header("Warden Defense")]
    [Tooltip("Fracción de su vida que le quita el consumible de limpiar pantalla (no lo mata de golpe).")]
    [Range(0f, 1f)] public float screenClearDamage = 0.25f;

    private float burstTimer;
    private float blinkTimer;
    private float angle;
    private float burstOffset;
    private Vector3 baseScale;

    public bool IsEnraged => HealthFraction <= 0.5f;

    protected override void Awake()
    {
        base.Awake();
        baseScale = transform.localScale;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        burstTimer = burstInterval;
        transform.localScale = baseScale;

        // Reutiliza el halo pulsante del élite, en rojo.
        if (visualFeedback != null)
        {
            visualFeedback.glowColor = glowColor;
            visualFeedback.SetEliteGlow(true);
        }
    }

    protected override void UpdateMovement(float deltaTime)
    {
        angle += spinSpeed * (IsEnraged ? 1.8f : 1f) * deltaTime;
        rb.SetRotation(angle);

        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        Vector2 direction = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.up;
        bool telegraphing = burstTimer <= telegraphDuration;
        rb.linearVelocity = telegraphing ? Vector2.zero : direction * (IsEnraged ? enragedSpeed : moveSpeed);

        burstTimer -= deltaTime;
        if (telegraphing)
        {
            float progress = 1f - Mathf.Clamp01(burstTimer / telegraphDuration);
            transform.localScale = baseScale * Mathf.Lerp(1f, telegraphScale, progress);

            blinkTimer -= deltaTime;
            if (blinkTimer <= 0f)
            {
                blinkTimer = 0.1f;
                if (visualFeedback != null) visualFeedback.TriggerHitFlash();
            }
        }

        if (burstTimer > 0f) return;

        transform.localScale = baseScale;
        Burst();
        burstTimer = IsEnraged ? enragedBurstInterval : burstInterval;
    }

    private void Burst()
    {
        if (SpawnManager.Instance == null) return;

        int count = IsEnraged ? enragedBurstProjectiles : burstProjectiles;
        // Cada anillo gira medio hueco respecto al anterior: quedarse quieto no basta.
        burstOffset += 180f / count;

        for (int i = 0; i < count; i++)
        {
            EnemyProjectile projectile = SpawnManager.Instance.GetProjectile();
            if (projectile == null) return;

            float radians = (burstOffset + i * 360f / count) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            projectile.Launch(rb.position + direction * 0.9f, direction, projectileSpeed);
        }
    }

    public override void KillByConsumable()
    {
        // El jefe es la barrera: el consumible le hace daño, pero no se la salta.
        OnHit(Mathf.Max(1, Mathf.RoundToInt(HealthCap * screenClearDamage)), false);
    }

    protected override void Die(bool giveReward = true, bool isKill = true)
    {
        bool defeated = !IsReleased && isKill;
        Vector3 position = transform.position;

        base.Die(giveReward, isKill);
        if (!defeated) return;

        // Consumible garantizado, como el élite.
        PickupManager.Instance?.RollDrop(position, true);
        SpawnManager.Instance?.NotifyBossDefeated(this, position);
    }
}
