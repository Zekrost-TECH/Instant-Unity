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

    private ObjectPool<GameObject> pool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(beamPrefab),
            actionOnGet: go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => Destroy(go),
            collectionCheck: false,
            defaultCapacity: poolCapacity,
            maxSize: poolMaxSize
        );
    }

    public void SpawnBeam(Transform origin, Vector3 hitPoint)
    {
        if (pool == null || beamPrefab == null || origin == null) return;

        GameObject beam = pool.Get();
        beam.transform.SetParent(null, true);
        beam.transform.position = Vector3.zero;
        beam.transform.rotation = Quaternion.identity;

        HitBeam hitBeam = beam.GetComponent<HitBeam>();
        if (hitBeam != null)
            hitBeam.Play(origin, hitPoint);
    }

    internal void ReturnToPool(GameObject go)
    {
        if (pool != null) pool.Release(go);
    }
}
