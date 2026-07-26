using UnityEngine;

/// <summary>
/// Encaja un RectTransform dentro del área segura de la pantalla. El proyecto tiene
/// androidRenderOutsideSafeArea activo (se dibuja bajo el notch), así que sin esto los
/// controles de las esquinas quedan tapados por el recorte o la barra de gestos.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("Aplica el margen izquierdo/derecho del área segura (notch en horizontal).")]
    public bool applyHorizontal = true;
    [Tooltip("Aplica el margen superior/inferior (barra de gestos).")]
    public bool applyVertical = true;

    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        // La rotación y el tamaño cambian en caliente; barato de comprobar.
        if (Screen.safeArea == lastSafeArea &&
            Screen.width == lastScreenSize.x && Screen.height == lastScreenSize.y &&
            Screen.orientation == lastOrientation)
            return;

        Apply();
    }

    private void Apply()
    {
        if (rect == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;
        lastSafeArea = safe;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;

        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        if (!applyHorizontal) { min.x = 0f; max.x = 1f; }
        if (!applyVertical) { min.y = 0f; max.y = 1f; }

        // Si el cálculo sale degenerado (puede pasar en el primer frame) no lo aplicamos:
        // dejaría el rect a tamaño cero y la UI desaparecería.
        if (max.x - min.x <= 0f || max.y - min.y <= 0f) return;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
