using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick visuals")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public float joystickRadius = 60f;
    public float deadZone = 8f;
    public float fadeWhenUnused = 0.4f;

    public Vector2 InputDirection { get; private set; }
    public bool IsDragging { get; private set; }

    private Vector2 joystickCenter;
    private Vector2 originalBackgroundPosition;

    private void Start()
    {
        if (joystickBackground != null)
            originalBackgroundPosition = joystickBackground.anchoredPosition;

        InputDirection = Vector2.zero;
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsDragging = true;

        if (joystickBackground != null)
        {
            joystickCenter = eventData.position;
            joystickBackground.position = joystickCenter;
        }

        UpdateHandle(eventData.position);
        UpdateVisuals();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;
        UpdateHandle(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;
        InputDirection = Vector2.zero;

        if (joystickBackground != null)
            joystickBackground.anchoredPosition = originalBackgroundPosition;

        UpdateHandle(joystickCenter);
        UpdateVisuals();
    }

    private void UpdateHandle(Vector2 pointerPosition)
    {
        if (joystickBackground == null || joystickHandle == null) return;

        Vector2 direction = pointerPosition - joystickCenter;
        float distance = direction.magnitude;

        if (distance > deadZone)
        {
            Vector2 normalized = direction.normalized;
            float handleDistance = Mathf.Min(distance, joystickRadius);
            joystickHandle.anchoredPosition = normalized * handleDistance;
            InputDirection = normalized * (handleDistance / joystickRadius);
        }
        else
        {
            joystickHandle.anchoredPosition = Vector2.zero;
            InputDirection = Vector2.zero;
        }
    }

    private void UpdateVisuals()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = IsDragging ? 1f : fadeWhenUnused;
        }
    }
}
