using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkinTarget
{
    Player,
    EnemySet
}

public class SkinManager : MonoBehaviour
{
    public static SkinManager Instance { get; private set; }

    [Header("Catálogo de skins")]
    [Tooltip("Orden en el que aparecen en la tienda. La primera debería ser la gratuita.")]
    [SerializeField] private List<SkinDefinition> skins = new List<SkinDefinition>();

    public event Action OnPlayerSkinChanged;
    public event Action OnEnemySetChanged;

    /// <summary>Se dispara al comprar o equipar: la tienda y el menú se refrescan con esto.</summary>
    public event Action OnCatalogChanged;

    public IReadOnlyList<SkinDefinition> Skins => skins;

    private void Reset()
    {
        skins = BuildDefaultCatalog();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Solo el componente: los managers comparten el GameObject "Managers" de 1_Game,
            // y Destroy(gameObject) se llevaria por delante a todos los demas.
            Destroy(this);
            return;
        }
        Instance = this;

        if (skins == null || skins.Count == 0)
            skins = BuildDefaultCatalog();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Crea el SkinManager si todavía no existe. La escena 0_MainMenu no lleva los
    /// managers, así que la tienda lo levanta por su cuenta.
    /// </summary>
    public static SkinManager Ensure()
    {
        if (Instance != null) return Instance;

        // Puede existir en la escena con un catálogo configurado a mano y su Awake
        // aún no haber corrido: hay que respetarlo en vez de crear un duplicado.
        SkinManager existing = FindAnyObjectByType<SkinManager>();
        if (existing != null) return existing;

        GameObject go = new GameObject("SkinManager");
        SkinManager manager = go.AddComponent<SkinManager>();
        DontDestroyOnLoad(go);
        return manager;
    }

    private static List<SkinDefinition> BuildDefaultCatalog()
    {
        // Los ids coinciden con los que ya conocía PlayerMovement.ApplyEquippedSkin.
        return new List<SkinDefinition>
        {
            // La primaria es blanca: el sprite se tiñe multiplicando, así que el blanco
            // deja ver el arte tal cual. El id se queda en "Cyan" porque es la clave de
            // PlayerPrefs ("EquippedSkin" y el default de SaveManager).
            new SkinDefinition { id = "Cyan",   displayName = "Blanco",   color = Color.white,                   price = 0 },
            new SkinDefinition { id = "Gold",   displayName = "Oro",      color = new Color(1f, 0.84f, 0f),      price = 300 },
            new SkinDefinition { id = "Purple", displayName = "Púrpura",  color = new Color(0.65f, 0.30f, 0.95f), price = 600 },
            new SkinDefinition { id = "Red",    displayName = "Carmesí",  color = new Color(1f, 0.28f, 0.28f),   price = 1000 },
            new SkinDefinition { id = "Green",  displayName = "Esmeralda", color = new Color(0.20f, 0.92f, 0.45f), price = 1500 }
        };
    }

    // ── Consulta ─────────────────────────────────────────────────────────────

    public SkinDefinition GetSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || skins == null) return null;

        for (int i = 0; i < skins.Count; i++)
        {
            if (skins[i] != null && skins[i].id == skinId) return skins[i];
        }
        return null;
    }

    public SkinDefinition GetEquippedSkinDefinition()
    {
        SkinDefinition equipped = GetSkin(GetEquippedSkin(SkinTarget.Player));
        if (equipped != null) return equipped;

        return skins != null && skins.Count > 0 ? skins[0] : null;
    }

    public Color GetEquippedColor(Color fallback)
    {
        SkinDefinition equipped = GetEquippedSkinDefinition();
        return equipped != null ? equipped.color : fallback;
    }

    public string GetEquippedSkin(SkinTarget target)
    {
        if (SaveManager.Instance == null)
        {
            return target == SkinTarget.Player ? "Cyan" : "Default";
        }

        return target == SkinTarget.Player ? SaveManager.Instance.EquippedSkin : SaveManager.Instance.EquippedEnemySet;
    }

    public bool IsSkinUnlocked(string skinId)
    {
        SkinDefinition skin = GetSkin(skinId);
        if (skin != null && skin.IsFree) return true;

        if (SaveManager.Instance == null) return false;
        return SaveManager.Instance.IsSkinUnlocked(skinId);
    }

    public bool IsEquipped(string skinId)
    {
        return !string.IsNullOrEmpty(skinId) && GetEquippedSkin(SkinTarget.Player) == skinId;
    }

    public bool CanAfford(string skinId)
    {
        SkinDefinition skin = GetSkin(skinId);
        if (skin == null) return false;
        if (skin.IsFree) return true;

        return SaveManager.Instance != null && SaveManager.Instance.Cronos >= skin.price;
    }

    // ── Acciones ─────────────────────────────────────────────────────────────

    /// <summary>Compra la skin si hay Cronos suficientes. Devuelve false si no se pudo.</summary>
    public bool TryPurchase(string skinId)
    {
        SkinDefinition skin = GetSkin(skinId);
        if (skin == null || SaveManager.Instance == null) return false;
        if (IsSkinUnlocked(skinId)) return false;

        if (!SaveManager.Instance.UnlockSkin(skin.id, skin.price)) return false;

        OnCatalogChanged?.Invoke();
        return true;
    }

    /// <summary>Compra si hace falta y equipa. Es lo que llama la tarjeta de la tienda.</summary>
    public bool PurchaseOrEquip(string skinId)
    {
        if (!IsSkinUnlocked(skinId))
        {
            if (!TryPurchase(skinId)) return false;
        }

        EquipSkin(skinId, SkinTarget.Player);
        return true;
    }

    public void EquipSkin(string skinId, SkinTarget target)
    {
        if (SaveManager.Instance == null) return;

        if (target == SkinTarget.Player)
        {
            if (!IsSkinUnlocked(skinId)) return;

            // SaveManager.EquipSkin vuelve a comprobar su propio IsSkinUnlocked, que sólo
            // trata "Cyan" como gratis. Registramos las demás gratuitas con coste 0.
            if (!SaveManager.Instance.IsSkinUnlocked(skinId))
                SaveManager.Instance.UnlockSkin(skinId, 0);

            SaveManager.Instance.EquipSkin(skinId);
            OnPlayerSkinChanged?.Invoke();
        }
        else
        {
            SaveManager.Instance.EquipEnemySet(skinId);
            OnEnemySetChanged?.Invoke();
        }

        OnCatalogChanged?.Invoke();
    }
}
