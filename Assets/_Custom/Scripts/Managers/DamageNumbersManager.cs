using DamageNumbersPro;
using UnityEngine;

public class DamageNumbersManager : MonoBehaviour
{
    public static DamageNumbersManager Instance { get; private set; }

    [Header("World Popups")]
    [SerializeField] private DamageNumberGUI enemyDamagePrefab;
    [SerializeField] private DamageNumberGUI playerDamagePrefab;
    [SerializeField] private RectTransform worldPopupParent;
    [SerializeField] private Vector3 enemyDamageOffset = new Vector3(0f, 0.45f, -1f);
    [SerializeField] private Vector3 playerDamageOffset = new Vector3(0f, 0.7f, -1f);
    [SerializeField] private Color enemyDamageColor = new Color(1f, 0.9f, 0.25f);
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.2f, 0.2f);

    [Header("Time Popup")]
    [SerializeField] private DamageNumberGUI timeChangePrefab;
    [SerializeField] private RectTransform timePopupParent;
    [SerializeField] private RectTransform timePopupAnchor;
    [SerializeField] private Vector2 timePopupOffset = new Vector2(0f, -150f);
    [SerializeField] private float timePopupScale = 0.85f;
    [SerializeField] private Color timeGainColor = new Color(0.2f, 1f, 0.45f);
    [SerializeField] private Color timeLossColor = new Color(1f, 0.2f, 0.2f);

    private Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (worldPopupParent == null)
            worldPopupParent = timePopupParent;

        if (TimeManager.Instance != null)
            TimeManager.Instance.OnTimeAdjusted += HandleTimeAdjusted;

        enemyDamagePrefab?.PrewarmPool();
        playerDamagePrefab?.PrewarmPool();
        timeChangePrefab?.PrewarmPool();
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnTimeAdjusted -= HandleTimeAdjusted;

        if (Instance == this)
            Instance = null;
    }

    public void SpawnEnemyDamage(Vector3 position, float amount)
    {
        SpawnWorldDamage(enemyDamagePrefab, position + enemyDamageOffset, amount, enemyDamageColor);
    }

    public void SpawnPlayerDamage(Vector3 position, float amount)
    {
        SpawnWorldDamage(playerDamagePrefab, position + playerDamageOffset, amount, playerDamageColor);
    }

    private void SpawnWorldDamage(DamageNumberGUI prefab, Vector3 worldPosition, float amount, Color color)
    {
        if (prefab == null || worldPopupParent == null || amount <= 0f)
            return;

        Camera camera = mainCamera != null ? mainCamera : Camera.main;
        if (camera == null)
            return;

        Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(worldPopupParent, screenPosition, null, out Vector2 anchoredPosition))
            return;

        DamageNumber popup = prefab.SpawnGUI(worldPopupParent, anchoredPosition, amount);
        popup.position = popup.GetPosition();
        popup.FadeIn();
        ApplyGradient(popup, color);
        popup.UpdateText();
    }

    private void HandleTimeAdjusted(float delta)
    {
        if (timeChangePrefab == null || timePopupParent == null || Mathf.Abs(delta) < 0.001f)
            return;

        RectTransform anchor = timePopupAnchor != null ? timePopupAnchor : timePopupParent;
        string sign = delta > 0f ? "+" : "-";
        string message = $"{sign}{Mathf.Abs(delta):0.0}s";
        DamageNumber popup = timeChangePrefab.SpawnGUI(timePopupParent, anchor, timePopupOffset, message);
        popup.position = popup.GetPosition();
        popup.FadeIn();
        ApplyGradient(popup, delta > 0f ? timeGainColor : timeLossColor);
        popup.SetScale(timePopupScale);
        popup.UpdateText();
    }

    private static void ApplyGradient(DamageNumber popup, Color baseColor)
    {
        Color highlight = Color.Lerp(baseColor, Color.white, 0.55f);
        Color shadow = Color.Lerp(baseColor, Color.black, 0.35f);
        popup.SetGradientColor(highlight, highlight, shadow, shadow);
    }
}
