using UnityEngine;

public class EnemyElite : EnemyBase
{
    private enum Phase { Reposition, Telegraph, Fire }

    [Header("Elite Behavior")]
    [Tooltip("Velocidad de desplazamiento.")]
    public float moveSpeed = 1.4f;
    [Tooltip("Distancia a la que intenta mantenerse del jugador.")]
    public float preferredDistance = 6.5f;
    [Tooltip("Margen alrededor de preferredDistance en el que se queda quieto.")]
    public float distanceTolerance = 1.2f;

    [Header("Láser")]
    [Tooltip("Segundos de reloj que le quita al jugador si el láser le alcanza.")]
    public float laserDamage = 12f;
    [Tooltip("Espera entre disparos.")]
    public float laserCooldown = 5f;
    [Tooltip("Espera antes del primer disparo tras aparecer.")]
    public float firstShotDelay = 2.5f;
    [Tooltip("Duración total del aviso previo.")]
    public float telegraphDuration = 1.2f;
    [Tooltip("Fracción final del aviso en la que la dirección queda FIJA. Esa es la ventana real de esquiva.")]
    [Range(0.1f, 0.9f)] public float lockedFraction = 0.45f;
    [Tooltip("Cuánto permanece visible el rayo ya disparado.")]
    public float beamDuration = 0.3f;
    public float beamLength = 30f;
    [Tooltip("Medio ancho del rayo para decidir el impacto.")]
    public float beamHalfWidth = 0.4f;
    [Tooltip("Sólo dispara si el jugador está a menos de esta distancia.")]
    public float maxFiringRange = 14f;

    [Header("Láser · Visual")]
    [Tooltip("Material del LineRenderer. Reutiliza Assets/_Custom/Materials/HitBeam.mat.")]
    public Material laserMaterial;
    public Color trackingColor = new Color(1f, 0.85f, 0.2f, 0.35f);
    public Color lockedColor = new Color(1f, 0.35f, 0.1f, 0.9f);
    public Color beamColor = new Color(1f, 0.2f, 0.15f, 1f);
    public float telegraphWidth = 0.07f;
    public float lockedWidth = 0.14f;
    public string laserSortingLayer = "Default";
    public int laserSortingOrder = 90;

    private Phase phase;
    private float phaseTimer;
    private float cooldownTimer;
    private Vector2 aimDirection = Vector2.up;
    private bool shotResolved;

    private LineRenderer laser;
    private PlayerCombat cachedPlayerCombat;

