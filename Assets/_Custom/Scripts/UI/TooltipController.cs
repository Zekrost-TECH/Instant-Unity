using System.Collections;
using UnityEngine;
using TMPro;

public class TooltipController : MonoBehaviour
{
    [Header("Tooltip UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public RectTransform arrowTransform;

    [Header("Tooltip Targets")]
    public RectTransform timeTextTarget;
    public RectTransform dashButtonTarget;

    [Header("Tooltip Data")]
    public string tooltip1Text = "TU VIDA";
    public string tooltip2Text = "MÁTALOS PARA GANAR TIEMPO";
    public string tooltip3Text = "DASH — INVULNERABLE";
    public float tooltipDuration = 2f;

    private int currentTooltip = 0;
    private bool isShowing = false;
    private bool firstRun = false;

    private void Start()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        if (SaveManager.Instance != null)
            firstRun = SaveManager.Instance.IsFirstTime();
        else
            firstRun = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnKillCountChanged += HandleKillCountChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnKillCountChanged -= HandleKillCountChanged;
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (firstRun && state == GameManager.GameState.Playing && currentTooltip == 0 && !isShowing)
        {
            StartCoroutine(ShowTooltipSequence());
        }
    }

    private void HandleKillCountChanged(int kills)
    {
        if (firstRun && kills >= 1 && currentTooltip == 1 && !isShowing)
        {
            StartCoroutine(ShowTooltip2());
        }
    }

    private IEnumerator ShowTooltipSequence()
    {
        isShowing = true;

        // Tooltip 1: apunta al reloj
        ShowTooltip(tooltip1Text, timeTextTarget);
        yield return StartCoroutine(WaitTooltipDuration());
        HideTooltip();
        currentTooltip = 1;
        isShowing = false;
    }

    private IEnumerator ShowTooltip2()
    {
        isShowing = true;
        ShowTooltip(tooltip2Text, null);
        yield return StartCoroutine(WaitTooltipDuration());
        HideTooltip();
        currentTooltip = 2;
        isShowing = false;
    }

    public void ShowDashTooltip()
    {
        if (firstRun && currentTooltip == 2 && !isShowing)
        {
            StartCoroutine(ShowTooltip3());
        }
    }

    private IEnumerator ShowTooltip3()
    {
        isShowing = true;
        ShowTooltip(tooltip3Text, dashButtonTarget);
        yield return StartCoroutine(WaitTooltipDuration());
        HideTooltip();
        currentTooltip = 3;
        isShowing = false;

        // Finalizar onboarding
        SaveManager.Instance?.SetFirstTimePlayed();
        firstRun = false;
    }

    private IEnumerator WaitTooltipDuration()
    {
        float elapsed = 0f;
        while (elapsed < tooltipDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ShowTooltip(string text, RectTransform target)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);

        if (arrowTransform != null && target != null)
        {
            arrowTransform.position = target.position;
        }
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}
