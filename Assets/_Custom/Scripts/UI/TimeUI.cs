using UnityEngine;
using TMPro;
using System.Diagnostics;

public class TimeUI : MonoBehaviour
{
    [Header("UI Component")]
    [Tooltip("Referencia al TextMeshPro del reloj en pantalla.")]
    private TextMeshProUGUI timeText;

    private void Start()
    {
        timeText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (TimeManager.Instance != null && timeText != null)
        {
            // Mostramos el tiempo sin decimales, redondeándolo hacia arriba
            timeText.text = Mathf.CeilToInt(TimeManager.Instance.CurrentTime).ToString(); 
        }
    }
}
