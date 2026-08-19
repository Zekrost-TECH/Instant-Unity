using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using MoreMountains.Tools;

public class DashButtonController : MonoBehaviour
{
    [Header("Visuals")]
    public Image cooldownRing;
    public Image buttonImage;
    public Image dashIcon;
    public Color readyColor = Color.blue;
    public Color cooldownColor = Color.gray;

    [Header("Binding")]
    public MMTouchButton touchButton;

    [Header("Events")]
    public UnityEvent OnDashRequested;

    private PlayerMovement playerMovement;
    private PlayerInput playerInput;
    private TooltipController tooltipController;
    private bool tooltipShown = false;
    private float lastFill = -1f;
    private bool lastReadyState = false;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerInput = player.GetComponent<PlayerInput>();
        }

        tooltipController = FindAnyObjectByType<TooltipController>();

        if (cooldownRing != null)
            cooldownRing.fillAmount = 1f;
    }

    private void Update()
    {
        if (playerMovement == null) return;

        float cooldownRatio = playerMovement.DashCooldownRatio;
        bool wasReady = cooldownRatio >= 1f;

        // Escribir en un Graphic marca el Canvas como dirty: sólo lo hacemos si el valor cambió.
        if (cooldownRing != null && !Mathf.Approximately(cooldownRatio, lastFill))
        {
            lastFill = cooldownRatio;
            cooldownRing.fillAmount = cooldownRatio;
        }

        if (wasReady != lastReadyState)
        {
            lastReadyState = wasReady;

            if (buttonImage != null)
                buttonImage.color = wasReady ? readyColor : cooldownColor;

            // MMTouchButton sólo puede recibir taps cuando está listo (Off/ButtonUp).
            if (touchButton != null)
            {
                if (wasReady) touchButton.EnableButton();
                else touchButton.DisableButton();
            }

            if (dashIcon != null)
            {
                Color iconColor = dashIcon.color;
                iconColor.a = wasReady ? 1f : 0.45f;
                dashIcon.color = iconColor;
            }
        }

        if (wasReady && !tooltipShown && tooltipController != null)
        {
            tooltipController.ShowDashTooltip();
            tooltipShown = true;
        }
    }

    public void OnDashButtonPressed()
    {
        // El botón no debe ejecutar dashes mientras el cooldown no ha terminado.
        if (playerMovement != null && playerMovement.DashCooldownRemaining > 0f)
            return;

        OnDashRequested?.Invoke();
        playerInput?.TriggerDash();
    }
}
