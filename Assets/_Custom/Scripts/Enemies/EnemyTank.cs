using UnityEngine;

public class EnemyTank : EnemyBase
{
    [Header("Tank Behavior")]
    [Tooltip("Velocidad de persecución (más lenta que el Fodder).")]
    public float moveSpeed = 1.5f;

    protected override void UpdateMovement()
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        if (direction != Vector2.zero)
        {
            transform.up = direction;
        }
    }
}
