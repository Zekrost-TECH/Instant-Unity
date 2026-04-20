using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Config")]
    [Tooltip("Tiempo límite máximo que el jugador puede acumular.")]
    public float maxTime = 100f;
    [Tooltip("Tiempo inicial al comenzar la partida.")]
    public float startingTime = 60f;
    [Tooltip("Cuántas unidades de tiempo se drenan por segundo real.")]
    public float timeDrainRate = 1f;

    public float CurrentTime { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    private void Start()
    {
        CurrentTime = startingTime;
    }

    private void Update()
    {
        // Solo drenar el tiempo si estamos en estado "Playing"
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Drenaje pasivo
        CurrentTime -= timeDrainRate * Time.deltaTime;

        CheckGameOverCondition();
    }

    public void AddTime(float amount)
    {
        CurrentTime += amount;
        
        if (CurrentTime > maxTime)
        {
            CurrentTime = maxTime;
        }
    }

    public void SubtractTime(float amount)
    {
        CurrentTime -= amount;
        CheckGameOverCondition();
    }

    private void CheckGameOverCondition()
    {
        if (CurrentTime <= 0f)
        {
            CurrentTime = 0f;
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGame();
            }
        }
    }
}
