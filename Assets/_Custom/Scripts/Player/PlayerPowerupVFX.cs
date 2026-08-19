using UnityEngine;

/// <summary>
/// Feedback visual neón sobre el jugador para los consumibles: burst al recoger y
/// una señal persistente CONSTANTE por buff temporal (aparece al empezar y
/// desaparece al terminar, sin pulsos intermedios). Los canales se construyen una
/// sola vez (en el prefab o en Awake) y se activan/desactivan; no hay Instantiate.
/// </summary>
public class PlayerPowerupVFX : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite ringSprite;
    public Sprite glowSprite;
    public Sprite shardSprite;
    public Sprite chevronSprite;
    public Sprite hexSprite;

    [Header("Config")]
    [Tooltip("Duración del burst genérico de recogida.")]
    public float burstDuration = 0.7f;
    [Tooltip("Duración de la shockwave de ScreenClear.")]
    public float clearDuration = 0.8f;
    [Tooltip("Radio máximo que alcanza la shockwave de ScreenClear.")]
    public float clearMaxRadius = 7f;

    [SerializeField] private Transform burstRoot;
    [SerializeField] private SpriteRenderer[] burstRings = new SpriteRenderer[2];
    [SerializeField] private SpriteRenderer[] burstShards = new SpriteRenderer[6];

    [SerializeField] private Transform speedRoot;
    [SerializeField] private SpriteRenderer[] speedChevrons = new SpriteRenderer[3];
    [SerializeField] private SpriteRenderer[] speedLines = new SpriteRenderer[2];

    [SerializeField] private Transform attackRoot;
    [SerializeField] private SpriteRenderer[] attackOrbits = new SpriteRenderer[4];

    [SerializeField] private Transform tripleRoot;
    [SerializeField] private SpriteRenderer[] tripleOrbits = new SpriteRenderer[3];

    [SerializeField] private Transform invulnRoot;
    [SerializeField] private SpriteRenderer invulnRingA;
    [SerializeField] private SpriteRenderer invulnRingB;
    [SerializeField] private SpriteRenderer[] invulnNodes = new SpriteRenderer[4];

    [SerializeField] private Transform clearRoot;
    [SerializeField] private SpriteRenderer[] clearRings = new SpriteRenderer[3];
    [SerializeField] private SpriteRenderer[] clearShards = new SpriteRenderer[8];

    private float speedTimer;
    private float attackTimer;
    private float tripleTimer;
    private float invulnTimer;

    private Color speedColor = Color.white;
    private Color attackColor = Color.white;
    private Color tripleColor = Color.white;
    private Color invulnColor = Color.white;

    private float burstElapsed;
    private Color burstColor = Color.white;
    private bool burstActive;

    private float clearElapsed;
    private Color clearColor = Color.white;
    private bool clearActive;

    private void Awake()
    {
        EnsureBuilt();
    }

    /// <summary>Construye los canales (idempotente). Útil para hornear el prefab en el editor.</summary>
    public void BuildChannels()
    {
        EnsureBuilt();
    }

    public void Play(ConsumableType type, Color color, float duration)
    {
        switch (type)
        {
            case ConsumableType.TimeBonus:
                TriggerBurst(color);
                break;
            case ConsumableType.SpeedBoost:
                TriggerBurst(color);
                speedColor = color;
                speedTimer = Mathf.Max(speedTimer, duration);
                ApplySpeedVisual();
                if (speedRoot != null) speedRoot.gameObject.SetActive(true);
                break;
            case ConsumableType.AttackSpeedBoost:
                TriggerBurst(color);
                attackColor = color;
                attackTimer = Mathf.Max(attackTimer, duration);
                ApplyAttackVisual();
                if (attackRoot != null) attackRoot.gameObject.SetActive(true);
                break;
            case ConsumableType.TripleShot:
                TriggerBurst(color);
                tripleColor = color;
                tripleTimer = Mathf.Max(tripleTimer, duration);
                ApplyTripleVisual();
                if (tripleRoot != null) tripleRoot.gameObject.SetActive(true);
                break;
            case ConsumableType.Invulnerability:
                TriggerBurst(color);
                invulnColor = color;
                invulnTimer = Mathf.Max(invulnTimer, duration);
                ApplyInvulnerabilityVisual();
                if (invulnRoot != null) invulnRoot.gameObject.SetActive(true);
                break;
            case ConsumableType.ScreenClear:
                TriggerClearBurst(color);
                break;
        }
    }

    public void ResetState()
    {
        speedTimer = 0f;
        attackTimer = 0f;
        tripleTimer = 0f;
        invulnTimer = 0f;
        burstActive = false;
        clearActive = false;

        if (burstRoot != null) burstRoot.gameObject.SetActive(false);
        if (speedRoot != null) speedRoot.gameObject.SetActive(false);
        if (attackRoot != null) attackRoot.gameObject.SetActive(false);
        if (tripleRoot != null) tripleRoot.gameObject.SetActive(false);
        if (invulnRoot != null) invulnRoot.gameObject.SetActive(false);
        if (clearRoot != null) clearRoot.gameObject.SetActive(false);
    }

    private void Update()
    {
        bool playing = GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing;

        if (burstActive)
        {
            burstElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(burstElapsed / burstDuration);
            AnimateBurst(t);
            if (t >= 1f)
            {
                burstActive = false;
                if (burstRoot != null) burstRoot.gameObject.SetActive(false);
            }
        }

        if (clearActive)
        {
            clearElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(clearElapsed / clearDuration);
            AnimateClear(t);
            if (t >= 1f)
            {
                clearActive = false;
                if (clearRoot != null) clearRoot.gameObject.SetActive(false);
            }
        }

        // Fuera de Playing los buffs se congelan (igual que los timers de gameplay) y se ocultan.
        if (!playing)
        {
            if (speedRoot != null && speedRoot.gameObject.activeSelf) speedRoot.gameObject.SetActive(false);
            if (attackRoot != null && attackRoot.gameObject.activeSelf) attackRoot.gameObject.SetActive(false);
            if (tripleRoot != null && tripleRoot.gameObject.activeSelf) tripleRoot.gameObject.SetActive(false);
            if (invulnRoot != null && invulnRoot.gameObject.activeSelf) invulnRoot.gameObject.SetActive(false);
            return;
        }

        if (speedTimer > 0f)
        {
            if (speedRoot != null && !speedRoot.gameObject.activeSelf) speedRoot.gameObject.SetActive(true);
            speedTimer -= Time.deltaTime;
            if (speedTimer <= 0f && speedRoot != null) speedRoot.gameObject.SetActive(false);
        }

        if (attackTimer > 0f)
        {
            if (attackRoot != null && !attackRoot.gameObject.activeSelf) attackRoot.gameObject.SetActive(true);
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f && attackRoot != null) attackRoot.gameObject.SetActive(false);
        }

        if (tripleTimer > 0f)
        {
            if (tripleRoot != null && !tripleRoot.gameObject.activeSelf) tripleRoot.gameObject.SetActive(true);
            tripleTimer -= Time.deltaTime;
            if (tripleTimer <= 0f && tripleRoot != null) tripleRoot.gameObject.SetActive(false);
        }

        if (invulnTimer > 0f)
        {
            if (invulnRoot != null && !invulnRoot.gameObject.activeSelf) invulnRoot.gameObject.SetActive(true);
            invulnTimer -= Time.deltaTime;
            if (invulnTimer <= 0f && invulnRoot != null) invulnRoot.gameObject.SetActive(false);
        }
    }

    private void TriggerBurst(Color color)
    {
        if (burstRoot == null) return;

        burstColor = color;
        burstElapsed = 0f;
        burstActive = true;
        burstRoot.gameObject.SetActive(true);

        if (burstRings[0] != null) { burstRings[0].color = color; }
        if (burstRings[1] != null) { burstRings[1].color = color; }
        for (int i = 0; i < burstShards.Length; i++)
        {
            if (burstShards[i] != null) burstShards[i].color = color;
        }
    }

    private void TriggerClearBurst(Color color)
    {
        if (clearRoot == null) return;

        clearColor = color;
        clearElapsed = 0f;
        clearActive = true;
        clearRoot.gameObject.SetActive(true);

        for (int i = 0; i < clearRings.Length; i++)
        {
            if (clearRings[i] != null) clearRings[i].color = color;
        }
        for (int i = 0; i < clearShards.Length; i++)
        {
            if (clearShards[i] != null) clearShards[i].color = color;
        }
    }

    private void AnimateBurst(float t)
    {
        float fade = 1f - t;

        if (burstRings[0] != null)
        {
            burstRings[0].transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.2f, t);
            Color c = burstColor;
            c.a = 0.9f * fade;
            burstRings[0].color = c;
        }
        if (burstRings[1] != null)
        {
            burstRings[1].transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.7f, t);
            Color c = burstColor;
            c.a = 0.8f * fade;
            burstRings[1].color = c;
        }

        for (int i = 0; i < burstShards.Length; i++)
        {
            SpriteRenderer sr = burstShards[i];
            if (sr == null) continue;

            float angle = i * 60f + t * 120f;
            float radius = 0.45f + t * 1.1f;
            float rad = angle * Mathf.Deg2Rad;
            sr.transform.localPosition = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius + t * 0.5f, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);

            Color c = burstColor;
            c.a = fade;
            sr.color = c;
        }
    }

    private void AnimateClear(float t)
    {
        float fade = 1f - t;

        for (int i = 0; i < clearRings.Length; i++)
        {
            SpriteRenderer sr = clearRings[i];
            if (sr == null) continue;

            sr.transform.localScale = Vector3.one * (0.5f + t * clearMaxRadius);
            Color c = clearColor;
            c.a = fade * (1f - i * 0.15f);
            sr.color = c;
        }

        for (int i = 0; i < clearShards.Length; i++)
        {
            SpriteRenderer sr = clearShards[i];
            if (sr == null) continue;

            float angle = i * 45f;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            sr.transform.localPosition = dir * (0.3f + t * (clearMaxRadius * 0.9f));
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);

            Color c = clearColor;
            c.a = fade;
            sr.color = c;
        }
    }

    // Los visuales persistentes son CONSTANTES: se posicionan una vez al activarse y
    // permanecen estáticos hasta que el timer termina, haciendo claro el inicio/final.

    private void ApplySpeedVisual()
    {
        for (int i = 0; i < speedChevrons.Length; i++)
        {
            SpriteRenderer sr = speedChevrons[i];
            if (sr == null) continue;

            sr.transform.localPosition = new Vector3(0f, -(0.35f + i * 0.3f), 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            sr.transform.localScale = Vector3.one;

            Color c = speedColor;
            c.a = 0.85f;
            sr.color = c;
        }

        for (int i = 0; i < speedLines.Length; i++)
        {
            SpriteRenderer sr = speedLines[i];
            if (sr == null) continue;

            float side = i == 0 ? 1f : -1f;
            sr.transform.localPosition = new Vector3(side * 0.55f, 0f, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            Color c = speedColor;
            c.a = 0.6f;
            sr.color = c;
        }
    }

    private void ApplyAttackVisual()
    {
        for (int i = 0; i < attackOrbits.Length; i++)
        {
            SpriteRenderer sr = attackOrbits[i];
            if (sr == null) continue;

            float angle = 45f + i * 90f;
            float rad = angle * Mathf.Deg2Rad;
            sr.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.85f, Mathf.Sin(rad) * 0.85f, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            sr.transform.localScale = Vector3.one * 0.8f;

            Color c = attackColor;
            c.a = 0.95f;
            sr.color = c;
        }
    }

    private void ApplyTripleVisual()
    {
        for (int i = 0; i < tripleOrbits.Length; i++)
        {
            SpriteRenderer sr = tripleOrbits[i];
            if (sr == null) continue;

            float angle = 90f + i * 120f;
            float rad = angle * Mathf.Deg2Rad;
            sr.transform.localPosition = new Vector3(Mathf.Cos(rad) * 1.4f, Mathf.Sin(rad) * 1.4f, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one * 0.55f;

            Color c = tripleColor;
            c.a = 0.95f;
            sr.color = c;
        }
    }

    private void ApplyInvulnerabilityVisual()
    {
        if (invulnRingA != null)
        {
            invulnRingA.transform.localScale = Vector3.one * 1.2f;
            invulnRingA.transform.localRotation = Quaternion.identity;

            Color c = invulnColor;
            c.a = 0.6f;
            invulnRingA.color = c;
        }

        if (invulnRingB != null)
        {
            invulnRingB.transform.localScale = Vector3.one;
            invulnRingB.transform.localRotation = Quaternion.identity;

            Color c = invulnColor;
            c.a = 0.45f;
            invulnRingB.color = c;
        }

        for (int i = 0; i < invulnNodes.Length; i++)
        {
            SpriteRenderer sr = invulnNodes[i];
            if (sr == null) continue;

            float angle = 45f + i * 90f;
            float rad = angle * Mathf.Deg2Rad;
            sr.transform.localPosition = new Vector3(Mathf.Cos(rad) * 1.05f, Mathf.Sin(rad) * 1.05f, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);

            Color c = invulnColor;
            c.a = 0.9f;
            sr.color = c;
        }
    }

    private void EnsureBuilt()
    {
        if (burstRoot == null) burstRoot = CreateChannel("Burst");
        if (speedRoot == null) speedRoot = CreateChannel("Speed");
        if (attackRoot == null) attackRoot = CreateChannel("AttackSpeed");
        if (tripleRoot == null) tripleRoot = CreateChannel("TripleShot");
        if (invulnRoot == null) invulnRoot = CreateChannel("Invulnerability");
        if (clearRoot == null) clearRoot = CreateChannel("ScreenClear");

        if (burstRings[0] == null) burstRings[0] = CreateRenderer(burstRoot, ringSprite, -2);
        if (burstRings[1] == null) burstRings[1] = CreateRenderer(burstRoot, ringSprite, -2);
        for (int i = 0; i < burstShards.Length; i++)
            if (burstShards[i] == null) burstShards[i] = CreateRenderer(burstRoot, shardSprite, 3);

        for (int i = 0; i < speedChevrons.Length; i++)
            if (speedChevrons[i] == null) speedChevrons[i] = CreateRenderer(speedRoot, chevronSprite, 2);
        for (int i = 0; i < speedLines.Length; i++)
            if (speedLines[i] == null) speedLines[i] = CreateRenderer(speedRoot, shardSprite, 2);

        for (int i = 0; i < attackOrbits.Length; i++)
            if (attackOrbits[i] == null) attackOrbits[i] = CreateRenderer(attackRoot, shardSprite, 2);

        for (int i = 0; i < tripleOrbits.Length; i++)
            if (tripleOrbits[i] == null) tripleOrbits[i] = CreateRenderer(tripleRoot, hexSprite, 2);

        if (invulnRingA == null) invulnRingA = CreateRenderer(invulnRoot, ringSprite, 2);
        if (invulnRingB == null) invulnRingB = CreateRenderer(invulnRoot, ringSprite, 2);
        for (int i = 0; i < invulnNodes.Length; i++)
            if (invulnNodes[i] == null) invulnNodes[i] = CreateRenderer(invulnRoot, shardSprite, 3);

        for (int i = 0; i < clearRings.Length; i++)
            if (clearRings[i] == null) clearRings[i] = CreateRenderer(clearRoot, ringSprite, -1);
        for (int i = 0; i < clearShards.Length; i++)
            if (clearShards[i] == null) clearShards[i] = CreateRenderer(clearRoot, shardSprite, 4);
    }

    private Transform CreateChannel(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.SetActive(false);
        return go.transform;
    }

    private SpriteRenderer CreateRenderer(Transform parent, Sprite sprite, int order)
    {
        GameObject go = new GameObject(sprite != null ? sprite.name : "renderer");
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }
}
