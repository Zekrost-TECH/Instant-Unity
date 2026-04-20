using UnityEngine;

public class EnemyFodder : EnemyBase
{
    [Header("Fodder Behavior")]
    [Tooltip("Velocidad de persecución en línea recta hacia el jugador.")]
    public float moveSpeed = 3f;

    private void FixedUpdate()
    {
        // Detener lógica si el juego terminó o pausó
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Si tenemos la posición del jugador, avanzamos hacia allí
        if (playerTransform != null)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Haz que gire mirando al jugador (similar al Sprite del héroe)
            if (direction != Vector2.zero)
            {
                transform.up = direction; 
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
