using UnityEngine;
using MoreMountains.Feedbacks;

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
    [Tooltip("Penalización de tiempo al recibir daño de un enemigo (cuerpo a cuerpo).")]
    public float hitTimePenalty = 5f;

    [Header("Game Feel (FEEL Asset)")]
    [Tooltip("Feedback al asestar un golpe a un enemigo (Hit-stop, partículas, sonido).")]
    public MMF_Player hitEnemyFeedback;
    [Tooltip("Feedback al recibir daño de un enemigo o proyectil (Screen Shake, flash rojo).")]
    public MMF_Player takeDamageFeedback;

    [Header("Gizmos")]
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
            
            // Jugar GameFeel: Hit-Stop, sonido de hit
            if (hitEnemyFeedback != null) hitEnemyFeedback.PlayFeedbacks();
        }
    }

    public void TakeDamageFromEnemy(float customDamage = 0f)
    {
        if (movement.IsInvulnerable) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        float damageToApply = customDamage > 0f ? customDamage : hitTimePenalty;
        TimeManager.Instance.SubtractTime(damageToApply);
        movement.TriggerHitInvulnerability();

        // Jugar GameFeel: Screen Shake, impacto visual fuerte
        if (takeDamageFeedback != null) takeDamageFeedback.PlayFeedbacks();

        Debug.Log($"¡Ouch! Te golpearon. -{damageToApply}s");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = rangeGizmoColor;
        Gizmos.DrawSphere(transform.position, attackRange);

        Gizmos.color = new Color(rangeGizmoColor.r, rangeGizmoColor.g, rangeGizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
