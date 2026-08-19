using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Joystick flotante: una zona invisible (mitad izquierda) captura el primer toque,
/// muestra base y knob en el punto tocado (clampado para que el visual no se recorte)
/// y produce RawValue a partir del arrastre relativo al ORIGEN REAL del dedo.
/// </summary>
public class FloatingJoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
{
    [Header("Joystick visuals")]
    public RectTransform background;
    public RectTransform handle;
    public CanvasGroup backgroundGroup;
    public CanvasGroup handleGroup;

    [Header("Settings")]
    public float maxRange = 78f;

    public Vector2 RawValue { get; private set; }

    private RectTransform zone;
    private int activePointerId = -1;
    private Vector2 pointerOriginLocal;
    private Vector2 visualCenterLocal;

    private void Awake()
    {
        zone = transform as RectTransform;
        RawValue = Vector2.zero;
        SetVisible(false);
        EnsureZoneInvisible();
    }

    // La zona captura toques pero no debe pintar NADA (ni un tinte sutil sobre el fondo):
    // alfa 0 real + cullTransparentMesh false mantiene el raycast sin generar píxeles.
    private void EnsureZoneInvisible()
    {
        CanvasRenderer zoneRenderer = GetComponent<CanvasRenderer>();
        if (zoneRenderer != null) zoneRenderer.cullTransparentMesh = false;

        Image zoneImage = GetComponent<Image>();
        if (zoneImage != null)
        {
            Color c = zoneImage.color;
            c.a = 0f;
            zoneImage.color = c;
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        bool playing = GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing;
        ApplyZoneInteractable(playing);
        if (!playing)
        {
            SetVisible(false);
            ResetJoystick();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void OnDisable()
    {
        ResetJoystick();
        SetVisible(false);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        bool playing = state == GameManager.GameState.Playing;
        ApplyZoneInteractable(playing);

        if (!playing)
        {
            ResetJoystick();
            SetVisible(false);
        }
    }

    private void ApplyZoneInteractable(bool interactable)
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != -1) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        activePointerId = eventData.pointerId;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position, eventData.pressEventCamera, out pointerOriginLocal))
        {
            ResetJoystick();
            return;
        }

        visualCenterLocal = ClampToZone(pointerOriginLocal);

        // El input arranca neutro aunque el centro visual se haya desplazado (bordes).
        background.localPosition = visualCenterLocal;
        handle.localPosition = Vector2.zero;
        RawValue = Vector2.zero;
        SetVisible(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        Vector2 currentLocal;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position, eventData.pressEventCamera, out currentLocal))
            return;

        Vector2 delta = currentLocal - pointerOriginLocal;
        float distance = delta.magnitude;

        if (distance > maxRange)
            delta = delta.normalized * maxRange;

        handle.localPosition = delta;
        RawValue = distance > 0.001f ? delta / maxRange : Vector2.zero;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        ResetJoystick();
        SetVisible(false);
    }

    public void ResetJoystick()
    {
        activePointerId = -1;
        RawValue = Vector2.zero;

        if (handle != null)
            handle.localPosition = Vector2.zero;
    }

    private void SetVisible(bool visible)
    {
        if (backgroundGroup != null)
        {
            backgroundGroup.alpha = visible ? 1f : 0f;
            backgroundGroup.interactable = false;
            backgroundGroup.blocksRaycasts = false;
        }

        if (handleGroup != null)
        {
            handleGroup.alpha = visible ? 1f : 0f;
            handleGroup.interactable = false;
            handleGroup.blocksRaycasts = false;
        }
    }

    private Vector2 ClampToZone(Vector2 localPoint)
    {
        if (zone == null || background == null) return localPoint;

        Rect zoneRect = zone.rect;
        Vector2 half = background.sizeDelta * 0.5f;

        float minX = zoneRect.xMin + half.x;
        float maxX = zoneRect.xMax - half.x;
        float minY = zoneRect.yMin + half.y;
        float maxY = zoneRect.yMax - half.y;

        // Si la zona fuera más pequeña que el fondo, centrar en la zona.
        if (maxX < minX) { minX = maxX = zoneRect.center.x; }
        if (maxY < minY) { minY = maxY = zoneRect.center.y; }

        return new Vector2(
            Mathf.Clamp(localPoint.x, minX, maxX),
            Mathf.Clamp(localPoint.y, minY, maxY));
    }
}
