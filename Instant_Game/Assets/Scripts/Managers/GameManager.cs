using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, GameOver, Paused }
    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Por ahora empezamos directamente el juego para facilitar las pruebas
        ChangeState(GameState.Playing);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.GameOver:
                Debug.Log("¡Game Over! El tiempo se ha agotado.");
                // Pausamos el juego físicamente al perder
                Time.timeScale = 0f; 
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.Menu:
                Time.timeScale = 1f;
                break;
        }
    }
}
