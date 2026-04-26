using UnityEngine;

public class EnemyFodder : EnemyBase
{
    [Header("Fodder Behavior")]
    [Tooltip("Velocidad de persecución en línea recta hacia el jugador.")]
    public float moveSpeed = 3f;

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
