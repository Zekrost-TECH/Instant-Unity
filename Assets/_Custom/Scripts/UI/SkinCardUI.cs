using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image background;
    public Image preview;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statusText;
    public Button selectButton;
    public GameObject equippedBadge;
    public GameObject lockedBadge;

    [Header("Estados")]
    public Color equippedBorder = new Color(0.27f, 0.85f, 1f);
    public Color ownedBorder = new Color(1f, 1f, 1f, 0.35f);
    public Color lockedBorder = new Color(1f, 1f, 1f, 0.12f);
    public Color affordablePrice = new Color(1f, 0.84f, 0f);
    public Color unaffordablePrice = new Color(1f, 0.35f, 0.35f);

    private SkinDefinition skin;
    private Action<SkinDefinition> onSelected;

    public string SkinId => skin != null ? skin.id : null;

    public void Setup(SkinDefinition definition, Sprite fallbackSprite, Action<SkinDefinition> onCardSelected)
    {
        skin = definition;
        onSelected = onCardSelected;

        if (nameText != null) nameText.SetText(definition.displayName);

        if (preview != null)
        {
            preview.sprite = definition.icon != null ? definition.icon : fallbackSprite;
            preview.color = definition.color;
            preview.preserveAspect = true;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClick);
            selectButton.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>Refresca precio/estado sin reconstruir la tarjeta.</summary>
    public void Refresh(bool unlocked, bool equipped, bool affordable)
    {
        if (skin == null) return;

        if (equippedBadge != null) equippedBadge.SetActive(equipped);
        if (lockedBadge != null) lockedBadge.SetActive(!unlocked);

        if (statusText != null)
        {
            if (equipped)
            {
                statusText.SetText("EQUIPPED");
                statusText.color = equippedBorder;
            }
            else if (unlocked)
            {
                statusText.SetText("EQUIP");
                statusText.color = Color.white;
            }
            else
            {
                // Sin símbolo de moneda: la fuente TMP del proyecto no tiene el glifo ⟳
                // y se dibujaba como un cuadrito.
                statusText.SetText("{0}", skin.price);
                statusText.color = affordable ? affordablePrice : unaffordablePrice;
            }
        }

        if (background != null)
            background.color = equipped ? equippedBorder : (unlocked ? ownedBorder : lockedBorder);

        if (preview != null)
        {
            Color previewColor = skin.color;
            // Las bloqueadas se muestran apagadas para leerse de un vistazo
            if (!unlocked) previewColor *= 0.45f;
            previewColor.a = 1f;
            preview.color = previewColor;
        }

        if (selectButton != null)
            selectButton.interactable = !equipped && (unlocked || affordable);
    }

    private void HandleClick()
    {
        if (skin == null) return;
        onSelected?.Invoke(skin);
    }

    private void OnDestroy()
    {
        if (selectButton != null) selectButton.onClick.RemoveListener(HandleClick);
    }
}
