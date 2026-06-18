using UnityEngine;

public class SkinRenderer : MonoBehaviour
{
    [Header("Skin Target")]
    public SkinTarget skinTarget = SkinTarget.Player;

    [Header("Renderers")]
    [Tooltip("SpriteRenderer que muestra la skin de Kenney. Desactivado por defecto.")]
    public SpriteRenderer skinSpriteRenderer;

    [Tooltip("Renderer de la figura geométrica base. Activado por defecto.")]
    public Renderer geometryRenderer;

    [Tooltip("Nombre de la skin por defecto si no hay equipada.")]
    public string defaultSkinId = "Cyan";

    private void Start()
    {
        ApplySkin();

        if (SkinManager.Instance != null)
        {
            SkinManager.Instance.OnPlayerSkinChanged += ApplySkin;
            SkinManager.Instance.OnEnemySetChanged += ApplySkin;
        }
    }

    private void OnDestroy()
    {
        if (SkinManager.Instance != null)
        {
            SkinManager.Instance.OnPlayerSkinChanged -= ApplySkin;
            SkinManager.Instance.OnEnemySetChanged -= ApplySkin;
        }
    }

    public void ApplySkin()
    {
        string equippedSkin = GetEquippedSkinId();
        bool hasSkin = HasSkinSprite(equippedSkin);

        if (skinSpriteRenderer != null)
            skinSpriteRenderer.enabled = hasSkin;

        if (geometryRenderer != null)
            geometryRenderer.enabled = !hasSkin;
    }

    private string GetEquippedSkinId()
    {
        if (SkinManager.Instance != null)
            return SkinManager.Instance.GetEquippedSkin(skinTarget);
        return defaultSkinId;
    }

    private bool HasSkinSprite(string skinId)
    {
        // Si hay un Sprite asignado en el SpriteRenderer, consideramos que la skin está activa.
        return skinSpriteRenderer != null && skinSpriteRenderer.sprite != null;
    }

    public void SetSkinSprite(Sprite sprite)
    {
        if (skinSpriteRenderer != null)
        {
            skinSpriteRenderer.sprite = sprite;
            ApplySkin();
        }
    }
}
