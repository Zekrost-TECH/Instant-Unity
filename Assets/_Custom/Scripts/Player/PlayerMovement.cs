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

    [Header("Play Area Boundary")]
    [Tooltip("Si es true, la zona se adaptará automáticamente a los bordes de la cámara en pantalla.")]
    public bool bindToCameraViewport = true;
    [Tooltip("Margen de seguridad para que el jugador no asome fuera de la pantalla.")]
    public float screenBorderPadding = 0.6f;
    [Tooltip("El ancho total de la zona jugable rectangular (si bindToCameraViewport es false).")]
    public float playAreaWidth = 28f;
    [Tooltip("El alto total de la zona jugable rectangular (si bindToCameraViewport es false).")]
    public float playAreaHeight = 16f;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Vector2 lastMoveDirection = Vector2.up;
    
    private bool isDashing;
    private float dashTimeCounter;
    private float dashCooldownCounter;
    private float hitInvulnerabilityCounter; 
    private Vector2 dashDirection;
    private Camera mainCamera;

    public bool IsInvulnerable => isDashing || hitInvulnerabilityCounter > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        mainCamera = Camera.main;
        
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

        // Aplicar límite de zona jugable rectangular (dinámica con la cámara o fija)
        Vector2 nextPosition = rb.position + rb.linearVelocity * Time.fixedDeltaTime;
        float halfWidth, halfHeight;
        Vector2 centerPoint = Vector2.zero;

        if (bindToCameraViewport && mainCamera != null)
        {
            halfHeight = mainCamera.orthographicSize - screenBorderPadding;
            halfWidth = (mainCamera.orthographicSize * mainCamera.aspect) - screenBorderPadding;
            centerPoint = mainCamera.transform.position;
        }
        else
        {
            halfWidth = playAreaWidth / 2f;
            halfHeight = playAreaHeight / 2f;
        }

        // Clampar posición física en los ejes X y Y de forma independiente
        float clampedX = Mathf.Clamp(nextPosition.x, centerPoint.x - halfWidth, centerPoint.x + halfWidth);
        float clampedY = Mathf.Clamp(nextPosition.y, centerPoint.y - halfHeight, centerPoint.y + halfHeight);
        rb.position = new Vector2(clampedX, clampedY);

        // Cancelar velocidad exterior en los bordes para permitir deslizamiento suave en las esquinas y lados
        Vector2 currentVelocity = rb.linearVelocity;
        if (nextPosition.x > centerPoint.x + halfWidth && currentVelocity.x > 0f) currentVelocity.x = 0f;
        if (nextPosition.x < centerPoint.x - halfWidth && currentVelocity.x < 0f) currentVelocity.x = 0f;
        if (nextPosition.y > centerPoint.y + halfHeight && currentVelocity.y > 0f) currentVelocity.y = 0f;
        if (nextPosition.y < centerPoint.y - halfHeight && currentVelocity.y < 0f) currentVelocity.y = 0f;
        rb.linearVelocity = currentVelocity;
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Camera cam = mainCamera != null ? mainCamera : Camera.main;

        if (bindToCameraViewport && cam != null)
        {
            float halfHeight = cam.orthographicSize - screenBorderPadding;
            float halfWidth = (cam.orthographicSize * cam.aspect) - screenBorderPadding;
            Vector3 camPos = cam.transform.position;
            camPos.z = 0f;
            Gizmos.DrawWireCube(camPos, new Vector3(halfWidth * 2f, halfHeight * 2f, 0f));
        }
        else
        {
            // Dibujar un cubo de alambre representando el límite de la zona jugable rectangular fija
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(playAreaWidth, playAreaHeight, 0f));
        }
    }
}
