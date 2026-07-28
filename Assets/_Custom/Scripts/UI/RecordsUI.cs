using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordsUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject recordsPanel;
    public Button closeButton;

    [Header("Valores")]
    public TextMeshProUGUI bestTimeText;
    public TextMeshProUGUI bestKillsText;
    public TextMeshProUGUI cronosText;

    private bool openRequested = false;

    public bool IsOpen => recordsPanel != null && recordsPanel.activeSelf;

    private void Awake()
    {
        SaveManager.Ensure();
    }

    private void Start()
    {
        // Si el componente vive en el propio panel, Start corre justo DESPUÉS de que
        // Open() lo active: cerrar a ciegas aquí lo volvería a ocultar.
        if (recordsPanel != null && !openRequested) recordsPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        openRequested = true;
        SaveManager.Ensure();

        Refresh();
        if (recordsPanel != null) recordsPanel.SetActive(true);
    }

    public void Close()
    {
        openRequested = false;
        if (recordsPanel != null) recordsPanel.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    private void Refresh()
    {
        if (SaveManager.Instance == null) return;

        if (bestTimeText != null)
            bestTimeText.SetText("{0:1}s", SaveManager.Instance.BestTime);

        if (bestKillsText != null)
            bestKillsText.SetText("{0}", SaveManager.Instance.BestKills);

        if (cronosText != null)
            cronosText.SetText("{0}", SaveManager.Instance.Cronos);
    }
}
