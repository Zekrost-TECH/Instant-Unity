using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Daño que aplica el ataque automático al enemigo más cercano.")]
    public int attackDamage = 1;
    [Tooltip("Tiempo en segundos entre cada ataque automático.")]
    public float attackRate = 0.6f;
    [Tooltip("Radio máximo en el que el jugador detecta enemigos.")]
    public float attackRange = 3f;
    [Tooltip("Penalización de tiempo al recibir daño de un enemigo.")]
    public float hitTimePenalty = 5f;

    [Header("Gizmos")]
    [Tooltip("Color del círculo de rango de ataque en el Editor.")]
    public Color rangeGizmoColor = new Color(1f, 0.4f, 0f, 0.35f);

    private PlayerMovement movement;
    private float attackTimer = 0f;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            attackTimer = attackRate;
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (EnemyManager.Instance == null) return;

        EnemyBase target = EnemyManager.Instance.GetNearestEnemy(transform.position, attackRange);
        if (target != null)
        {
            target.OnHit(attackDamage);
        }
    }

    /// <summary>
    /// Llamado por EnemyBase cuando un enemigo toca al jugador.
    /// Aplica la penalización de tiempo y activa el periodo de invulnerabilidad.
    /// </summary>
    public void TakeDamageFromEnemy()
    {
        if (movement.IsInvulnerable) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        TimeManager.Instance.SubtractTime(hitTimePenalty);
        movement.TriggerHitInvulnerability();

        Debug.Log($"¡Ouch! El enemigo te golpeó. -{hitTimePenalty}s");
    }

    // ── Gizmos ──────────────────────────────────────────────────────────────

    // Se dibuja siempre visible en la Scene View (no solo al seleccionar).
    private void OnDrawGizmos()
    {
        Gizmos.color = rangeGizmoColor;
        Gizmos.DrawSphere(transform.position, attackRange);

        // Borde sólido para definir con precisión el límite del rango
        Gizmos.color = new Color(rangeGizmoColor.r, rangeGizmoColor.g, rangeGizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
