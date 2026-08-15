using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Daño que aplica el ataque automático al enemigo más cercano.")]
    public int attackDamage = 1;
    [Tooltip("Tiempo en segundos entre cada ataque automático.")]
    public float attackRate = 0.75f;
    [Tooltip("Radio máximo en el que el jugador detecta enemigos.")]
    public float attackRange = 3f;
    [Tooltip("Penalización de tiempo al recibir daño de un enemigo (cuerpo a cuerpo).")]
    public float hitTimePenalty = 6f;
    public int DamageTakenCount { get; private set; }

    [Header("Game Feel (FEEL Asset)")]
    [Tooltip("Feedback al asestar un golpe a un enemigo (Hit-stop, partículas, sonido).")]
    public MMF_Player hitEnemyFeedback;
    [Tooltip("Feedback al recibir daño de un enemigo o proyectil (Screen Shake, flash rojo).")]
    public MMF_Player takeDamageFeedback;

    [Header("Gizmos")]
    public Color rangeGizmoColor = new Color(1f, 0.4f, 0f, 0.35f);

    [Header("Visual Range Indicator")]
    [Tooltip("Transform del objeto hijo con un SpriteRenderer circular que servirá de preview in-game.")]
    public Transform rangeVisual;

    private PlayerMovement movement;
    private SpriteRenderer rangeSpriteRenderer;
    private float attackTimer = 0f;
    private float lastRange;

    private int baseAttackDamage;
    private float baseAttackRate;
    private float baseAttackRange;

    // Buffs temporales de consumibles
    private float attackRateMultiplier = 1f;
    private float attackSpeedTimer;
    private bool tripleShotActive;
    private float tripleShotTimer;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();

        // Igual que en PlayerMovement: capturar en Awake, no en Start. El arranque de
        // partida desde sceneLoaded llama a ResetState() antes de Start, y con los bases
        // sin inicializar dejaba attackDamage/Rate/Range a 0 (jugador incapaz de matar).
        baseAttackDamage = attackDamage;
        baseAttackRate = attackRate;
        baseAttackRange = attackRange;
        lastRange = attackRange;
    }

    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            attackRange *= (1f + SaveManager.Instance.AttackRangeLevel * 0.07f);
            baseAttackRange = attackRange;   // el upgrade permanente entra en el valor de reset
        }
        lastRange = attackRange;

        UpdateRangeVisual();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (attackSpeedTimer > 0f)
        {
            attackSpeedTimer -= Time.deltaTime;
            if (attackSpeedTimer <= 0f) attackRateMultiplier = 1f;
        }

        if (tripleShotTimer > 0f)
        {
            tripleShotTimer -= Time.deltaTime;
            if (tripleShotTimer <= 0f) tripleShotActive = false;
        }

        // Si el rango cambia (por ejemplo, mediante un Upgrade), actualizamos el visual dinámicamente
        if (lastRange != attackRange)
        {
            UpdateRangeVisual();
            lastRange = attackRange;
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            attackTimer = attackRate * attackRateMultiplier;
            TryAttack();
        }
    }

    private void UpdateRangeVisual()
    {
        if (rangeVisual != null)
        {
            if (rangeSpriteRenderer == null) rangeSpriteRenderer = rangeVisual.GetComponent<SpriteRenderer>();

            SpriteRenderer spriteRenderer = rangeSpriteRenderer;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                // Obtenemos el tamaño real nativo en unidades de mundo del Sprite (ancho en píxeles / Pixels Per Unit)
                float spriteWorldWidth = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
                
                if (spriteWorldWidth <= 0f) spriteWorldWidth = 1f;

                // El diámetro real que queremos en el mundo es (attackRange * 2)
                // Dividimos ese diámetro deseado entre el ancho nativo del sprite para obtener la escala exacta en Unity
                float targetScale = (attackRange * 2f) / spriteWorldWidth;

                rangeVisual.localScale = new Vector3(targetScale, targetScale, 1f);
            }
            else
            {
                // Fallback por defecto si no hay un Sprite asignado todavía
                float targetScale = attackRange * 2f;
                rangeVisual.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }
    }

    private void TryAttack()
    {
        if (EnemyManager.Instance == null) return;

        if (tripleShotActive)
        {
            // Triple disparo: golpea hasta 3 enemigos a la vez
            List<EnemyBase> targets = EnemyManager.Instance.GetNearestEnemies(transform.position, attackRange, 3);
            bool hitAny = false;
            for (int i = 0; i < targets.Count; i++)
            {
                EnemyBase targetEnemy = targets[i];
                if (targetEnemy == null) continue;

                Vector3 hitPosition = targetEnemy.transform.position;
                targetEnemy.OnHit(attackDamage);
                DamageNumbersManager.Instance?.SpawnEnemyDamage(hitPosition, attackDamage);
                hitAny = true;
            }

            if (hitAny && hitEnemyFeedback != null) hitEnemyFeedback.PlayFeedbacks();
            return;
        }

        EnemyBase target = EnemyManager.Instance.GetNearestEnemy(transform.position, attackRange);
        if (target != null)
        {
            Vector3 hitPosition = target.transform.position;
            target.OnHit(attackDamage);
            DamageNumbersManager.Instance?.SpawnEnemyDamage(hitPosition, attackDamage);

            // Jugar GameFeel: Hit-Stop, sonido de hit
            if (hitEnemyFeedback != null) hitEnemyFeedback.PlayFeedbacks();
        }
    }

    public bool TakeDamageFromEnemy(float customDamage = 0f)
    {
        if (movement.IsInvulnerable) return false;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return false;

        float damageToApply = customDamage > 0f ? customDamage : hitTimePenalty;
        float timeLost = TimeManager.Instance != null
            ? TimeManager.Instance.SubtractTime(damageToApply)
            : damageToApply;
        DamageNumbersManager.Instance?.SpawnPlayerDamage(transform.position, timeLost);
        DamageTakenCount++;
        movement.TriggerHitInvulnerability();

        // Jugar GameFeel: Screen Shake, impacto visual fuerte
        if (takeDamageFeedback != null) takeDamageFeedback.PlayFeedbacks();

        HapticManager.Instance?.TriggerDamage();

        //Debug.Log($"¡Ouch! Te golpearon. -{damageToApply}s");
        return true;
    }

    public void ApplyAttackSpeedBoost(float multiplier, float duration)
    {
        attackRateMultiplier = multiplier;
        attackSpeedTimer = duration;
    }

    public void ApplyTripleShot(float duration)
    {
        tripleShotActive = true;
        tripleShotTimer = Mathf.Max(tripleShotTimer, duration);
    }

    public void ResetState()
    {
        attackDamage = baseAttackDamage;
        attackRate = baseAttackRate;
        attackRange = baseAttackRange;
        attackRateMultiplier = 1f;
        attackSpeedTimer = 0f;
        tripleShotActive = false;
        tripleShotTimer = 0f;
        DamageTakenCount = 0;
        lastRange = attackRange;
        attackTimer = 0f;
        UpdateRangeVisual();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = rangeGizmoColor;
        Gizmos.DrawSphere(transform.position, attackRange);

        Gizmos.color = new Color(rangeGizmoColor.r, rangeGizmoColor.g, rangeGizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
