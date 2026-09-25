using UnityEngine;

/// <summary>
/// Zona muerta (upgrade raro): área naranja que deja el dash y daña a los enemigos
/// que la pisan durante unos segundos. PlayerCombat las reutiliza, nunca se destruyen.
/// </summary>
public class ToxicZone : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer areaRenderer;
    public SpriteRenderer borderRenderer;
    public float rotationSpeed = 30f;
    public Color toxicColor = new Color(1f, 0.4f, 0f, 0.33f); // #FF660055

    [Header("Damage")]
    [Tooltip("Cada cuánto (segundos) daña a los enemigos que estén dentro.")]
    public float damageInterval = 0.5f;

    // Radio en unidades de las figuras procedurales a escala 1 (26.5px a 100 ppu).
    private const float SpriteRadius = 0.265f;
    private const float FadeDuration = 0.3f;

    private float radius;
    private int damage;
    private float lifeTimer;
    private float damageTimer;

    public static ToxicZone Create()
    {
        GameObject go = new GameObject("DeadZone");
        ToxicZone zone = go.AddComponent<ToxicZone>();

        zone.areaRenderer = go.AddComponent<SpriteRenderer>();
        zone.areaRenderer.sprite = ProceduralSprites.Get("disc");
        zone.areaRenderer.sortingOrder = -3;

        GameObject border = new GameObject("Border");
        border.transform.SetParent(go.transform, false);
        zone.borderRenderer = border.AddComponent<SpriteRenderer>();
        zone.borderRenderer.sprite = ProceduralSprites.Get("dashedcircle");
        zone.borderRenderer.sortingOrder = -2;

        go.SetActive(false);
        return zone;
    }

    public void Activate(Vector3 position, float zoneRadius, int zoneDamage, float duration)
    {
        radius = zoneRadius;
        damage = zoneDamage;
        lifeTimer = duration;
        damageTimer = 0f;

        transform.position = position;
        transform.localScale = Vector3.one * (zoneRadius / SpriteRadius);
        SetAlpha(1f);
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        float deltaTime = Time.deltaTime;

        if (borderRenderer != null)
            borderRenderer.transform.Rotate(0f, 0f, rotationSpeed * deltaTime);

        damageTimer -= deltaTime;
        if (damageTimer <= 0f)
        {
            damageTimer = damageInterval;
            EnemyManager.Instance?.DamageEnemiesInRadius(transform.position, radius, damage);
        }

        lifeTimer -= deltaTime;
        if (lifeTimer < FadeDuration) SetAlpha(Mathf.Clamp01(lifeTimer / FadeDuration));
        if (lifeTimer <= 0f) gameObject.SetActive(false);
    }

    private void SetAlpha(float t)
    {
        if (areaRenderer != null)
        {
            Color area = toxicColor;
            area.a *= t;
            areaRenderer.color = area;
        }

        if (borderRenderer != null)
            borderRenderer.color = new Color(toxicColor.r, toxicColor.g, toxicColor.b, 0.8f * t);
    }
}
