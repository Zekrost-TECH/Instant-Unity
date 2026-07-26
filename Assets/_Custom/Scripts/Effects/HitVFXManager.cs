using UnityEngine;
using UnityEngine.Pool;

public class HitVFXManager : MonoBehaviour
{
    public static HitVFXManager Instance { get; private set; }

    [Header("Prefab")]
    public GameObject beamPrefab;

    [Header("Pool")]
    [SerializeField] private int poolCapacity = 20;
    [SerializeField] private int poolMaxSize = 40;

    [Header("Beam Defaults")]
    [ColorUsage(true, true)] public Color color = new Color(0.27f, 0.78f, 1.0f) * 3f;
    public float lifetime = 0.12f;
    public float startWidth = 0.18f;
    public float endWidth = 0.04f;
    public string sortingLayerName = "Default";
    public int sortingOrder = 100;
    public AnimationCurve widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private ObjectPool<HitBeam> pool;
    private Transform beamContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Solo el componente: los managers comparten el GameObject "Managers" de 1_Game,
            // y Destroy(gameObject) se llevaria por delante a todos los demas.
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Los beams instanciados viven bajo un contenedor persistente. Sin esto se
        // destruyen al cambiar de escena y el pool devuelve referencias muertas.
        beamContainer = new GameObject("HitBeamPool").transform;
        beamContainer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        DontDestroyOnLoad(beamContainer.gameObject);

        pool = new ObjectPool<HitBeam>(
            createFunc: CreateBeam,
            actionOnGet: beam => beam.gameObject.SetActive(true),
            actionOnRelease: beam => beam.gameObject.SetActive(false),
            actionOnDestroy: beam => { if (beam != null) Destroy(beam.gameObject); },
            collectionCheck: false,
            defaultCapacity: poolCapacity,
            maxSize: poolMaxSize
        );
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        pool?.Clear();
        if (beamContainer != null) Destroy(beamContainer.gameObject);
        Instance = null;
    }

    private HitBeam CreateBeam()
    {
        GameObject instance = Instantiate(beamPrefab, beamContainer);
        HitBeam beam = instance.GetComponent<HitBeam>();
        if (beam == null) beam = instance.AddComponent<HitBeam>();
        return beam;
    }

    public void SpawnBeam(Transform origin, Vector3 hitPoint)
    {
        if (pool == null || beamPrefab == null || origin == null) return;

        HitBeam beam = pool.Get();
        if (beam == null) return;

        beam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        beam.Play(origin, hitPoint);
    }

    internal void ReturnToPool(HitBeam beam)
    {
        if (pool != null && beam != null) pool.Release(beam);
    }
}
