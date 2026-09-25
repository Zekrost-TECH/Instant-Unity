using UnityEngine;

/// <summary>
/// Invocador (hexagrama magenta): se queda lejos y cada pocos segundos invoca esbirros.
/// Pregunta: ¿voy a por él entre la multitud antes de que llene la arena?
/// </summary>
public class EnemySummoner : EnemyBase
{
    [Header("Summoner Behavior")]
    public float moveSpeed = 1.8f;
    public float preferredDistance = 7f;
    public float summonInterval = 4.5f;
    public int minionsPerSummon = 2;
    [Tooltip("Color de los esbirros y del anillo de invocación.")]
    public Color summonColor = new Color(1f, 0.55f, 0.98f, 1f);

    private float summonTimer;

    protected override void OnEnable()
    {
        base.OnEnable();
        summonTimer = summonInterval * 0.6f;
    }

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.001f ? toPlayer / distance : Vector2.up;

        if (distance > preferredDistance + 1f) rb.linearVelocity = direction * moveSpeed;
        else if (distance < preferredDistance - 1f) rb.linearVelocity = -direction * moveSpeed;
        else rb.linearVelocity = new Vector2(-direction.y, direction.x) * (moveSpeed * 0.5f);
        FaceDirection(direction);

        summonTimer -= deltaTime;
        if (summonTimer <= 0f)
        {
            summonTimer = summonInterval;
            Summon();
        }
    }

    private void Summon()
    {
        if (SpawnManager.Instance == null) return;

        for (int i = 0; i < minionsPerSummon; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * 0.9f;
            EnemyBase minion = SpawnManager.Instance.SpawnMinion(rb.position + offset);
            if (minion != null) minion.SetTint(summonColor);
        }

        PickupManager.Instance?.SpawnRing(rb.position, new Color(summonColor.r, summonColor.g, summonColor.b, 0.5f), 1.2f);
        if (visualFeedback != null) visualFeedback.TriggerHitFlash();
    }
}
