using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, Upgrade, GameOver }
    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;
    public event Action<float, int, int> OnGameOver; // time, kills, cronos
    public event Action OnGameRestarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        CurrentState = GameState.Menu;
        Time.timeScale = 1f; 
    }

    private void Start()
    {
        // For Phase 1 testing, we start the game automatically.
        // In the future, this will be called by a UI Button.
        StartGame();
    }

    public void StartGame()
    {
        ChangeState(GameState.Playing);
        ResetGameSystems();
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

            // Fórmulas de Cronos ganados: 1 por cada baja, más la mitad del tiempo sobrevivido
            int cronosGained = finalKills + Mathf.FloorToInt(finalTime * 0.5f);

            SaveManager.Instance.AddCronos(cronosGained);
            SaveManager.Instance.UpdateRecords(finalTime, finalKills);
            SaveManager.Instance.SetFirstTimePlayed();

            Debug.Log($"Resultados de la partida: +{cronosGained} Cronos ganados. Record Time: {SaveManager.Instance.BestTime:F1}s, Record Kills: {SaveManager.Instance.BestKills}");

            OnGameOver?.Invoke(finalTime, finalKills, cronosGained);
        }
    }

    public void RestartGame()
    {
        ResetGameSystems();
        ChangeState(GameState.Playing);
        AudioManager.Instance?.FadeMusicTo(1f, 0.3f);
        OnGameRestarted?.Invoke();
    }

    public void Revive()
    {
        TimeManager.Instance?.AddTime(60f);
        AudioManager.Instance?.FadeMusicTo(1f, 0.3f);
        ChangeState(GameState.Playing);
    }

    private void ResetGameSystems()
    {
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
