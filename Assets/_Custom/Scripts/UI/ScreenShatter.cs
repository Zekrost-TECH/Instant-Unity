using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barrera rota: congela la imagen de la pantalla (HUD incluido), la agrieta desde donde
/// cayó el jefe y la hace estallar en pedazos de cristal que caen y dejan ver el juego.
/// Va en un canvas propio por encima de todos; crackLight es la luz que asoma por las grietas.
/// </summary>
public class ScreenShatter : MaskableGraphic
{
    [Header("Grietas")]
    [Tooltip("Image a pantalla completa detrás del cristal: asoma por las grietas y destella al romperse.")]
    [SerializeField] private Image crackLight;
    [SerializeField] private Color crackColor = new Color(1f, 0.92f, 0.7f, 1f);
    [Tooltip("Grietas radiales desde el impacto.")]
    [SerializeField] private int rays = 14;
    [Tooltip("Anillos de grietas: más anillos, pedazos más pequeños.")]
    [SerializeField] private int rings = 6;
    [Tooltip("Ancho de cada grieta en unidades del canvas.")]
    [SerializeField] private float crackWidth = 5f;
    [Tooltip("Segundos (tiempo real) que tarda la grieta en cruzar la pantalla.")]
    [SerializeField] private float crackSpread = 0.3f;
    [SerializeField] private float impactShake = 14f;

    [Header("Estallido")]
    [SerializeField] private float shardSpeed = 900f;
    [SerializeField] private float shardGravity = 3200f;
    [SerializeField] private float shardDuration = 1.1f;
    [Tooltip("Velocidad (unidades/s) a la que la rotura viaja del impacto a los bordes.")]
    [SerializeField] private float breakSpread = 5000f;
    [Range(0f, 1f)] [SerializeField] private float breakFlash = 0.6f;

    private struct Shard
    {
        public Vector2 a, b, c;
        public Vector2 center;
        public Vector2 direction;
        public Vector2 velocity;
        public float distance;
        public float spin;
        public float shade;
    }

    private const float FlashDuration = 0.35f;
    private const float StuckTimeout = 2f;

    private readonly List<Shard> shards = new List<Shard>(256);
    private Texture2D capture;
    private Vector2 impact;
    private float screenReach;
    private float crackStart = -1f;
    private float breakStart = -1f;
    private bool busy;
    private bool subscribed;

    public override Texture mainTexture => capture != null ? capture : s_WhiteTexture;

