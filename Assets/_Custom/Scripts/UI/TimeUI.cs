using UnityEngine;
using TMPro;

public class TimeUI : MonoBehaviour
{
    [SerializeField] private Color calmColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.67f, 0f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f);

    private TextMeshProUGUI timeText;
    private int lastDisplayedSeconds = int.MinValue;

    private void Awake()
    {
        timeText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnTimeColorChanged += UpdateColor;

        UpdateColor(TimeManager.TimeColorState.Calm);
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnTimeColorChanged -= UpdateColor;
    }

    private void Update()
    {
        if (TimeManager.Instance == null || timeText == null) return;

        // Mostramos el tiempo sin decimales, redondeándolo hacia arriba.
        // Sólo escribimos en el TMP cuando el segundo cambia (evita rebuilds y allocs por frame).
        int seconds = Mathf.CeilToInt(TimeManager.Instance.CurrentTime);
        if (seconds == lastDisplayedSeconds) return;

        lastDisplayedSeconds = seconds;
        timeText.SetText(NumberStrings.Get(seconds));
    }

    private void UpdateColor(TimeManager.TimeColorState state)
    {
        if (timeText == null) return;

        switch (state)
        {
            case TimeManager.TimeColorState.Warning:
                timeText.color = warningColor;
                break;
            case TimeManager.TimeColorState.Danger:
                timeText.color = dangerColor;
                break;
            default:
                timeText.color = calmColor;
                break;
        }
    }
}
