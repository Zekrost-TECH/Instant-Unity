using System.Collections;
using UnityEngine;
using TMPro;

public class TooltipController : MonoBehaviour
{
    [Header("Tooltip UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    [Tooltip("Flecha con el sprite apuntando hacia arriba; se rota hacia el objetivo.")]
    public RectTransform arrowTransform;

    [Header("Tooltip Targets")]
    public RectTransform timeTextTarget;
    public RectTransform dashButtonTarget;

    [Header("Tooltip Data")]
    public string tooltip1Text = "YOUR LIFE";
    public string tooltip2Text = "KILL THEM TO GAIN TIME";
    public string tooltip3Text = "DASH — INVULNERABLE";
    public float tooltipDuration = 2f;
    [Tooltip("Hueco en píxeles de referencia entre el borde del objetivo y el cartel.")]
    public float panelDistance = 120f;

    private RectTransform panelRect;
    private PlayerMovement playerMovement;
    private Camera mainCamera;
    private Coroutine sequence;

    private void Start()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            panelRect = tooltipPanel.transform as RectTransform;
        }

        mainCamera = Camera.main;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerMovement = player.GetComponent<PlayerMovement>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            // La partida arranca desde sceneLoaded, antes de este Start: el paso a
            // Playing ya ocurrió y el evento no volverá a llegar.
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
        {
            if (sequence != null) StopCoroutine(sequence);
            HideTooltip();
            return;
        }

        if (state != GameManager.GameState.Playing || sequence != null) return;

        bool firstRun = SaveManager.Instance == null || SaveManager.Instance.IsFirstTime();
        if (firstRun) sequence = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        yield return ShowPaused(tooltip1Text, timeTextTarget);
        yield return new WaitUntil(AnyEnemyOnScreen);
        yield return ShowPaused(tooltip2Text, null);
        yield return new WaitUntil(IsDashReady);
        yield return ShowPaused(tooltip3Text, dashButtonTarget);

        SaveManager.Instance?.SetFirstTimePlayed();
    }

    /// <summary>
    /// Pausa el juego (estado Paused: reloj, enemigos y spawn congelados), muestra el
    /// cartel y reanuda solo tras tooltipDuration segundos reales, sin input del jugador.
    /// </summary>
    private IEnumerator ShowPaused(string text, RectTransform target)
    {
        // Si hay una ventana de upgrade abierta se espera a que cierre.
        yield return new WaitUntil(IsPlaying);

        GameManager.Instance?.PauseGame();
        ShowTooltip(text, target);
        yield return new WaitForSecondsRealtime(tooltipDuration);
        HideTooltip();

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            GameManager.Instance.ResumeGame();
    }

    private bool IsPlaying()
    {
        return GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing;
    }

    private bool IsDashReady()
    {
        return IsPlaying() && (playerMovement == null || playerMovement.DashCooldownRatio >= 1f);
    }

    private bool AnyEnemyOnScreen()
    {
        if (!IsPlaying() || EnemyManager.Instance == null) return false;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return false;

        var enemies = EnemyManager.Instance.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            Vector3 viewport = mainCamera.WorldToViewportPoint(enemies[i].transform.position);
            if (viewport.x > 0.05f && viewport.x < 0.95f && viewport.y > 0.05f && viewport.y < 0.95f)
                return true;
        }
        return false;
    }

    private void ShowTooltip(string text, RectTransform target)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);

        bool pointed = target != null && arrowTransform != null;
        if (arrowTransform != null) arrowTransform.gameObject.SetActive(pointed);
        if (panelRect == null) return;

        // Canvas en Screen Space Overlay: la posición de mundo es la de pantalla.
        Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        if (!pointed)
        {
            panelRect.position = center;
            return;
        }

        Vector3 targetPosition = target.TransformPoint(target.rect.center);
        Vector3 toCenter = (center - targetPosition).normalized;
        float scale = panelRect.lossyScale.x;

        // Todo en píxeles de pantalla: el objetivo puede vivir en otro Canvas con otra escala.
        // Media extensión de cada rect en la dirección de la flecha (un reloj ancho y bajo
        // apenas ocupa hacia abajo; un botón redondo ocupa lo mismo en diagonal).
        float targetRadius = HalfExtent(target, toCenter) * target.lossyScale.x;
        float panelExtent = HalfExtent(panelRect, toCenter) * scale;
        float gap = panelDistance * scale;

        arrowTransform.position = targetPosition + toCenter * (targetRadius + gap * 0.35f);
        panelRect.position = targetPosition + toCenter * (targetRadius + gap + panelExtent);

        float angle = Mathf.Atan2(-toCenter.y, -toCenter.x) * Mathf.Rad2Deg;
        arrowTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private static float HalfExtent(RectTransform rect, Vector3 direction)
    {
        return (Mathf.Abs(direction.x) * rect.rect.width + Mathf.Abs(direction.y) * rect.rect.height) * 0.5f;
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        if (arrowTransform != null)
            arrowTransform.gameObject.SetActive(false);
    }
}
