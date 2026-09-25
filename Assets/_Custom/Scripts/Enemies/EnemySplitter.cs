using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Divisor (pentágono morado): al morir se parte en dos copias pequeñas y rápidas que
/// ya no se dividen. Pregunta: ¿dónde lo mato para que las copias no me rodeen?
/// </summary>
public class EnemySplitter : EnemyBase
{
    [Header("Splitter Behavior")]
    public float moveSpeed = 2.2f;
    [Tooltip("Copias que suelta al morir.")]
    public int splitCount = 2;
    [Tooltip("Escala de las copias respecto al original.")]
    public float splitScale = 0.6f;
    public float splitSpeed = 3.6f;
    public float splitReward = 0.5f;

    private bool isSplitling;
    private Vector3 baseScale;
    private float baseReward;

    protected override void Awake()
    {
        base.Awake();
        baseScale = transform.localScale;
        baseReward = timeRewardOnDeath;
    }

    protected override void OnEnable()
    {
        // Sale del pool como divisor completo; MakeSplitling lo convierte en copia.
        isSplitling = false;
        transform.localScale = baseScale;
        timeRewardOnDeath = baseReward;
        base.OnEnable();
    }

    private void MakeSplitling()
    {
        isSplitling = true;
        transform.localScale = baseScale * splitScale;
        timeRewardOnDeath = splitReward;
        ResetHealth(1);
    }

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)playerTransform.position - rb.position).normalized;
        rb.linearVelocity = direction * (isSplitling ? splitSpeed : moveSpeed);
        FaceDirection(direction);
    }

    protected override void Die(bool giveReward = true, bool isKill = true)
    {
        // Sólo se divide en bajas reales: el consumible de limpieza o el reciclaje no.
        bool split = !isSplitling && giveReward && isKill && !IsReleased;
        Vector2 position = rb.position;
        Vector2 away = playerTransform != null ? (position - (Vector2)playerTransform.position).normalized : Vector2.up;
        ObjectPool<EnemyBase> pool = OwnerPool;

        base.Die(giveReward, isKill);
        if (!split || pool == null) return;

        Vector2 side = new Vector2(-away.y, away.x);
        for (int i = 0; i < splitCount; i++)
        {
            float spread = splitCount > 1 ? i / (float)(splitCount - 1) * 2f - 1f : 0f;
            EnemySplitter child = pool.Get() as EnemySplitter;
            if (child == null) continue;

            child.PlaceAt(position + side * (0.45f * spread) + away * 0.2f);
            child.MakeSplitling();
        }
    }
}
