using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI timeText;
    public Image timeBar;
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI runCronosText;
    public GameObject hudRoot;

    [Header("Colors")]
    public Color calmColor = Color.white;
    public Color warningColor = new Color(1f, 0.67f, 0f); // #FFAA00
    public Color dangerColor = new Color(1f, 0.2f, 0.2f); // #FF3333

    private void Start()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged += UpdateTimer;
            TimeManager.Instance.OnTimeColorChanged += UpdateTimeColor;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnKillCountChanged += UpdateKillCount;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        UpdateTimeColor(TimeManager.TimeColorState.Calm);
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged -= UpdateTimer;
            TimeManager.Instance.OnTimeColorChanged -= UpdateTimeColor;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnKillCountChanged -= UpdateKillCount;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (hudRoot != null)
        {
            hudRoot.SetActive(state == GameManager.GameState.Playing || state == GameManager.GameState.Upgrade);
        }
    }

    private void UpdateTimer(float time)
    {
        if (timeText != null)
            timeText.text = Mathf.CeilToInt(time).ToString();

        if (timeBar != null)
        {
            float fill = Mathf.Clamp01(time / TimeManager.TIME_MAX);
            timeBar.fillAmount = fill;
        }

        UpdateRunCronos();
    }

    private void UpdateTimeColor(TimeManager.TimeColorState state)
    {
        Color targetColor = calmColor;
        switch (state)
        {
            case TimeManager.TimeColorState.Warning:
                targetColor = warningColor;
                break;
            case TimeManager.TimeColorState.Danger:
                targetColor = dangerColor;
                break;
        }

        if (timeText != null)
            timeText.color = targetColor;
    }

    private void UpdateKillCount(int kills)
    {
        if (killCountText != null)
            killCountText.text = $"Kills: {kills}";
    }

    private void UpdateRunCronos()
    {
        if (runCronosText == null) return;

        int runCronos = 0;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            int kills = EnemyManager.Instance?.KillCount ?? 0;
            float time = SpawnManager.Instance?.GameTime ?? 0f;
            runCronos = kills + Mathf.FloorToInt(time * 0.5f);
        }
        runCronosText.text = $"⟳ {runCronos}";
    }
}
