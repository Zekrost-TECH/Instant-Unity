using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HitBeam : MonoBehaviour
{
    private LineRenderer lr;
    private Coroutine running;
    private GradientColorKey[] colorKeys;
    private GradientAlphaKey[] alphaKeys;
    private Vector3[] positionsBuffer;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        colorKeys = new GradientColorKey[2];
        alphaKeys = new GradientAlphaKey[2];
        positionsBuffer = new Vector3[2];
    }

    public void Play(Transform origin, Vector3 hitPoint)
    {
        HitVFXManager config = HitVFXManager.Instance;
        if (config == null || lr == null) return;

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        positionsBuffer[0] = origin.position;
        positionsBuffer[1] = hitPoint;
        lr.SetPositions(positionsBuffer);

        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, config.startWidth),
            new Keyframe(1f, config.endWidth)
        );
        lr.widthMultiplier = 1f;
        lr.sortingLayerName = config.sortingLayerName;
        lr.sortingOrder = config.sortingOrder;

        lr.startColor = config.color;
        lr.endColor = config.color;

        Gradient gradient = new Gradient();
        colorKeys[0] = new GradientColorKey(config.color, 0f);
        colorKeys[1] = new GradientColorKey(config.color, 1f);
        alphaKeys[0] = new GradientAlphaKey(config.color.a, 0f);
        alphaKeys[1] = new GradientAlphaKey(config.color.a, 1f);
        gradient.SetKeys(colorKeys, alphaKeys);
        lr.colorGradient = gradient;

        lr.bounds = new Bounds((positionsBuffer[0] + positionsBuffer[1]) * 0.5f, new Vector3(20f, 20f, 20f));
        lr.Simplify(0.01f);

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FadeAndRelease(config, gradient));
    }

    private IEnumerator FadeAndRelease(HitVFXManager config, Gradient gradient)
    {
        float t = 0f;
        float duration = Mathf.Max(0.001f, config.lifetime);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            float widthMul = config.widthCurve != null && config.widthCurve.length > 0
                ? config.widthCurve.Evaluate(k)
                : 1f - k;

            float alphaMul = config.alphaCurve != null && config.alphaCurve.length > 0
                ? config.alphaCurve.Evaluate(k)
                : 1f - k;

            float baseAlpha = config.color.a;
            colorKeys[0].color = config.color;
            colorKeys[1].color = config.color;
            alphaKeys[0].alpha = baseAlpha * alphaMul;
            alphaKeys[1].alpha = baseAlpha * alphaMul;
            gradient.SetKeys(colorKeys, alphaKeys);
            lr.colorGradient = gradient;

            lr.widthMultiplier = widthMul;
            lr.bounds = new Bounds((positionsBuffer[0] + positionsBuffer[1]) * 0.5f, new Vector3(20f, 20f, 20f));

            yield return null;
        }

        HitVFXManager.Instance.ReturnToPool(gameObject);
    }
}
