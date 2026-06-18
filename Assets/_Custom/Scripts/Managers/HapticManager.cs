using UnityEngine;

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
