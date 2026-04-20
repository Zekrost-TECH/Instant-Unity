using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, GameOver, Paused }
    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        CurrentState = GameState.Playing;
        
        // Desbloquear el tiempo en caso de que un reinicio lo haya dejado en 0
        Time.timeScale = 1f; 
    }

    public void EndGame()
    {
        if (CurrentState == GameState.GameOver) return;
        
        CurrentState = GameState.GameOver;
        Debug.Log("¡Game Over! Te has quedado sin tiempo.");
        
        // Por ahora lo pausaremos. Más adelante llamaremos a la UI de derrota.
        Time.timeScale = 0f;
    }
}
