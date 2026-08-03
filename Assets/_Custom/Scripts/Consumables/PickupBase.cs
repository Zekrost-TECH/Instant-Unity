using UnityEngine;

public class PickupBase : MonoBehaviour
{
    [Header("Lifetime")]
    [Tooltip("Tiempo que el pickup permanece en el mundo antes de desaparecer.")]
    public float lifetime = 12f;
    [Tooltip("Últimos segundos de vida en los que el pickup parpadea.")]
    public float blinkDuration = 3f;

    [Header("Magnet")]
    [Tooltip("Radio desde el que el pickup vuela hacia el jugador.")]
    public float magnetRadius = 1.5f;
    [Tooltip("Velocidad del vuelo magnético.")]
    public float magnetSpeed = 6f;

    public ConsumableType Type { get; private set; }

    private GeometryRenderer shapeRenderer;
    private Color baseColor;
    private float age;
    private Transform playerTransform;
    private Rigidbody2D rb;

    private void Awake()
    {
        shapeRenderer = GetComponent<GeometryRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        age = 0f;
        if (shapeRenderer != null) shapeRenderer.SetColor(baseColor);
    }

    public void Setup(ConsumableType type, Color color, float size, GeometryRenderer.ShapeType shape)
    {
        Type = type;
        baseColor = color;

        if (shapeRenderer != null)
        {
            shapeRenderer.shape = shape;
            shapeRenderer.size = size;
            shapeRenderer.GenerateMesh();
            shapeRenderer.SetColor(baseColor);
        }

        playerTransform = null;
        age = 0f;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        age += Time.deltaTime;

        // Parpadeo antes de desaparecer
        float remaining = lifetime - age;
        if (shapeRenderer != null && remaining < blinkDuration)
        {
            float blink = Mathf.Abs(Mathf.Sin(age * 10f));
            Color c = baseColor;
            c.a = Mathf.Lerp(0.15f, 1f, blink);
            shapeRenderer.SetColor(c);
        }

        if (age >= lifetime)
        {
            PickupManager.Instance?.Recycle(this);
            return;
        }

        // Imán hacia el jugador
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        if (playerTransform == null) return;

        Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude < magnetRadius * magnetRadius && toPlayer.sqrMagnitude > 0.0001f)
        {
            if (rb != null)
            {
                rb.linearVelocity = toPlayer.normalized * magnetSpeed;
            }
            else
            {
                transform.position += (Vector3)(toPlayer.normalized * magnetSpeed * Time.deltaTime);
            }
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PickupManager.Instance?.Collect(this);
    }
}
