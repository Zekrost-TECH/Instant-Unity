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
            titleText.text = "ELIGE UN UPGRADE";

        if (progressBarContainer != null)
        {
            progressBarContainer.SetActive(false);
        }

        // Limpiar cartas anteriores si las hubiera
        foreach (var card in activeCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        activeCards.Clear();

        // Crear nuevas cartas con un retraso progresivo para la animación
        float delay = 0f;
        foreach (var option in options)
        {
            UpgradeCardUI newCard = Instantiate(cardPrefab, cardsContainer);
            newCard.Setup(option, OnCardSelected, delay);
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
    }

    private void OnCardSelected(UpgradeData selectedUpgrade)
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ApplyUpgrade(selectedUpgrade);
        }
    }
}
