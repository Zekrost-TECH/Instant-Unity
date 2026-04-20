using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Configuración del Tiempo")]
    [SerializeField] private float startingTime = 30f;
    [Tooltip("Cantidad de tiempo drenado por segundo real.")]
    [SerializeField] private float timeDrainRate = 1f;

    private float _currentTime;

    // Eventos para desacoplar el sistema. La UI u otros scripts pueden suscribirse a ellos.
    public event Action<float> OnTimeChanged;
    public event Action OnTimeOut;

    public float CurrentTime => _currentTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _currentTime = startingTime;
        OnTimeChanged?.Invoke(_currentTime);
    }

    private void Update()
    {
        // Solo drenar tiempo si el estado del juego es 'Playing'
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Drenaje pasivo (1 segundo de juego = 1 segundo real por defecto)
        _currentTime -= Time.deltaTime * timeDrainRate;
        
        if (_currentTime <= 0f)
        {
            _currentTime = 0f;
            TriggerGameOver();
        }

        OnTimeChanged?.Invoke(_currentTime);
    }

    public void AddTime(float amount)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        _currentTime += amount;
        OnTimeChanged?.Invoke(_currentTime);
    }

    public void SubtractTime(float amount)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        _currentTime -= amount;
        if (_currentTime <= 0f)
        {
            _currentTime = 0f;
            TriggerGameOver();
        }
        
        OnTimeChanged?.Invoke(_currentTime);
    }

    private void TriggerGameOver()
    {
        OnTimeOut?.Invoke();
        GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
    }
}
