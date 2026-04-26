using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public const float TIME_START = 30f;
    public const float TIME_MAX = 45f;
    public const float TIME_DRAIN = 1f;
    public const float TIME_PENALTY = 5f;

    public float CurrentTime { get; private set; }
    
    private float drainMultiplier = 1.0f;
    private bool criticalStateNotified = false;

    public event Action<float> OnTimeChanged;
    public event Action OnTimeCritical;
    public event Action OnTimeOut;

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
        CurrentTime = TIME_START;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        CurrentTime -= TIME_DRAIN * drainMultiplier * Time.deltaTime;
        
        OnTimeChanged?.Invoke(CurrentTime);

        if (CurrentTime <= 5f && !criticalStateNotified)
        {
            criticalStateNotified = true;
            OnTimeCritical?.Invoke();
        }
        else if (CurrentTime > 5f && criticalStateNotified)
        {
            criticalStateNotified = false;
        }

        CheckGameOverCondition();
    }

    public void AddTime(float amount)
    {
        CurrentTime += amount;
        if (CurrentTime > TIME_MAX)
        {
            CurrentTime = TIME_MAX;
        }
        OnTimeChanged?.Invoke(CurrentTime);
    }

    public void SubtractTime(float amount)
    {
        CurrentTime -= amount;
        OnTimeChanged?.Invoke(CurrentTime);
        CheckGameOverCondition();
    }

    public void SetDrainMultiplier(float multiplier)
    {
        drainMultiplier = multiplier;
    }

    private void CheckGameOverCondition()
    {
        if (CurrentTime <= 0f)
        {
            CurrentTime = 0f;
            OnTimeOut?.Invoke();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}
