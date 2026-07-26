using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIManager : MonoBehaviour
{
    [Header("References")]
    public GameObject upgradeCanvasPanel;
    public Transform cardsContainer;
    public UpgradeCardUI cardPrefab;
    public Image overlayImage;
    public GameObject progressBarContainer;
    public TextMeshProUGUI titleText;

    private List<UpgradeCardUI> activeCards = new List<UpgradeCardUI>();

    private void Start()
    {
        if (upgradeCanvasPanel != null)
        {
            upgradeCanvasPanel.SetActive(false);
        }

        if (progressBarContainer != null)
        {
            progressBarContainer.SetActive(false);
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeWindowOpened += HandleUpgradeWindowOpened;
            UpgradeManager.Instance.OnUpgradeWindowClosed += HandleUpgradeWindowClosed;
        }
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeWindowOpened -= HandleUpgradeWindowOpened;
            UpgradeManager.Instance.OnUpgradeWindowClosed -= HandleUpgradeWindowClosed;
        }
    }

    private void HandleUpgradeWindowOpened(List<UpgradeData> options)
    {
        if (upgradeCanvasPanel != null)
        {
            upgradeCanvasPanel.SetActive(true);
        }

        if (titleText != null)
            titleText.text = "CHOOSE AN UPGRADE";

        if (progressBarContainer != null)
        {
            progressBarContainer.SetActive(false);
        }

        // Limpiar cartas anteriores si las hubiera
        ClearCards();

        if (cardPrefab == null || cardsContainer == null || options == null) return;

        // Crear nuevas cartas con un retraso progresivo para la animación
        float delay = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            UpgradeCardUI newCard = Instantiate(cardPrefab, cardsContainer);
            newCard.Setup(options[i], OnCardSelected, delay);
            activeCards.Add(newCard);

            delay += 0.25f; // Retraso escalonado entre cada carta
        }
    }

    private void HandleUpgradeWindowClosed()
    {
        if (upgradeCanvasPanel != null)
        {
            upgradeCanvasPanel.SetActive(false);
        }

        // Las cartas se liberan al cerrar, no en la siguiente apertura: así no quedan
        // GameObjects (con sus corrutinas) colgando en la jerarquía durante la partida.
        ClearCards();
    }

    private void ClearCards()
    {
        for (int i = 0; i < activeCards.Count; i++)
        {
            if (activeCards[i] != null) Destroy(activeCards[i].gameObject);
        }
        activeCards.Clear();
    }

    private void OnCardSelected(UpgradeData selectedUpgrade)
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ApplyUpgrade(selectedUpgrade);
        }
    }
}
