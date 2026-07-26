using UnityEngine;

public class EnemyFodder : EnemyBase
{
    [Header("Fodder Behavior")]
    [Tooltip("Velocidad de persecución en línea recta hacia el jugador.")]
    public float moveSpeed = 1.6f;

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
