using UnityEngine;

public class EnemyElite : EnemyBase
{
    [Header("Elite Behavior")]
    [Tooltip("Velocidad de persecución.")]
    public float moveSpeed = 2.5f;

    protected override void Awake()
    {
        base.Awake();
        isElite = true; 
    }

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
