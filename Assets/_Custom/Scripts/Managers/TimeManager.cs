using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public float TIME_START = 30f;
    public const float TIME_MAX = 45f;
    public const float TIME_DRAIN = 1.25f;
    public const float TIME_PENALTY = 6f;

    public float CurrentTime { get; private set; }
    public float TimeGainedThisRun { get; private set; }
    
    private float drainMultiplier = 1.0f;
    public float PermanentDrainModifier = 1.0f;
    private bool criticalStateNotified = false;
    private bool timeOutNotified = false;

    public event Action<float> OnTimeChanged;
    public event Action OnTimeCritical;
    public event Action OnTimeCriticalEnded;
    public event Action OnTimeOut;
    public event Action<TimeColorState> OnTimeColorChanged;

    private float nextBeepTime = 0f;
    private float beepInterval = 0.5f;

    public enum TimeColorState
    {
        Calm,    // > 15s
        Warning, // 5-15s
        Danger   // <= 5s
    }

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
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        float bonusTime = 0f;
        if (SaveManager.Instance != null)
        {
            bonusTime = SaveManager.Instance.StartingTimeLevel * 2f;
        }
        CurrentTime = Mathf.Min(TIME_START + bonusTime, TIME_MAX);
        UpdateTimeColorState();
    }

    private void Update()
    {
        // Playing drena al 100%; Upgrade drena parcialmente vía drainMultiplier (pausa parcial).
        // Cualquier otro estado congela el reloj.
        if (GameManager.Instance != null)
        {
            GameManager.GameState state = GameManager.Instance.CurrentState;
            if (state != GameManager.GameState.Playing && state != GameManager.GameState.Upgrade)
                return;
        }

        CurrentTime -= TIME_DRAIN * drainMultiplier * PermanentDrainModifier * Time.deltaTime;
        CurrentTime = Mathf.Clamp(CurrentTime, 0f, TIME_MAX);
        
        OnTimeChanged?.Invoke(CurrentTime);
        UpdateTimeColorState();

        if (CurrentTime <= 5f && !criticalStateNotified)
        {
            criticalStateNotified = true;
            OnTimeCritical?.Invoke();
        }
        else if (CurrentTime > 5f && criticalStateNotified)
        {
            criticalStateNotified = false;
            OnTimeCriticalEnded?.Invoke();
        }

        if (CurrentTime <= 5f)
        {
            PlayCriticalBeep();
        }

        CheckGameOverCondition();
    }

    private TimeColorState currentColorState = TimeColorState.Calm;

    private void UpdateTimeColorState()
    {
        TimeColorState newState;
        if (CurrentTime <= 5f) newState = TimeColorState.Danger;
        else if (CurrentTime <= 15f) newState = TimeColorState.Warning;
        else newState = TimeColorState.Calm;

        if (newState != currentColorState)
        {
            currentColorState = newState;
            OnTimeColorChanged?.Invoke(currentColorState);
        }
    }

    private void PlayCriticalBeep()
    {
        if (Time.time >= nextBeepTime)
        {
            AudioManager.Instance?.PlayClockBeep();
            nextBeepTime = Time.time + beepInterval;
        }
    }

    public void AddTime(float amount)
    {
        float previousTime = CurrentTime;
        CurrentTime += amount;
        CurrentTime = Mathf.Clamp(CurrentTime, 0f, TIME_MAX);
        TimeGainedThisRun += Mathf.Max(0f, CurrentTime - previousTime);
        OnTimeChanged?.Invoke(CurrentTime);
        UpdateTimeColorState();
    }

    /// <summary>Deja el reloj al máximo. Lo usa el revivir por anuncio.</summary>
    public void FillToMax()
    {
        CurrentTime = TIME_MAX;
        criticalStateNotified = false;
        nextBeepTime = 0f;
        OnTimeChanged?.Invoke(CurrentTime);
        OnTimeCriticalEnded?.Invoke();
        UpdateTimeColorState();
    }

    public void SubtractTime(float amount)
    {
        CurrentTime -= amount;
        CurrentTime = Mathf.Clamp(CurrentTime, 0f, TIME_MAX);
        OnTimeChanged?.Invoke(CurrentTime);
        UpdateTimeColorState();
        CheckGameOverCondition();
    }

    public void SetDrainMultiplier(float multiplier)
    {
        drainMultiplier = multiplier;
    }

    public void ResetTime()
    {
        float bonusTime = 0f;
        if (SaveManager.Instance != null)
        {
            bonusTime = SaveManager.Instance.StartingTimeLevel * 2f;
        }
        CurrentTime = Mathf.Min(TIME_START + bonusTime, TIME_MAX);
        TimeGainedThisRun = 0f;
        criticalStateNotified = false;
        timeOutNotified = false;
        currentColorState = TimeColorState.Calm;
        nextBeepTime = 0f;
        drainMultiplier = 1.0f;
        PermanentDrainModifier = 1.0f;
        OnTimeChanged?.Invoke(CurrentTime);
        OnTimeColorChanged?.Invoke(currentColorState);
    }

    private void CheckGameOverCondition()
    {
        if (CurrentTime > 0f)
        {
            timeOutNotified = false;
            return;
        }

        CurrentTime = 0f;
        if (timeOutNotified) return;

        timeOutNotified = true;
        OnTimeOut?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}
