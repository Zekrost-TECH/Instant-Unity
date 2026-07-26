using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class ToxicZone : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer areaRenderer;
    public SpriteRenderer borderRenderer;
    public float rotationSpeed = 30f;
    public Color toxicColor = new Color(1f, 0.4f, 0f, 0.33f); // #FF660055

    [Header("Damage")]
    public float timeDamagePerSecond = 2f;
    public float damageInterval = 0.5f;
    public float radius = 1.2f;

    private float damageTimer = 0f;
    private PlayerCombat playerCombat;
    private Transform borderTransform;

    private void Start()
    {
        transform.localScale = Vector3.one * radius * 2f;

        if (areaRenderer != null)
            areaRenderer.color = toxicColor;

        if (borderRenderer != null)
        {
            borderRenderer.color = new Color(toxicColor.r, toxicColor.g, toxicColor.b, 0.8f);
            borderTransform = borderRenderer.transform;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (borderTransform != null)
            borderTransform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0f && playerCombat != null)
        {
            damageTimer = damageInterval;
            playerCombat.TakeDamageFromEnemy(timeDamagePerSecond * damageInterval);
        }
    }

    private void OnDisable()
    {
        // Si la zona se desactiva mientras el jugador está dentro, OnTriggerExit2D no llega.
        playerCombat = null;
        damageTimer = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerCombat = other.GetComponent<PlayerCombat>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerCombat = null;
        }
    }
}
