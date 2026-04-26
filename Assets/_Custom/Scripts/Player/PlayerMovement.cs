using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
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

    [Header("Health / Defense Settings")]
    [Tooltip("Tiempo de inmunidad en segundos después de recibir un golpe de un enemigo.")]
    public float hitInvulnerabilityDuration = 1f;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Vector2 lastMoveDirection = Vector2.up;
    
    private bool isDashing;
    private float dashTimeCounter;
    private float dashCooldownCounter;
    private float hitInvulnerabilityCounter; 
    private Vector2 dashDirection;

    public bool IsInvulnerable => isDashing || hitInvulnerabilityCounter > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        playerInput.OnDashPressed += TryStartDash;
    }

    private void OnDisable()
    {
        playerInput.OnDashPressed -= TryStartDash;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        HandleTimers();

        if (!isDashing)
        {
            if (playerInput.MoveInput != Vector2.zero)
            {
                lastMoveDirection = playerInput.MoveInput.normalized;
            }
        }

        RotatePlayer();
    }

    private void RotatePlayer()
    {
        if (lastMoveDirection != Vector2.zero)
        {
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
            rb.linearVelocity = dashDirection * dashSpeed;
        }
        else
        {
            rb.linearVelocity = playerInput.MoveInput.normalized * moveSpeed;
        }
    }

    private void TryStartDash()
    {
        if (dashCooldownCounter <= 0f && !isDashing)
        {
            isDashing = true;
            dashTimeCounter = dashDuration;
            dashCooldownCounter = dashCooldown;

            dashDirection = playerInput.MoveInput != Vector2.zero ? playerInput.MoveInput.normalized : lastMoveDirection;
        }
    }

    private void HandleTimers()
    {
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

    public void TriggerHitInvulnerability()
    {
        hitInvulnerabilityCounter = hitInvulnerabilityDuration;
    }
}
