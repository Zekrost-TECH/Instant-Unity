using UnityEngine;
using TMPro;

public class TimeUI : MonoBehaviour
{
    private TextMeshProUGUI timeText;
    private int lastDisplayedSeconds = int.MinValue;

    private void Awake()
    {
        timeText = GetComponent<TextMeshProUGUI>();
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
}
