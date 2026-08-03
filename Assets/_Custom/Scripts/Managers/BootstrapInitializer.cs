using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapInitializer : MonoBehaviour
{
    [Header("Manager Prefabs")]
    public GameObject gameManagerPrefab;
    public GameObject timeManagerPrefab;
    public GameObject spawnManagerPrefab;
    public GameObject enemyManagerPrefab;
    public GameObject upgradeManagerPrefab;
    public GameObject audioManagerPrefab;
    public GameObject uiManagerPrefab;
    public GameObject saveManagerPrefab;
    public GameObject skinManagerPrefab;
    public GameObject adsManagerPrefab;
    public GameObject hapticManagerPrefab;
    public GameObject particleManagerPrefab;
    public GameObject hitVfxManagerPrefab;

    [Header("Scenes")]
    public string mainMenuSceneName = "0_MainMenu";
    public string gameSceneName = "1_Game";
    public bool loadMainMenuOnStart = true;

    private void Awake()
    {
        EnsureManager(ref gameManagerPrefab, "GameManager");
        EnsureManager(ref timeManagerPrefab, "TimeManager");
        EnsureManager(ref spawnManagerPrefab, "SpawnManager");
        EnsureManager(ref enemyManagerPrefab, "EnemyManager");
        EnsureManager(ref upgradeManagerPrefab, "UpgradeManager");
        EnsureManager(ref audioManagerPrefab, "AudioManager");
        EnsureManager(ref uiManagerPrefab, "UIManager");
        EnsureManager(ref saveManagerPrefab, "SaveManager");
        EnsureManager(ref skinManagerPrefab, "SkinManager");
        EnsureManager(ref adsManagerPrefab, "AdsManager");
        EnsureManager(ref hapticManagerPrefab, "HapticManager");
        EnsureManager(ref particleManagerPrefab, "ParticleManager");
        EnsureManager(ref hitVfxManagerPrefab, "HitVFXManager");
    }

    private void Start()
    {
        if (loadMainMenuOnStart && SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void EnsureManager(ref GameObject prefab, string managerName)
    {
        if (HasManagerComponent(managerName)) return;
        if (GameObject.Find(managerName) != null) return;

        GameObject managerObj;
        if (prefab != null)
        {
            managerObj = Instantiate(prefab);
        }
        else
        {
            managerObj = new GameObject(managerName);
            AddManagerComponent(managerObj, managerName);
        }

        managerObj.name = managerName;
        DontDestroyOnLoad(managerObj);
    }

    private bool HasManagerComponent(string managerName)
    {
        switch (managerName)
        {
            case "GameManager": return FindAnyObjectByType<GameManager>() != null;
            case "TimeManager": return FindAnyObjectByType<TimeManager>() != null;
            case "SpawnManager": return FindAnyObjectByType<SpawnManager>() != null;
            case "EnemyManager": return FindAnyObjectByType<EnemyManager>() != null;
            case "UpgradeManager": return FindAnyObjectByType<UpgradeManager>() != null;
            case "AudioManager": return FindAnyObjectByType<AudioManager>() != null;
            case "UIManager": return FindAnyObjectByType<UIManager>() != null;
            case "SaveManager": return FindAnyObjectByType<SaveManager>() != null;
            case "SkinManager": return FindAnyObjectByType<SkinManager>() != null;
            case "AdsManager": return FindAnyObjectByType<AdsManager>() != null;
            case "HapticManager": return FindAnyObjectByType<HapticManager>() != null;
            case "ParticleManager": return FindAnyObjectByType<ParticleManager>() != null;
            case "HitVFXManager": return FindAnyObjectByType<HitVFXManager>() != null;
            default: return false;
        }
    }

    private void AddManagerComponent(GameObject obj, string managerName)
    {
        switch (managerName)
        {
            case "GameManager": obj.AddComponent<GameManager>(); break;
            case "TimeManager": obj.AddComponent<TimeManager>(); break;
            case "SpawnManager": obj.AddComponent<SpawnManager>(); break;
            case "EnemyManager": obj.AddComponent<EnemyManager>(); break;
            case "UpgradeManager": obj.AddComponent<UpgradeManager>(); break;
            case "AudioManager": obj.AddComponent<AudioManager>(); break;
            case "UIManager": obj.AddComponent<UIManager>(); break;
            case "SaveManager": obj.AddComponent<SaveManager>(); break;
            case "SkinManager": obj.AddComponent<SkinManager>(); break;
            case "AdsManager": obj.AddComponent<AdsManager>(); break;
            case "HapticManager": obj.AddComponent<HapticManager>(); break;
            case "ParticleManager": obj.AddComponent<ParticleManager>(); break;
            case "HitVFXManager": obj.AddComponent<HitVFXManager>(); break;
        }
    }
}
