using UnityEngine;

public class EnemyTank : EnemyBase
{
    [Header("Tank Behavior")]
    [Tooltip("Velocidad de persecución (más lenta que el Fodder).")]
    public float moveSpeed = 0.8f;

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)playerTransform.position - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
        FaceDirection(direction);
    }
}
