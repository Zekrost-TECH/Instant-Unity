using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, Upgrade, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("Escenas")]
    public string gameSceneName = "1_Game";
    public string mainMenuSceneName = "0_MainMenu";

    public event Action<GameState> OnGameStateChanged;
    public event Action<float, int, int, bool> OnGameOver; // time, kills, payout, newRecord
    public event Action OnGameRestarted;

    private int cronosAwardedThisRun = 0;

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
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;

        SaveManager.Ensure();

        ApplyMobilePerformanceSettings();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// En móvil Unity limita a 30 fps por defecto. Para un survivor de reflejos como este
    /// hay que pedir 60 explícitamente, y con vSync activo targetFrameRate se ignora.
    /// </summary>
    private void ApplyMobilePerformanceSettings()
    {
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
    }

    private void Start()
    {
        // Al entrar en play mode directamente en 1_Game no se dispara sceneLoaded,
        // así que hay que arrancar aquí. Si veníamos del menú, sceneLoaded ya lo hizo.
        if (CurrentState != GameState.Playing && SceneManager.GetActiveScene().name == gameSceneName)
        {
            StartGame();
        }
    }

    /// <summary>
    /// El GameManager es local a la escena de juego, así que cada entrada crea un estado
    /// limpio y suscribe de nuevo el flujo de escena.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            StartGame();
        }
        else if (scene.name == mainMenuSceneName)
        {
            // Limpieza defensiva si el menú se carga desde una partida todavía activa.
            SpawnManager.Instance?.ClearAllEnemies();
            ChangeState(GameState.Menu);
        }
    }

    public static int CalculateRunCronos(int kills, float time)
    {
        // Balanceo: kills son la fuente principal (40%), tiempo es bonus secundario (8%)
        // Esto evita que una sola partida larga llene la wallet de cronos
        int fromKills = Mathf.FloorToInt(kills * 0.4f);
        int fromTime = Mathf.FloorToInt(time * 0.08f);
        return Mathf.Max(1, fromKills + fromTime);
    }

    public void StartGame()
    {
        // Resetear ANTES de anunciar el estado: si no, quien escuche OnGameStateChanged
        // lee todavía el reloj a 0 y las kills de la partida anterior.
        ResetGameSystems();
        ChangeState(GameState.Playing);
        AudioManager.Instance?.PlayMainMusic();
    }

    public void PauseGame()
    {
        ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        ChangeState(GameState.Playing);
    }

    public void ChangeToUpgradeState()
    {
        ChangeState(GameState.Upgrade);
    }

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        ChangeState(GameState.GameOver);
        Debug.Log("¡Game Over!");
        AudioManager.Instance?.FadeMusicTo(0.3f, 0.3f);

        // Calcular puntuación y guardar datos si es posible
        if (SaveManager.Instance != null && SpawnManager.Instance != null && EnemyManager.Instance != null)
        {
            float finalTime = SpawnManager.Instance.GameTime;
            int finalKills = EnemyManager.Instance.KillCount;

            int cronosGained = CalculateRunCronos(finalKills, finalTime);

            // Tras revivir se vuelve a pasar por aquí con las MISMAS kills acumuladas.
            // Pagamos sólo la diferencia o la partida cobraría dos veces lo ya cobrado.
            int payout = Mathf.Max(0, cronosGained - cronosAwardedThisRun);
            cronosAwardedThisRun += payout;

            SaveManager.Instance.AddCronos(payout);
            bool newRecord = SaveManager.Instance.UpdateRecords(finalTime, finalKills);
            SaveManager.Instance.SetFirstTimePlayed();

            Debug.Log($"Resultados de la partida: +{cronosGained} Cronos ganados. Record Time: {SaveManager.Instance.BestTime:F1}s, Record Kills: {SaveManager.Instance.BestKills}");

            OnGameOver?.Invoke(finalTime, finalKills, payout, newRecord);
        }
    }

    public void RestartGame()
    {
        ResetGameSystems();
        ChangeState(GameState.Playing);
        AudioManager.Instance?.FadeMusicTo(1f, 0.3f);
        AudioManager.Instance?.PlayMainMusic();
        OnGameRestarted?.Invoke();
    }

    /// <summary>
    /// Continúa la misma partida: kills y Cronos acumulados intactos, reloj al máximo.
    /// No se toca EnemyManager.KillCount ni SpawnManager.GameTime a propósito.
    /// </summary>
    public void Revive()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.FillToMax();

        // Sin esto revives dentro del enjambre que te mató y mueres otra vez al instante.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
            if (movement != null) movement.TriggerHitInvulnerability();
        }

        AudioManager.Instance?.FadeMusicTo(1f, 0.3f);
        AudioManager.Instance?.PlayMainMusic();
        ChangeState(GameState.Playing);

        Debug.Log($"[GameManager] Revivido. Kills conservadas: {(EnemyManager.Instance != null ? EnemyManager.Instance.KillCount : 0)}");
    }

    private void ResetGameSystems()
    {
        // Partida nueva: vuelve a contar desde cero lo ya pagado
        cronosAwardedThisRun = 0;

        // Limpiar enemigos y proyectiles antes de resetear el resto
        SpawnManager.Instance?.ClearAllEnemies();

        // Resetear el jugador (posición, velocidad, stats base, estado de dash)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
            PlayerCombat combat = playerObj.GetComponent<PlayerCombat>();
            if (movement != null) movement.ResetState();
            if (combat != null) combat.ResetState();
        }

        TimeManager.Instance?.ResetTime();
        EnemyManager.Instance?.ResetKillCount();
        SpawnManager.Instance?.ResetGameTime();
        UpgradeManager.Instance?.ResetUpgrades();
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);
    }
}
