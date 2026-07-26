using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI killCountText;

    private const string killsLabel = "Kills: {0}";
    private int lastDisplayedSeconds = int.MinValue;

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

    private void Start()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged += UpdateTimer;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnKillCountChanged += UpdateKillCount;
        }
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged -= UpdateTimer;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnKillCountChanged -= UpdateKillCount;
        }

        if (Instance == this) Instance = null;
    }

    public void UpdateTimer(float time)
    {
        if (timeText == null) return;

        // OnTimeChanged se dispara cada frame: sólo tocamos el TMP cuando el segundo cambia.
        int seconds = Mathf.CeilToInt(time);
        if (seconds == lastDisplayedSeconds) return;

        lastDisplayedSeconds = seconds;
        timeText.SetText(NumberStrings.Get(seconds));
    }

    public void UpdateKillCount(int kills)
    {
        if (killCountText == null) return;
        killCountText.SetText(killsLabel, kills);
    }
}
