using UnityEngine;

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

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

    public void TriggerDamage()
    {
        if (!IsEnabled()) return;
        Vibrate(80);
    }

    public void TriggerEliteKill()
    {
        if (!IsEnabled()) return;
        Vibrate(40);
    }

    private bool IsEnabled()
    {
        if (SaveManager.Instance != null)
            return SaveManager.Instance.VibrationEnabled;
        return true;
    }

    private void Vibrate(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#else
        // En editor no hay vibración disponible
#endif
    }
}