    protected override void Start()
    {
        base.Start();
        if (!Application.isPlaying) return;

        raycastTarget = false;
        SetCrackLight(0f);

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.OnBarrierBroken += HandleBarrierBroken;
            SpawnManager.Instance.OnBarrierShattered += HandleBarrierShattered;
            subscribed = true;
        }
    }

    protected override void OnDestroy()
    {
        if (subscribed && SpawnManager.Instance != null)
        {
            SpawnManager.Instance.OnBarrierBroken -= HandleBarrierBroken;
            SpawnManager.Instance.OnBarrierShattered -= HandleBarrierShattered;
        }
        if (capture != null) Destroy(capture);
        base.OnDestroy();
    }

    private void HandleBarrierBroken(Vector3 worldPosition, float newMaxTime)
    {
        if (busy) return;
        busy = true;
        breakStart = -1f;
        StartCoroutine(CaptureAndCrack(worldPosition));
    }

    private void HandleBarrierShattered()
    {
        if (busy && breakStart < 0f) breakStart = Time.unscaledTime;
    }

    private IEnumerator CaptureAndCrack(Vector3 worldPosition)
    {
        // Al final del frame la imagen ya está completa (HUD incluido) y el cristal aún no se dibuja.
        yield return new WaitForEndOfFrame();

        if (capture != null) Destroy(capture);
        // La captura llega marcada como lineal con datos sRGB: en espacio de color lineal
        // se vería más clara que el juego. Se copia a una textura sRGB.
        Texture2D raw = ScreenCapture.CaptureScreenshotAsTexture();
        capture = new Texture2D(raw.width, raw.height, raw.format, false, false);
        capture.LoadRawTextureData(raw.GetRawTextureData<byte>());
        capture.Apply(false, true);
        Destroy(raw);
        impact = ToLocal(worldPosition);
        BuildShards();

        crackStart = Time.unscaledTime;
        SetCrackLight(1f);
        SetAllDirty();
    }

    private void Update()
    {
        if (!Application.isPlaying || crackStart < 0f) return;

        float now = Time.unscaledTime;
        if (breakStart < 0f)
        {
            // Reinicio a mitad de la grieta: nadie la va a romper.
            float crackDuration = SpawnManager.Instance != null ? SpawnManager.Instance.barrierCrackDuration : 0f;
            if (now - crackStart > crackDuration + StuckTimeout) Finish();
            else SetVerticesDirty();
            return;
        }

        float elapsed = now - breakStart;
        SetCrackLight(breakFlash * (1f - elapsed / FlashDuration));
        if (elapsed > screenReach / breakSpread + shardDuration) Finish();
        else SetVerticesDirty();
    }

    private void Finish()
    {
        shards.Clear();
        crackStart = -1f;
        breakStart = -1f;
        busy = false;
        SetCrackLight(0f);
        if (capture != null)
        {
            Destroy(capture);
            capture = null;
        }
        SetAllDirty();
    }

    private void SetCrackLight(float alpha)
    {
        if (crackLight == null) return;

        Color color = crackColor;
        color.a = Mathf.Clamp01(alpha);
        crackLight.color = color;
        crackLight.enabled = color.a > 0.001f;
    }

    private Vector2 ToLocal(Vector3 worldPosition)
    {
        Camera cam = Camera.main;
        Vector2 screenPoint = cam != null
            ? (Vector2)cam.WorldToScreenPoint(worldPosition)
            : new Vector2(Screen.width, Screen.height) * 0.5f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out Vector2 local);

        Rect rect = rectTransform.rect;
        return new Vector2(Mathf.Clamp(local.x, rect.xMin, rect.xMax), Mathf.Clamp(local.y, rect.yMin, rect.yMax));
    }

    /// <summary>
    /// Telaraña: rayos desde el impacto cortados por anillos cada vez más separados. Cada
    /// celda se parte en dos triángulos por una diagonal al azar.
    /// </summary>
    private void BuildShards()
    {
        shards.Clear();
        Rect rect = rectTransform.rect;
        screenReach = Mathf.Max(
            Mathf.Max(Vector2.Distance(impact, rect.min), Vector2.Distance(impact, rect.max)),
            Mathf.Max(Vector2.Distance(impact, new Vector2(rect.xMin, rect.yMax)), Vector2.Distance(impact, new Vector2(rect.xMax, rect.yMin))));
        // Margen: el último anillo son cuerdas, no arcos, y no debe dejar esquinas sin cristal.
        float reach = screenReach * 1.25f;

        int rayCount = Mathf.Max(3, rays);
        int ringCount = Mathf.Max(1, rings);
        Vector2[,] points = new Vector2[rayCount, ringCount];
        float step = Mathf.PI * 2f / rayCount;
        float start = Random.value * step;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = start + (i + Random.Range(-0.3f, 0.3f)) * step;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (int j = 0; j < ringCount; j++)
            {
                float radius = j == ringCount - 1
                    ? reach
                    : reach * Mathf.Pow((j + 1f) / ringCount, 1.7f) * Random.Range(0.88f, 1.12f);
                points[i, j] = impact + direction * radius;
            }
        }

        for (int i = 0; i < rayCount; i++)
        {
            int next = (i + 1) % rayCount;
            AddShard(impact, points[i, 0], points[next, 0], rect);

            for (int j = 0; j < ringCount - 1; j++)
            {
                Vector2 p0 = points[i, j];
                Vector2 p1 = points[next, j];
                Vector2 p2 = points[next, j + 1];
                Vector2 p3 = points[i, j + 1];
                if (Random.value < 0.5f)
                {
                    AddShard(p0, p1, p2, rect);
                    AddShard(p0, p2, p3, rect);
                }
                else
                {
                    AddShard(p0, p1, p3, rect);
                    AddShard(p1, p2, p3, rect);
                }
            }
        }
    }

    private void AddShard(Vector2 a, Vector2 b, Vector2 c, Rect rect)
    {
        Vector2 min = Vector2.Min(a, Vector2.Min(b, c));
        Vector2 max = Vector2.Max(a, Vector2.Max(b, c));
        if (!rect.Overlaps(new Rect(min, max - min))) return;

        Vector2 center = (a + b + c) / 3f;
        Vector2 away = center - impact;
        float distance = away.magnitude;
        Vector2 direction = distance > 0.01f ? away / distance : Vector2.up;

        // Los pedazos del centro salen disparados; los del borde casi sólo caen.
        float burst = Mathf.Lerp(1.5f, 0.45f, Mathf.Clamp01(distance / screenReach));
        shards.Add(new Shard
        {
            a = a - center,
            b = b - center,
            c = c - center,
            center = center,
            direction = direction,
            velocity = direction * shardSpeed * burst * Random.Range(0.7f, 1.2f) + Vector2.up * Random.Range(0f, 350f),
            distance = distance,
            spin = Random.Range(-420f, 420f),
            shade = Random.Range(0.8f, 1f)
        });
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (capture == null || crackStart < 0f || shards.Count == 0) return;

        Rect rect = rectTransform.rect;
        float now = Time.unscaledTime;
        float crackElapsed = now - crackStart;
        float crackT = Mathf.Clamp01(crackElapsed / crackSpread);
        float front = screenReach * (1f - (1f - crackT) * (1f - crackT));

        Vector2 shake = Vector2.zero;
        if (crackElapsed < 0.2f && breakStart < 0f)
            shake = Random.insideUnitCircle * impactShake * (1f - crackElapsed / 0.2f);

        for (int i = 0; i < shards.Count; i++)
        {
            Shard shard = shards[i];
            float cracked = Mathf.Clamp01((front - shard.distance) / 120f);
            float flight = breakStart < 0f ? -1f : now - breakStart - shard.distance / breakSpread;
            if (flight >= shardDuration) continue;

            Vector2 position = shard.center + shake + shard.direction * (crackWidth * 0.8f * cracked);
            float angle = 0f;
            float scale = 1f;
            float alpha = 1f;
            if (flight > 0f)
            {
                cracked = 1f;
                float t = flight / shardDuration;
                position += shard.velocity * flight + Vector2.down * (0.5f * shardGravity * flight * flight);
                angle = shard.spin * flight * Mathf.Deg2Rad;
                scale = Mathf.Lerp(1f, 0.7f, t);
                alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, t));
            }

            float shade = Mathf.Lerp(1f, shard.shade, cracked);
            Color32 tint = new Color(color.r * shade, color.g * shade, color.b * shade, color.a * alpha);
            float shrink = crackWidth * 0.5f * cracked;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            int index = vh.currentVertCount;
            AddVertex(vh, shard.a, shard.center, position, shrink, scale, cos, sin, tint, rect);
            AddVertex(vh, shard.b, shard.center, position, shrink, scale, cos, sin, tint, rect);
            AddVertex(vh, shard.c, shard.center, position, shrink, scale, cos, sin, tint, rect);
            vh.AddTriangle(index, index + 1, index + 2);
        }
    }

    private static void AddVertex(VertexHelper vh, Vector2 offset, Vector2 origin, Vector2 position,
        float shrink, float scale, float cos, float sin, Color32 tint, Rect rect)
    {
        // Se recorta hacia el centro (la grieta) y la UV sigue al vértice recortado:
        // el pedazo pierde borde en vez de deformar la imagen.
        float length = offset.magnitude;
        if (length > shrink) offset -= offset / length * shrink;

        Vector2 uvPoint = origin + offset;
        Vector2 uv = new Vector2((uvPoint.x - rect.xMin) / rect.width, (uvPoint.y - rect.yMin) / rect.height);

        offset *= scale;
        Vector2 rotated = new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
        vh.AddVert(position + rotated, tint, uv);
    }
}
