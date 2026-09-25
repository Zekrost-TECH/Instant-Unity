using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Muerte del jugador: la cámara se centra en él y se acerca, el jugador se carga
/// (parpadeo y crecimiento) y estalla en pedazos. Al terminar avisa para mostrar el
/// Game Over. Todo en tiempo real: la partida ya está en GameOver y congelada.
/// </summary>
public class PlayerDeathSequence : MonoBehaviour
{
    [Header("Cámara")]
    [Tooltip("Pausa congelada antes de mover la cámara, para que se lea el golpe final.")]
    public float freezeDuration = 0.15f;
    [Tooltip("Tamaño ortográfico al final del zoom (el de partida es 7).")]
    public float zoomOrthoSize = 3.2f;
    public float zoomDuration = 0.7f;

    [Header("Carga")]
    public float chargeDuration = 0.45f;
    [Tooltip("Escala máxima del jugador justo antes de estallar.")]
    public float chargeScale = 1.35f;
    [Tooltip("Temblor de cámara (unidades) al final de la carga.")]
    public float chargeShake = 0.05f;

    [Header("Explosión")]
    public Sprite shardSprite;
    public int shardCount = 24;
    public float shardSpeedMin = 2.5f;
    public float shardSpeedMax = 6.5f;
    [Tooltip("Escala de cada pedazo. El dibujo del sprite ocupa poco de su textura: valores bajos apenas se ven.")]
    public float shardSizeMin = 0.45f;
    public float shardSizeMax = 0.9f;
    [Tooltip("Radio máximo (unidades) del destello blanco al estallar.")]
    public float flashRadius = 2.2f;
    public float flashDuration = 0.25f;
    public float shardLifetime = 1.1f;
    public Color accentColor = new Color(0.53f, 0.8f, 1f, 1f); // #88CCFF
    [Tooltip("Temblor de cámara (unidades) en el instante de la explosión.")]
    public float explosionShake = 0.25f;
    [Tooltip("Espera tras la explosión antes de mostrar el Game Over.")]
    public float holdDuration = 0.9f;

    [Header("Revivir (la explosión al revés)")]
    [Tooltip("Los pedazos vuelven de todas partes y se juntan en el jugador.")]
    public float gatherDuration = 0.8f;
    [Tooltip("El jugador reaparece grande y parpadeando y se asienta: la carga al revés.")]
    public float reformDuration = 0.35f;
    [Tooltip("La cámara vuelve a su encuadre de partida.")]
    public float zoomOutDuration = 0.5f;

