using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyVisualFeedback : MonoBehaviour
{
    [Header("Hit Flash")]
    public float hitFlashDuration = 0.1f;
    private Color baseColor;
    private SpriteRenderer sr;
    private float flashTimer;

    public Color BaseColor => baseColor;

    [Header("Elite Glow")]
    public bool isEliteGlow = false;
    public Color glowColor = new Color(1f, 0.67f, 0f, 0.33f); // #FFAA0055
    public float glowMinScale = 1f;
    public float glowMaxScale = 1.15f;
    public float glowCycleDuration = 0.8f;
    private Transform glowTransform;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            baseColor = sr.color;

        CreateGlowEffect();
    }

    private void CreateGlowEffect()
    {
        if (!isEliteGlow) return;

        GameObject glowObj = new GameObject("EliteGlow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localRotation = Quaternion.identity;

        SpriteRenderer glowSr = glowObj.AddComponent<SpriteRenderer>();
        if (sr != null)
        {
            glowSr.sprite = sr.sprite;
            glowSr.color = glowColor;
            glowSr.sortingOrder = sr.sortingOrder - 1;
        }

        glowTransform = glowObj.transform;
    }

    /// <summary>
    /// Llamado por EnemyManager, no por Unity: con muchos enemigos en pantalla, N
    /// callbacks Update managed cuestan más que el trabajo que hacen.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (flashTimer > 0f)
        {
            flashTimer -= deltaTime;
            if (flashTimer <= 0f)
            {
                flashTimer = 0f;
                if (sr != null) sr.color = baseColor;
            }
        }

        if (isEliteGlow && glowTransform != null)
        {
            float t = Mathf.PingPong(Time.time / glowCycleDuration, 1f);
            float scale = Mathf.Lerp(glowMinScale, glowMaxScale, t);
            glowTransform.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// El enemigo vuelve al pool: si el flash seguía activo, el sprite se quedaría
    /// blanco para siempre en la siguiente reutilización.
    /// </summary>
    private void OnDisable()
    {
        flashTimer = 0f;
        if (sr != null) sr.color = baseColor;
    }

    public void TriggerHitFlash()
    {
        if (sr == null) return;

        flashTimer = hitFlashDuration;
        sr.color = Color.white;
    }

    public void SetBaseColor(Color color)
    {
        baseColor = color;
        if (sr != null && flashTimer <= 0f)
            sr.color = baseColor;
    }

    public void SetEliteGlow(bool enabled)
    {
        isEliteGlow = enabled;
        if (enabled && glowTransform == null)
            CreateGlowEffect();
        if (glowTransform != null)
            glowTransform.gameObject.SetActive(enabled);
    }
}
