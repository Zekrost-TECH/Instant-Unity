using TMPro;
using UnityEngine;

public class PickupFloatingText : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Duración del texto flotante en segundos.")]
    public float duration = 0.9f;
    [Tooltip("Velocidad de ascenso en unidades por segundo.")]
    public float riseSpeed = 1.6f;

    private TextMeshPro textMesh;
    private Color baseColor;
    private float elapsed;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        if (textMesh != null)
        {
            textMesh.color = baseColor;
            textMesh.alpha = 1f;
        }
    }

    public void Setup(string message, Color color)
    {
        baseColor = color;
        elapsed = 0f;

        if (textMesh != null)
        {
            textMesh.text = message;
            textMesh.color = color;
            textMesh.alpha = 1f;
        }
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        transform.position += Vector3.up * (riseSpeed * Time.unscaledDeltaTime);

        if (textMesh != null)
        {
            textMesh.alpha = 1f - (t * t);
        }

        if (t >= 1f)
        {
            PickupManager.Instance?.RecycleFloatingText(this);
        }
    }
}
