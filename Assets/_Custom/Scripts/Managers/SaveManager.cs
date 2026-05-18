using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Player Records")]
    public float BestTime { get; private set; }
    public int BestKills { get; private set; }
    public int Cronos { get; private set; }

    [Header("Settings")]
    public float MusicVolume { get; private set; } = 0.8f;
    public float SFXVolume { get; private set; } = 0.8f;

    [Header("Passive Upgrades")]
    public int StartingTimeLevel { get; private set; } = 0; // Max 5 (+2s per level)
    public int AttackRangeLevel { get; private set; } = 0;  // Max 5 (+5% range per level)
    public int DashCooldownLevel { get; private set; } = 0; // Max 5 (-8% cooldown per level)

    [Header("Skins")]
    public string EquippedSkin { get; private set; } = "Cyan";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    public void LoadData()
    {
        BestTime = PlayerPrefs.GetFloat("BestTime", 0f);
        BestKills = PlayerPrefs.GetInt("BestKills", 0);
        Cronos = PlayerPrefs.GetInt("Cronos", 0);

        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        StartingTimeLevel = PlayerPrefs.GetInt("StartingTimeLevel", 0);
        AttackRangeLevel = PlayerPrefs.GetInt("AttackRangeLevel", 0);
        DashCooldownLevel = PlayerPrefs.GetInt("DashCooldownLevel", 0);

        EquippedSkin = PlayerPrefs.GetString("EquippedSkin", "Cyan");
    }

    public void SaveData()
    {
        PlayerPrefs.SetFloat("BestTime", BestTime);
        PlayerPrefs.SetInt("BestKills", BestKills);
        PlayerPrefs.SetInt("Cronos", Cronos);

        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);

        PlayerPrefs.SetInt("StartingTimeLevel", StartingTimeLevel);
        PlayerPrefs.SetInt("AttackRangeLevel", AttackRangeLevel);
        PlayerPrefs.SetInt("DashCooldownLevel", DashCooldownLevel);

        PlayerPrefs.SetString("EquippedSkin", EquippedSkin);

        PlayerPrefs.Save();
    }

    public void AddCronos(int amount)
    {
        Cronos += amount;
        SaveData();
    }

    public bool SpendCronos(int amount)
    {
        if (Cronos >= amount)
        {
            Cronos -= amount;
            SaveData();
            return true;
        }
        return false;
    }

    public void SetVolume(float music, float sfx)
    {
        MusicVolume = music;
        SFXVolume = sfx;
        SaveData();

        // Aplicar volumen en el AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(music, sfx);
        }
    }

    public bool UpdateRecords(float newTime, int newKills)
    {
        bool newRecord = false;
        if (newTime > BestTime)
        {
            BestTime = newTime;
            newRecord = true;
        }
        if (newKills > BestKills)
        {
            BestKills = newKills;
            newRecord = true;
        }

        if (newRecord)
        {
            SaveData();
        }
        return newRecord;
    }

    // Upgrades permanentes
    public bool UpgradeStartingTime()
    {
        int cost = (StartingTimeLevel + 1) * 150;
        if (StartingTimeLevel < 5 && SpendCronos(cost))
        {
            StartingTimeLevel++;
            SaveData();
            return true;
        }
        return false;
    }

    public bool UpgradeAttackRange()
    {
        int cost = (AttackRangeLevel + 1) * 150;
        if (AttackRangeLevel < 5 && SpendCronos(cost))
        {
            AttackRangeLevel++;
            SaveData();
            return true;
        }
        return false;
    }

    public bool UpgradeDashCooldown()
    {
        int cost = (DashCooldownLevel + 1) * 150;
        if (DashCooldownLevel < 5 && SpendCronos(cost))
        {
            DashCooldownLevel++;
            SaveData();
            return true;
        }
        return false;
    }

    // Skin Management
    public bool IsSkinUnlocked(string skinName)
    {
        if (skinName == "Cyan") return true; // Skin inicial gratis
        return PlayerPrefs.GetInt("SkinUnlocked_" + skinName, 0) == 1;
    }

    public bool UnlockSkin(string skinName, int cost)
    {
        if (IsSkinUnlocked(skinName)) return true;

        if (SpendCronos(cost))
        {
            PlayerPrefs.SetInt("SkinUnlocked_" + skinName, 1);
            SaveData();
            return true;
        }
        return false;
    }

    public void EquipSkin(string skinName)
    {
        if (IsSkinUnlocked(skinName))
        {
            EquippedSkin = skinName;
            SaveData();
        }
    }
}
