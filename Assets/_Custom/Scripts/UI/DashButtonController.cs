using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DashButtonController : MonoBehaviour
{
    [Header("Visuals")]
    public Image cooldownRing;
    public Image buttonImage;
    public Color readyColor = Color.blue;
    public Color cooldownColor = Color.gray;

    [Header("Events")]
    public UnityEvent OnDashRequested;

    private PlayerMovement playerMovement;
    private PlayerInput playerInput;
    private TooltipController tooltipController;
    private bool tooltipShown = false;

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

        if (cooldownRing != null)
            cooldownRing.fillAmount = cooldownRatio;

        if (buttonImage != null)
            buttonImage.color = wasReady ? readyColor : cooldownColor;

        if (wasReady && !tooltipShown && tooltipController != null)
        {
            tooltipController.ShowDashTooltip();
            tooltipShown = true;
        }
    }

    public void OnDashButtonPressed()
    {
        OnDashRequested?.Invoke();
        playerInput?.TriggerDash();
    }
}
