using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento en unidades por segundo.")]
    public float moveSpeed = 5f;
    
    [Header("Dash Settings")]
    [Tooltip("Velocidad o fuerza del dash.")]
    public float dashSpeed = 15f;
    [Tooltip("Duración del dash en segundos.")]
    public float dashDuration = 0.2f;
    [Tooltip("Tiempo de enfriamiento (cooldown) entre dashes.")]
    public float dashCooldown = 1f;

    [Header("Dash Combat Settings")]
    [Tooltip("Cantidad de daño que aplica el dash al traspasar enemigos.")]
    public int dashDamage = 1;
    [Tooltip("Radio del área de impacto del jugador durante el dash.")]
    public float dashHitboxRadius = 0.6f;
    [Tooltip("Capa (Layer) de los enemigos.")]
    public LayerMask enemyLayers;

    [Header("Health / Defense Settings")]
    [Tooltip("Tiempo de inmunidad en segundos después de recibir un golpe de un enemigo.")]
    public float hitInvulnerabilityDuration = 1f;

    [Header("Input References")]
    [Tooltip("Referencia a la acción de movimiento (tipo Value - Vector2)")]
    public InputActionReference moveAction;
    [Tooltip("Referencia a la acción de Dash (tipo Button)")]
    public InputActionReference dashAction;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Vector2 lastMoveDirection = Vector2.up; // Dirección hacia donde mirar por defecto
    
    private bool isDashing;
    private float dashTimeCounter;
    private float dashCooldownCounter;
    private float hitInvulnerabilityCounter; // Contador de I-frames post golpe
    private Vector2 dashDirection;
    private HashSet<IDamageable> damagedEnemiesDuringDash = new HashSet<IDamageable>();

    // Determina si el jugador no puede recibir daño (por dash o por haber sido golpeado recién)
    public bool IsInvulnerable => isDashing || hitInvulnerabilityCounter > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Ajustes por defecto de físicas 2D (Top-down)
        rb.gravityScale = 0f;
        
        // rb.freezeRotation usualmente se prefiere activado para juegos top-down. 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        // Activar y suscribirse a los eventos del Nuevo Input System
        if (moveAction != null) moveAction.action.Enable();
        if (dashAction != null)
        {
            dashAction.action.Enable();
            dashAction.action.performed += OnDashPerformed;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (dashAction != null)
        {
            dashAction.action.Disable();
            dashAction.action.performed -= OnDashPerformed;
        }
    }

    private void Update()
    {
        // Detener lógica si no estamos en estado de "Juego"
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        HandleTimers();

        // Si no estamos haciendo el dash, recogemos inputs regulares
        if (!isDashing && moveAction != null)
        {
            // Vector2 leído desde el WASD o Joystick (normalizado para la misma velocidad diagonal)
            movementInput = moveAction.action.ReadValue<Vector2>().normalized;
            
            // Guardamos la última dirección si nos estamos moviendo (para saber hacia dónde dashear si estamos quietos)
            if (movementInput != Vector2.zero)
            {
                lastMoveDirection = movementInput;
            }
        }

        RotatePlayer();
    }

    private void RotatePlayer()
    {
        if (lastMoveDirection != Vector2.zero)
        {
            // Hace que el eje Y local (la "punta" de arriba) apunte hacia donde nos movemos.
            // Al guardar el lastMoveDirection, mantiene su rotación incluso en diagonal cuando nos detenemos.
            transform.up = lastMoveDirection;
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;  // Aplicar velocidad de dash
            DamageEnemiesInDash(); // Golpea a los enemigos al traspasarlos
        }
        else
        {
            rb.linearVelocity = movementInput * moveSpeed;  // Aplicar movimiento normal
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Validar si el cooldown terminó y no estamos ya en medio de un dash
        if (dashCooldownCounter <= 0f && !isDashing)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimeCounter = dashDuration;
        dashCooldownCounter = dashCooldown;
        damagedEnemiesDuringDash.Clear(); // Reinicia la lista de enemigos golpeados

        // La dirección del dash dependerá si nos movemos; de lo contrario usará la última dirección en que nos movimos.
        dashDirection = movementInput != Vector2.zero ? movementInput : lastMoveDirection;

        // NOTA GDD: Más adelante se implementarán I-Frames (inmunidad) conectándolo al script de vida (Vida/Daño).
        Debug.Log("¡Dash Iniciado! Cooldown activado.");
    }

    private void HandleTimers()
    {
        // Contador de invulnerabilidad post-daño
        if (hitInvulnerabilityCounter > 0f)
        {
            hitInvulnerabilityCounter -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimeCounter -= Time.deltaTime;
            if (dashTimeCounter <= 0f)
            {
                isDashing = false;
            }
        }

        if (dashCooldownCounter > 0f)
        {
            dashCooldownCounter -= Time.deltaTime;
        }
    }

    private void DamageEnemiesInDash()
    {
        // Detecta todo lo que este dentro del radio de daño del Dash
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, dashHitboxRadius, enemyLayers);
        
        foreach (Collider2D enemy in hitEnemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            
            // Aplicamos daño SOLO si tiene IDamageable y si NO lo hemos dañado en este mismo uso del Dash.
            if (damageable != null && !damagedEnemiesDuringDash.Contains(damageable))
            {
                damageable.TakeDamage(dashDamage);
                damagedEnemiesDuringDash.Add(damageable);
                Debug.Log("¡Atravesaste a un enemigo con el Dash! Tiempo robado (Daño aplicado).");
            }
        }
    }

    // Dibujar el área de impacto del Dash en el Editor para configurarlo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dashHitboxRadius);
    }

    public void TakeDamageFromEnemy(float timePenalty)
    {
        // Ignoramos el daño si estamos en medio de un Dash o en periodo de recuperación
        if (IsInvulnerable || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        TimeManager.Instance.SubtractTime(timePenalty);
        hitInvulnerabilityCounter = hitInvulnerabilityDuration;
        
        Debug.Log($"¡Ouch! El enemigo te golpeó. Perdiste {timePenalty} segundos.");
    }
}
