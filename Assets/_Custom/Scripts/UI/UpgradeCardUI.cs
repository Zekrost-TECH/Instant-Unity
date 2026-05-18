using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System;

public class UpgradeCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage;
    [Tooltip("El contenedor visual del reverso de la carta.")]
    public GameObject cardBack; 
    [Tooltip("El contenedor visual del frente de la carta.")]
    public GameObject cardFront; 

    [Header("Balatro Tilt Effect")]
    public float maxTiltAngle = 15f;
    public float tiltSmoothness = 10f;
    public float hoverScale = 1.1f;
    
    private RectTransform rectTransform;
    private Quaternion targetRotation;
    private Vector3 targetScale;
    private bool isHovered = false;

    private UpgradeData assignedUpgrade;
    private Action<UpgradeData> onCardSelected;
    
    private bool isInteractable = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetRotation = rectTransform.localRotation;
        targetScale = Vector3.one;
        
        if (cardFront != null) cardFront.SetActive(false);
        if (cardBack != null) cardBack.SetActive(true);
    }

    private void Update()
    {
        if (isInteractable)
        {
            if (!isHovered)
            {
                targetRotation = Quaternion.identity;
                targetScale = Vector3.one;
            }
            rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, targetRotation, Time.unscaledDeltaTime * tiltSmoothness);
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * tiltSmoothness);
        }
    }

    public void Setup(UpgradeData upgrade, Action<UpgradeData> onSelected, float delayBeforeEntry)
    {
        assignedUpgrade = upgrade;
        onCardSelected = onSelected;
        
        if (titleText != null) titleText.text = upgrade.title;
        if (descriptionText != null) descriptionText.text = upgrade.description;
        if (iconImage != null && upgrade.icon != null) iconImage.sprite = upgrade.icon;
        
        isInteractable = false;
        
        StartCoroutine(EntryAnimation(delayBeforeEntry));
    }

    private IEnumerator EntryAnimation(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        // 1. Slide In Animation
        Vector2 endPos = rectTransform.anchoredPosition;
        // Iniciar fuera de la pantalla (a la derecha)
        Vector2 startPos = new Vector2(Screen.width + rectTransform.rect.width, endPos.y);
        
        rectTransform.anchoredPosition = startPos;
        float duration = 0.5f;
        float elapsed = 0f;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDashSFX, 0.5f); // Placeholder sound
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        rectTransform.anchoredPosition = endPos;

        // 2. Delay Extra si es Rara (Genera anticipación)
        if (assignedUpgrade.isRare)
        {
            yield return new WaitForSecondsRealtime(0.6f);
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.15f);
        }

        // 3. Flip Animation (Mitad 1: rotar a 90 grados en Y)
        duration = 0.3f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rectTransform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 90f, t), 0f);
            yield return null;
        }

        // Cambiar gráficos
        if (cardBack != null) cardBack.SetActive(false);
        if (cardFront != null) cardFront.SetActive(true);

        // 3. Flip Animation (Mitad 2: rotar de 90 a 0 grados)
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rectTransform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(90f, 0f, t), 0f);
            yield return null;
        }

        rectTransform.localRotation = Quaternion.identity;
        isInteractable = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;
        isHovered = true;
        targetScale = Vector3.one * hoverScale;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSelectSFX, 0.3f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;
        isHovered = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isInteractable || !isHovered) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // Evitar división por cero si el layout no ha terminado de calcularse
        if (width <= 0f || height <= 0f) return;

        // Calcular porcentaje (-0.5 a 0.5 desde el centro)
        float xPercent = localPoint.x / width;
        float yPercent = localPoint.y / height;

        // Validar que no haya valores indeterminados (NaN o Infinity)
        if (float.IsNaN(xPercent) || float.IsInfinity(xPercent) || float.IsNaN(yPercent) || float.IsInfinity(yPercent)) 
            return;

        // Invertir ejes para que la carta se incline de forma natural hacia el ratón
        float xAngle = yPercent * maxTiltAngle; 
        float yAngle = -xPercent * maxTiltAngle;

        targetRotation = Quaternion.Euler(xAngle, yAngle, 0f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSelectSFX, 1f);
        }
        
        onCardSelected?.Invoke(assignedUpgrade);
    }
}