    protected override void Awake()
    {
        base.Awake();
        isElite = true;
        EnsureLaser();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // El élite se reutiliza desde el pool: hay que rearmar la secuencia entera.
        phase = Phase.Reposition;
        cooldownTimer = firstShotDelay;
        shotResolved = false;
        aimDirection = Vector2.up;
        HideLaser();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        HideLaser();
    }

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            HideLaser();
            return;
        }

        switch (phase)
        {
            case Phase.Reposition: TickReposition(deltaTime); break;
            case Phase.Telegraph: TickTelegraph(deltaTime); break;
            case Phase.Fire: TickFire(deltaTime); break;
        }
    }

    // ── Fases ────────────────────────────────────────────────────────────────

    private void TickReposition(float deltaTime)
    {
        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.001f ? toPlayer / distance : Vector2.up;

        // Mantiene la distancia: se acerca si está lejos, retrocede si está encima.
        if (distance > preferredDistance + distanceTolerance)
            rb.linearVelocity = direction * moveSpeed;
        else if (distance < preferredDistance - distanceTolerance)
            rb.linearVelocity = -direction * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;

        FaceDirection(direction);

        cooldownTimer -= deltaTime;
        if (cooldownTimer <= 0f && distance <= maxFiringRange)
        {
            phase = Phase.Telegraph;
            phaseTimer = telegraphDuration;
            aimDirection = direction;
            shotResolved = false;
        }
    }

    private void TickTelegraph(float deltaTime)
    {
        // Se planta para disparar: parado es mucho más legible que apuntando en movimiento.
        rb.linearVelocity = Vector2.zero;
        phaseTimer -= deltaTime;

        float remainingFraction = telegraphDuration > 0f ? phaseTimer / telegraphDuration : 0f;
        bool locked = remainingFraction <= lockedFraction;

        if (!locked)
        {
            // Todavía sigue al jugador. Al bloquearse deja de corregir: ahí se esquiva.
            Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
            if (toPlayer.sqrMagnitude > 0.000001f) aimDirection = toPlayer.normalized;
        }

        FaceDirection(aimDirection);
        DrawLaser(locked ? lockedColor : trackingColor, locked ? lockedWidth : telegraphWidth);

        if (phaseTimer <= 0f)
        {
            phase = Phase.Fire;
            phaseTimer = beamDuration;
        }
    }

    private void TickFire(float deltaTime)
    {
        rb.linearVelocity = Vector2.zero;

        // El daño se resuelve una sola vez, en el primer frame del disparo.
        if (!shotResolved)
        {
            shotResolved = true;
            ResolveLaserHit();
        }

        phaseTimer -= deltaTime;

        float t = beamDuration > 0f ? Mathf.Clamp01(phaseTimer / beamDuration) : 0f;
        Color fading = beamColor;
        fading.a *= t;
        DrawLaser(fading, Mathf.Lerp(0f, beamHalfWidth * 2f, t));

        if (phaseTimer <= 0f)
        {
            HideLaser();
            cooldownTimer = laserCooldown;
            phase = Phase.Reposition;
        }
    }

    // ── Impacto ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Comprueba si el jugador está dentro del rayo. Es una distancia punto-segmento,
    /// no un raycast: el rayo no debe frenarse con enemigos por medio.
    /// </summary>
    private void ResolveLaserHit()
    {
        Vector2 origin = rb.position;
        Vector2 toPlayer = (Vector2)playerTransform.position - origin;

        float along = Vector2.Dot(toPlayer, aimDirection);
        if (along < 0f || along > beamLength) return;

        // aimDirection está normalizado, así que el módulo del producto cruzado
        // es directamente la distancia perpendicular al eje del rayo.
        float perpendicular = Mathf.Abs(toPlayer.x * aimDirection.y - toPlayer.y * aimDirection.x);
        if (perpendicular > beamHalfWidth) return;

        if (cachedPlayerCombat == null)
            cachedPlayerCombat = playerTransform.GetComponent<PlayerCombat>();

        // Devuelve false si el jugador es invulnerable: el dash también esquiva.
        if (cachedPlayerCombat != null)
            cachedPlayerCombat.TakeDamageFromEnemy(laserDamage);
    }

    // ── Visual ───────────────────────────────────────────────────────────────

    private void EnsureLaser()
    {
        if (laser != null) return;

        GameObject go = new GameObject("EliteLaser");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        laser = go.AddComponent<LineRenderer>();
        laser.useWorldSpace = true;   // así la rotación del élite no arrastra el rayo
        laser.positionCount = 2;
        laser.numCapVertices = 2;
        laser.textureMode = LineTextureMode.Stretch;
        laser.alignment = LineAlignment.View;
        laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laser.receiveShadows = false;

        // sharedMaterial: material_ instancia una copia por élite y se filtra.
        if (laserMaterial != null) laser.sharedMaterial = laserMaterial;

        laser.sortingLayerName = laserSortingLayer;
        laser.sortingOrder = laserSortingOrder;
        laser.enabled = false;
    }

    private void DrawLaser(Color color, float width)
    {
        if (laser == null) return;

        Vector3 origin = rb.position;
        laser.SetPosition(0, origin);
        laser.SetPosition(1, origin + (Vector3)(aimDirection * beamLength));

        laser.startColor = color;
        laser.endColor = color;
        laser.widthMultiplier = width;
        laser.enabled = true;
    }

    private void HideLaser()
    {
        if (laser != null) laser.enabled = false;
    }
}
