using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HitBeam : MonoBehaviour
{
    private LineRenderer lr;
    private GradientColorKey[] colorKeys;
    private GradientAlphaKey[] alphaKeys;
    private Vector3[] positionsBuffer;
    private Gradient gradient;
    private AnimationCurve widthCurve;
    private Bounds bounds;

    private HitVFXManager config;
    private float elapsed;
    private float duration;
    private bool isPlaying;

    private float cachedStartWidth = float.NaN;
    private float cachedEndWidth = float.NaN;
    private string cachedSortingLayerName;
    private int cachedSortingLayerId;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        colorKeys = new GradientColorKey[2];
        alphaKeys = new GradientAlphaKey[2];
        positionsBuffer = new Vector3[2];
        gradient = new Gradient();
        widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        bounds = new Bounds(Vector3.zero, new Vector3(20f, 20f, 20f));
    }

    public void Play(Transform origin, Vector3 hitPoint)
    {
        config = HitVFXManager.Instance;
        if (config == null || lr == null) return;

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        positionsBuffer[0] = origin.position;
        positionsBuffer[1] = hitPoint;
        lr.SetPositions(positionsBuffer);

        // La curva de ancho y la sorting layer sólo se reconstruyen si la config cambió.
        if (cachedStartWidth != config.startWidth || cachedEndWidth != config.endWidth)
        {
            cachedStartWidth = config.startWidth;
            cachedEndWidth = config.endWidth;
            widthCurve = new AnimationCurve(
                new Keyframe(0f, config.startWidth),
                new Keyframe(1f, config.endWidth)
            );
            lr.widthCurve = widthCurve;
        }

        if (!ReferenceEquals(cachedSortingLayerName, config.sortingLayerName))
        {
            cachedSortingLayerName = config.sortingLayerName;
            cachedSortingLayerId = SortingLayer.NameToID(config.sortingLayerName);
        }

        lr.widthMultiplier = 1f;
        lr.sortingLayerID = cachedSortingLayerId;
        lr.sortingOrder = config.sortingOrder;

        lr.startColor = config.color;
        lr.endColor = config.color;

        elapsed = 0f;
        duration = Mathf.Max(0.001f, config.lifetime);
        isPlaying = true;

        ApplyFade(0f);
    }

    private void Update()
    {
        if (!isPlaying) return;

        elapsed += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(elapsed / duration);
        ApplyFade(k);

        if (elapsed < duration) return;

        isPlaying = false;
        if (HitVFXManager.Instance != null)
            HitVFXManager.Instance.ReturnToPool(this);
        else
            gameObject.SetActive(false);
    }

    private void ApplyFade(float k)
    {
        float widthMul = config.widthCurve != null && config.widthCurve.length > 0
            ? config.widthCurve.Evaluate(k)
            : 1f - k;

        float alphaMul = config.alphaCurve != null && config.alphaCurve.length > 0
            ? config.alphaCurve.Evaluate(k)
            : 1f - k;

        colorKeys[0].color = config.color;
        colorKeys[1].color = config.color;
        colorKeys[0].time = 0f;
        colorKeys[1].time = 1f;

        float baseAlpha = config.color.a;
        alphaKeys[0].alpha = baseAlpha * alphaMul;
        alphaKeys[1].alpha = baseAlpha * alphaMul;
        alphaKeys[0].time = 0f;
        alphaKeys[1].time = 1f;

        gradient.SetKeys(colorKeys, alphaKeys);
        lr.colorGradient = gradient;
        lr.widthMultiplier = widthMul;

        bounds.center = (positionsBuffer[0] + positionsBuffer[1]) * 0.5f;
        lr.bounds = bounds;
    }

    private void OnDisable()
    {
        isPlaying = false;
    }
}
