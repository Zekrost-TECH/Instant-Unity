using UnityEngine;

/// <summary>
/// Sanador (cruz verde): se queda detrás del grupo y cada pocos segundos cura a los
/// enemigos cercanos (tanques, escuderos, el élite). Pregunta: ¿lo priorizo?
/// </summary>
public class EnemyHealer : EnemyBase
{
    [Header("Healer Behavior")]
    public float moveSpeed = 2f;
    public float preferredDistance = 6f;
    public float healInterval = 3f;
    public float healRadius = 3.5f;
    public int healAmount = 1;
    public Color healColor = new Color(0.24f, 1f, 0.48f, 1f);

    private float healTimer;

    protected override void OnEnable()
    {
        base.OnEnable();
        healTimer = healInterval;
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
        else rb.linearVelocity = Vector2.zero;
        FaceDirection(direction);

        healTimer -= deltaTime;
        if (healTimer <= 0f)
        {
            healTimer = healInterval;
            HealAround();
        }
    }

    private void HealAround()
    {
        if (EnemyManager.Instance == null) return;

        float radiusSqr = healRadius * healRadius;
        var enemies = EnemyManager.Instance.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null || enemy == this) continue;
            if (((Vector2)enemy.transform.position - rb.position).sqrMagnitude > radiusSqr) continue;

            if (enemy.Heal(healAmount))
                PickupManager.Instance?.SpawnRing(enemy.transform.position, healColor, 0.45f);
        }

        // Translúcido: marca el radio de curación sin tapar la arena cuando hay varios.
        PickupManager.Instance?.SpawnRing(rb.position, new Color(healColor.r, healColor.g, healColor.b, 0.3f), healRadius);
    }
}
