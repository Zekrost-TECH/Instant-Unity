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

    private float baseMoveSpeed;
    private float baseDashCooldown;

    public bool IsInvulnerable => isDashing || hitInvulnerabilityCounter > 0f;
    public bool IsDashing => isDashing;
    public float DashCooldownRemaining => dashCooldownCounter;
    public float DashCooldownTotal => dashCooldown;
    public float DashCooldownRatio => dashCooldown > 0f ? Mathf.Clamp01(1f - (dashCooldownCounter / dashCooldown)) : 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        mainCamera = Camera.main;
        
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            dashCooldown *= (1f - SaveManager.Instance.DashCooldownLevel * 0.08f);
            ApplyEquippedSkin();
        }

        baseMoveSpeed = moveSpeed;
        baseDashCooldown = dashCooldown;
    }

    private void ApplyEquippedSkin()
    {
        SkinRenderer skinRenderer = GetComponent<SkinRenderer>();
        if (skinRenderer != null) skinRenderer.ApplySkin();

        // El color sale del catálogo de SkinManager, no de un switch duplicado aquí:
        // añadir una skin nueva es un solo cambio, en el catálogo.
        SkinManager manager = SkinManager.Ensure();
        if (manager == null) return;

        SkinDefinition equipped = manager.GetEquippedSkinDefinition();
        if (equipped == null) return;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            if (equipped.icon != null) sr.sprite = equipped.icon;
            sr.color = equipped.color;
        }
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

        ClampToPlayArea();
    }

    private void ClampToPlayArea()
    {
        float halfWidth, halfHeight;
        Vector2 centerPoint = Vector2.zero;

        if (bindToCameraViewport)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            halfHeight = mainCamera.orthographicSize - screenBorderPadding;
            halfWidth = (mainCamera.orthographicSize * mainCamera.aspect) - screenBorderPadding;
            centerPoint = mainCamera.transform.position;
        }
        else
        {
            halfWidth = playAreaWidth / 2f;
            halfHeight = playAreaHeight / 2f;
        }

        float minX = centerPoint.x - halfWidth;
        float maxX = centerPoint.x + halfWidth;
        float minY = centerPoint.y - halfHeight;
        float maxY = centerPoint.y + halfHeight;

        // Se predice el paso de física para saber si el jugador cruzaría el borde.
        // Sólo se corrige la posición del eje infractor: escribir rb.position con la
        // posición predicha en cada FixedUpdate hacía que el motor volviera a integrar
        // la velocidad encima, moviendo al jugador al doble de moveSpeed.
        Vector2 position = rb.position;
        Vector2 velocity = rb.linearVelocity;
        Vector2 predicted = position + velocity * Time.fixedDeltaTime;
        bool corrected = false;

        if (predicted.x > maxX)
        {
            position.x = maxX;
            if (velocity.x > 0f) velocity.x = 0f;
            corrected = true;
        }
        else if (predicted.x < minX)
        {
            position.x = minX;
            if (velocity.x < 0f) velocity.x = 0f;
            corrected = true;
        }

        if (predicted.y > maxY)
        {
            position.y = maxY;
            if (velocity.y > 0f) velocity.y = 0f;
            corrected = true;
        }
        else if (predicted.y < minY)
        {
            position.y = minY;
            if (velocity.y < 0f) velocity.y = 0f;
            corrected = true;
        }

        if (!corrected) return;

        rb.position = position;
        rb.linearVelocity = velocity;
    }

    private void TryStartDash()
    {
        if (dashCooldownCounter <= 0f && !isDashing)
        {
            isDashing = true;
            dashTimeCounter = dashDuration;
            dashCooldownCounter = dashCooldown;

            dashDirection = playerInput.MoveInput != Vector2.zero ? playerInput.MoveInput.normalized : lastMoveDirection;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDashSFX, 0.8f);

            if (ParticleManager.Instance != null)
                ParticleManager.Instance.SpawnDashTrail(transform.position, dashDirection);
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

    public void ResetState()
    {
        moveSpeed = baseMoveSpeed;
        dashCooldown = baseDashCooldown;

        transform.position = Vector3.zero;
        lastMoveDirection = Vector2.up;
        transform.up = lastMoveDirection;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        isDashing = false;
        dashTimeCounter = 0f;
        dashCooldownCounter = 0f;
        hitInvulnerabilityCounter = 0f;
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
