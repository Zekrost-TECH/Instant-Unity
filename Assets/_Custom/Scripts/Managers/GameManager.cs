using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, Upgrade, GameOver }
    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;

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
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        ChangeState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        ChangeState(GameState.Playing);
        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        
        ChangeState(GameState.GameOver);
        Debug.Log("¡Game Over!");
        Time.timeScale = 0f;

        // Calcular puntuación y guardar datos si es posible
        if (SaveManager.Instance != null && SpawnManager.Instance != null && EnemyManager.Instance != null)
        {
            float finalTime = SpawnManager.Instance.GameTime;
            int finalKills = EnemyManager.Instance.KillCount;

            // Fórmulas de Cronos ganados: 1 por cada baja, más la mitad del tiempo sobrevivido
            int cronosGained = finalKills + Mathf.FloorToInt(finalTime * 0.5f);
            
            SaveManager.Instance.AddCronos(cronosGained);
            SaveManager.Instance.UpdateRecords(finalTime, finalKills);

            Debug.Log($"Resultados de la partida: +{cronosGained} Cronos ganados. Record Time: {SaveManager.Instance.BestTime:F1}s, Record Kills: {SaveManager.Instance.BestKills}");
        }
    }

    private void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);
    }
}
