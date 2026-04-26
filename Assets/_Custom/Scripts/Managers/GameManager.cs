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
    }

    private void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);
    }
}