    private struct Shard
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float spin;
        public float size;
        public Color color;
    }

    // Radio en unidades del disco procedural a escala 1 (26.5px a 100 ppu).
    private const float FlashSpriteRadius = 0.265f;

    private readonly List<Shard> shards = new List<Shard>();
    private readonly List<SpriteRenderer> hiddenRenderers = new List<SpriteRenderer>();
    private Transform shardRoot;
    private SpriteRenderer flash;
    private Camera cam;
    private Vector3 cameraLocalPosition;
    private float cameraOrthoSize;
    private bool cameraCaptured;
    private Vector3 baseScale;
    private Coroutine routine;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

        if (shardRoot != null) Destroy(shardRoot.gameObject);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        // Reiniciar o revivir: cámara, jugador y pedazos vuelven a su estado de partida.
        if (state == GameManager.GameState.Playing) Restore();
    }

    public void Play(Action onComplete)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(onComplete));
    }

    private IEnumerator Run(Action onComplete)
    {
        CaptureCamera();
        Vector3 center = transform.position;
        Color bodyColor = SkinManager.Instance != null ? SkinManager.Instance.GetEquippedColor(Color.white) : Color.white;
        SpriteRenderer body = GetComponent<SpriteRenderer>();

        yield return new WaitForSecondsRealtime(freezeDuration);

        // 1. La cámara se centra en el jugador y se acerca
        Vector3 startPosition = cam != null ? cam.transform.position : Vector3.zero;
        Vector3 focus = new Vector3(center.x, center.y, startPosition.z);
        float startSize = cam != null ? cam.orthographicSize : zoomOrthoSize;

        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / zoomDuration);
            float eased = t * t * (3f - 2f * t);
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(startPosition, focus, eased);
                cam.orthographicSize = Mathf.Lerp(startSize, zoomOrthoSize, eased);
            }
            yield return null;
        }

        // 2. Carga: crece y parpadea en blanco cada vez más rápido. El temblor va en la
        // cámara: mover el jugador pelearía con su Rigidbody2D.
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / chargeDuration);
            transform.localScale = baseScale * Mathf.Lerp(1f, chargeScale, t * t);
            if (body != null) body.color = Color.Lerp(bodyColor, Color.white, Mathf.PingPong(t * t * 8f, 1f));
            if (cam != null) cam.transform.position = focus + (Vector3)(UnityEngine.Random.insideUnitCircle * chargeShake * t);
            yield return null;
        }

        transform.localScale = baseScale;
        if (body != null) body.color = bodyColor;

        // 3. Estalla en pedazos
        HideVisuals();
        LaunchShards(center, bodyColor);
        ParticleManager.Instance?.SpawnDeathParticles(center, bodyColor, 24);
        PickupManager.Instance?.SpawnRing(center, Color.white, 1.2f);
        PickupManager.Instance?.SpawnRing(center, accentColor, 2.6f);
        AudioManager.Instance?.PlayPlayerDeathSFX();
        HapticManager.Instance?.TriggerDeath();

        float duration = Mathf.Max(shardLifetime, holdDuration);
        for (float elapsed = 0f; elapsed < duration;)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;
            UpdateShards(deltaTime, elapsed);
            UpdateFlash(elapsed);

            float shake = explosionShake * Mathf.Clamp01(1f - elapsed / 0.35f);
            if (cam != null) cam.transform.position = focus + (Vector3)(UnityEngine.Random.insideUnitCircle * shake);
            yield return null;
        }

        if (cam != null) cam.transform.position = focus;
        SetShardsActive(false);
        routine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Revivir: los pedazos vuelven girando, implosionan con un destello, el jugador se
    /// rearma y la cámara se aleja a su encuadre. La partida sigue congelada en GameOver
    /// hasta onComplete.
    /// </summary>
    public void PlayRevive(Action onComplete)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(RunRevive(onComplete));
    }

    private IEnumerator RunRevive(Action onComplete)
    {
        CaptureCamera();
        EnsureShards();
        Vector3 center = transform.position;
        Color bodyColor = SkinManager.Instance != null ? SkinManager.Instance.GetEquippedColor(Color.white) : Color.white;
        SpriteRenderer body = GetComponent<SpriteRenderer>();
        if (hiddenRenderers.Count == 0) HideVisuals();

        // 1. Los pedazos aparecen dispersos y aceleran hacia el centro: la explosión al revés.
        GatherShards(center, bodyColor);
        for (float t = 0f; t < 1f;)
        {
            float deltaTime = Time.unscaledDeltaTime;
            t = Mathf.Min(1f, t + deltaTime / gatherDuration);
            UpdateGather(center, deltaTime, t);
            yield return null;
        }

        // 2. Implosión: se rearma grande y parpadeando y se asienta
        SetShardsActive(false);
        ShowVisuals();
        ParticleManager.Instance?.SpawnDeathParticles(center, bodyColor, 16);
        PickupManager.Instance?.SpawnRing(center, Color.white, 1.2f);
        PickupManager.Instance?.SpawnRing(center, accentColor, 2.6f);
        AudioManager.Instance?.PlayReviveSFX();
        HapticManager.Instance?.TriggerEliteKill();

        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / reformDuration);
            float settle = 1f - (1f - t) * (1f - t);
            transform.localScale = baseScale * Mathf.Lerp(chargeScale, 1f, settle);
            if (body != null) body.color = Color.Lerp(bodyColor, Color.white, Mathf.PingPong((1f - t) * (1f - t) * 8f, 1f));
            float shake = chargeShake * 2f * (1f - t);
            if (cam != null) cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z) + (Vector3)(UnityEngine.Random.insideUnitCircle * shake);
            yield return null;
        }
        transform.localScale = baseScale;
        if (body != null) body.color = bodyColor;

        // 3. La cámara vuelve al encuadre de partida
        if (cam != null && cameraCaptured)
        {
            Vector3 fromPosition = new Vector3(center.x, center.y, cam.transform.localPosition.z);
            float fromSize = cam.orthographicSize;
            for (float t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / zoomOutDuration);
                float eased = t * t * (3f - 2f * t);
                cam.transform.localPosition = Vector3.Lerp(fromPosition, cameraLocalPosition, eased);
                cam.orthographicSize = Mathf.Lerp(fromSize, cameraOrthoSize, eased);
                yield return null;
            }
        }

        routine = null;
        onComplete?.Invoke();
    }

    private void GatherShards(Vector3 center, Color bodyColor)
    {
        for (int i = 0; i < shards.Count; i++)
        {
            Shard shard = shards[i];
            float angle = (i + UnityEngine.Random.value * 0.6f) / shards.Count * Mathf.PI * 2f;
            // velocity guarda aquí el desplazamiento de partida respecto al centro.
            shard.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(1.2f, 3f);
            shard.spin = UnityEngine.Random.Range(-720f, 720f);
            shard.size = UnityEngine.Random.Range(shardSizeMin, shardSizeMax);
            shard.color = i % 3 == 0 ? Color.white : (i % 3 == 1 ? accentColor : bodyColor);

            shard.transform.position = center + (Vector3)shard.velocity;
            shard.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            shard.renderer.color = Color.clear;
            shard.transform.gameObject.SetActive(true);
            shards[i] = shard;
        }
    }

    private void UpdateGather(Vector3 center, float deltaTime, float t)
    {
        float pull = t * t * t;
        float fadeIn = Mathf.SmoothStep(0f, 1f, t / 0.35f);

        for (int i = 0; i < shards.Count; i++)
        {
            Shard shard = shards[i];
            shard.transform.position = center + (Vector3)(shard.velocity * (1f - pull));
            shard.transform.Rotate(0f, 0f, shard.spin * deltaTime);
            shard.transform.localScale = Vector3.one * (shard.size * Mathf.Lerp(0.4f, 1f, t));

            Color c = shard.color;
            c.a *= fadeIn;
            shard.renderer.color = c;
        }

        // El destello al revés: un disco que se cierra y se enciende al llegar los pedazos.
        float implode = Mathf.InverseLerp(0.55f, 1f, t);
        flash.gameObject.SetActive(implode > 0f);
        flash.transform.position = center;
        flash.transform.localScale = Vector3.one * (Mathf.Lerp(flashRadius, 0.3f, implode * implode) / FlashSpriteRadius);
        flash.color = new Color(1f, 1f, 1f, 0.9f * implode);
    }

    private void ShowVisuals()
    {
        for (int i = 0; i < hiddenRenderers.Count; i++)
        {
            if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = true;
        }
        hiddenRenderers.Clear();
    }

    private void CaptureCamera()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || cameraCaptured) return;

        cameraLocalPosition = cam.transform.localPosition;
        cameraOrthoSize = cam.orthographicSize;
        cameraCaptured = true;
    }

    private void Restore()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (cameraCaptured && cam != null)
        {
            cam.transform.localPosition = cameraLocalPosition;
            cam.orthographicSize = cameraOrthoSize;
        }

        transform.localScale = baseScale;
        ShowVisuals();
        SetShardsActive(false);
    }

    /// <summary>
    /// Oculta el cuerpo y sus indicadores. Los efectos de consumible se apagan con su
    /// propio ResetState: si se restauraran a ciegas volverían a verse al reiniciar.
    /// </summary>
    private void HideVisuals()
    {
        PlayerPowerupVFX powerupVFX = GetComponentInChildren<PlayerPowerupVFX>();
        if (powerupVFX != null) powerupVFX.ResetState();

        hiddenRenderers.Clear();
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer r = renderers[i];
            if (!r.enabled) continue;
            if (powerupVFX != null && r.transform.IsChildOf(powerupVFX.transform)) continue;

            r.enabled = false;
            hiddenRenderers.Add(r);
        }
    }

    private void LaunchShards(Vector3 center, Color bodyColor)
    {
        EnsureShards();

        flash.transform.position = center;
        flash.gameObject.SetActive(true);
        UpdateFlash(0f);

        for (int i = 0; i < shards.Count; i++)
        {
            Shard shard = shards[i];
            float angle = (i + UnityEngine.Random.value * 0.6f) / shards.Count * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            shard.velocity = direction * UnityEngine.Random.Range(shardSpeedMin, shardSpeedMax);
            shard.spin = UnityEngine.Random.Range(-720f, 720f);
            shard.size = UnityEngine.Random.Range(shardSizeMin, shardSizeMax);
            shard.color = i % 3 == 0 ? Color.white : (i % 3 == 1 ? accentColor : bodyColor);

            shard.transform.position = center;
            shard.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            shard.transform.localScale = Vector3.one * shard.size;
            shard.renderer.color = shard.color;
            shard.transform.gameObject.SetActive(true);
            shards[i] = shard;
        }
    }

    private void UpdateShards(float deltaTime, float elapsed)
    {
        float t = Mathf.Clamp01(elapsed / shardLifetime);
        float fade = 1f - Mathf.SmoothStep(0.45f, 1f, t);
        float damping = Mathf.Exp(-2.2f * deltaTime);

        for (int i = 0; i < shards.Count; i++)
        {
            Shard shard = shards[i];
            shard.velocity *= damping;
            shard.transform.position += (Vector3)(shard.velocity * deltaTime);
            shard.transform.Rotate(0f, 0f, shard.spin * deltaTime);
            shard.transform.localScale = Vector3.one * (shard.size * Mathf.Lerp(1f, 0.4f, t));

            Color c = shard.color;
            c.a *= fade;
            shard.renderer.color = c;
            shards[i] = shard;
        }
    }

    private void UpdateFlash(float elapsed)
    {
        if (flash == null || !flash.gameObject.activeSelf) return;

        float t = Mathf.Clamp01(elapsed / flashDuration);
        flash.transform.localScale = Vector3.one * (Mathf.Lerp(0.3f, flashRadius, 1f - (1f - t) * (1f - t)) / FlashSpriteRadius);
        flash.color = new Color(1f, 1f, 1f, 0.9f * (1f - t));
        if (t >= 1f) flash.gameObject.SetActive(false);
    }

    /// <summary>Los pedazos se crean una sola vez por partida y se reutilizan en cada muerte.</summary>
    private void EnsureShards()
    {
        if (shardRoot != null) return;

        shardRoot = new GameObject("PlayerDeathShards").transform;
        for (int i = 0; i < shardCount; i++)
        {
            GameObject go = new GameObject("Shard");
            go.transform.SetParent(shardRoot, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = shardSprite;
            sr.sortingOrder = 5;
            go.SetActive(false);
            shards.Add(new Shard { transform = go.transform, renderer = sr });
        }

        GameObject flashGo = new GameObject("Flash");
        flashGo.transform.SetParent(shardRoot, false);
        flash = flashGo.AddComponent<SpriteRenderer>();
        flash.sprite = ProceduralSprites.Get("disc");
        flash.sortingOrder = 6;
        flashGo.SetActive(false);
    }

    private void SetShardsActive(bool active)
    {
        for (int i = 0; i < shards.Count; i++)
        {
            if (shards[i].transform != null) shards[i].transform.gameObject.SetActive(active);
        }

        if (flash != null) flash.gameObject.SetActive(active);
    }
}
