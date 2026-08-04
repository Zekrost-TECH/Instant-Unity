using UnityEngine;

public class PickupRingFx : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Duración del anillo en segundos.")]
    public float duration = 0.35f;
    [Tooltip("Escala máxima que alcanza el anillo.")]
    public float maxScale = 1.4f;

    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float elapsed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        transform.localScale = Vector3.zero;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;
    }

    public void Setup(Color color)
    {
        baseColor = color;
        elapsed = 0f;
        transform.localScale = Vector3.zero;
        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        transform.localScale = Vector3.one * (maxScale * t);

        if (spriteRenderer != null)
        {
            Color c = baseColor;
            c.a = 1f - t;
            spriteRenderer.color = c;
        }

        if (t >= 1f)
        {
            PickupManager.Instance?.RecycleRing(this);
        }
    }
}
