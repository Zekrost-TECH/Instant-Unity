using System;
using UnityEngine;

public enum SkinTarget
{
    Player,
    EnemySet
}

public class SkinManager : MonoBehaviour
{
    public static SkinManager Instance { get; private set; }

    public event Action OnPlayerSkinChanged;
    public event Action OnEnemySetChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void EquipSkin(string skinId, SkinTarget target)
    {
        if (SaveManager.Instance == null) return;

        if (target == SkinTarget.Player)
        {
            SaveManager.Instance.EquipSkin(skinId);
            OnPlayerSkinChanged?.Invoke();
        }
        else if (target == SkinTarget.EnemySet)
        {
            SaveManager.Instance.EquipEnemySet(skinId);
            OnEnemySetChanged?.Invoke();
        }
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
        if (SaveManager.Instance == null) return false;
        return SaveManager.Instance.IsSkinUnlocked(skinId);
    }
}
