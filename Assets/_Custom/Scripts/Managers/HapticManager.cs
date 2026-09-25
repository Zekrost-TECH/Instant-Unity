using UnityEngine;
using Lofelt.NiceVibrations;

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
        HapticController.Init();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Handheld.Vibrate() ignoraba la duración (siempre el pulso largo del sistema).
    // Nice Vibrations respeta amplitud y duración: daño y élite son los eventos
    // fuertes del GDD; dash y pickup quedan como toques casi imperceptibles.

    public void TriggerDamage()
    {
        Play(1f, 0.8f, 0.08f);
    }

    public void TriggerDeath()
    {
        Play(1f, 0.5f, 0.25f);
    }

    public void TriggerEliteKill()
    {
        Play(0.65f, 0.6f, 0.04f);
    }

    public void TriggerPickup()
    {
        Play(0.3f, 0.5f, 0.02f);
    }

    public void TriggerDash()
    {
        Play(0.2f, 0.4f, 0.02f);
    }

    private bool IsEnabled()
    {
        if (SaveManager.Instance != null)
            return SaveManager.Instance.VibrationEnabled;
        return true;
    }

    private void Play(float amplitude, float frequency, float duration)
    {
        if (!IsEnabled()) return;
        HapticPatterns.PlayConstant(amplitude, frequency, duration);
    }
}
