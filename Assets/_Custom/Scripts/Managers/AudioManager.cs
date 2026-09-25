using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip mainMusic;
    public AudioClip tensionMusic;
    public AudioClip impactSFX;
    public AudioClip enemyDeathSFX;
    public AudioClip playerDashSFX;
    public AudioClip upgradeSelectSFX;
    public AudioClip upgradeMissedSFX;
    public AudioClip timeGainSFX;
    public AudioClip clockBeepSFX;
    public AudioClip pickupSFX;
    public AudioClip playerHurtSFX;
    public AudioClip eliteDeathSFX;
    public AudioClip upgradeAvailableSFX;
    public AudioClip playerDeathSFX;
    public AudioClip barrierCrackSFX;
    public AudioClip barrierShatterSFX;
    public AudioClip reviveSFX;

    [Header("Death Feel")]
    [Range(0f, 1f)] public float enemyDeathVolume = 0.45f;
    [Range(0f, 1f)] public float eliteDeathVolume = 0.75f;

    [Header("Combat Feel")]
    [Tooltip("El golpe suena ~100 veces por partida: bajo y con variación para que no canse.")]
    [Range(0f, 1f)] public float hitVolume = 0.3f;
    [Range(0f, 1f)] public float hurtVolume = 1f;

    // Volumen elegido por el jugador. Los fades trabajan como fracción de este valor.
    private float musicVolume = 0.8f;
    private float musicFade = 1f;
    private Coroutine fadeRoutine;

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

        // Sin AudioSource los ajustes de volumen no tendrían dónde aplicarse.
        // Sólo se rellenan los huecos: si vienen asignados en el Inspector, se respetan.
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    /// <summary>Crea un AudioManager local si la escena todavía no tiene uno.</summary>
    public static AudioManager Ensure()
    {
        if (Instance != null) return Instance;

        AudioManager existing = FindAnyObjectByType<AudioManager>();
        if (existing != null) return existing;

        GameObject go = new GameObject("AudioManager");
        AudioManager manager = go.AddComponent<AudioManager>();
        return manager;
    }

    private void Start()
    {
        // Inicializar niveles de volumen guardados
        if (SaveManager.Instance != null)
        {
            SetVolume(SaveManager.Instance.MusicVolume, SaveManager.Instance.SFXVolume);
        }
        else
        {
            SetVolume(0.8f, 0.8f);
        }

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeCritical += HandleTimeCritical;
            TimeManager.Instance.OnTimeCriticalEnded += HandleTimeCriticalEnded;
        }
        
        PlayMusic(mainMusic);
    }

    public void SetVolume(float music, float sfx)
    {
        musicVolume = music;
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * musicFade;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfx;
        }
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeCritical -= HandleTimeCritical;
            TimeManager.Instance.OnTimeCriticalEnded -= HandleTimeCriticalEnded;
        }

        if (Instance == this) Instance = null;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>Beep de zona roja. urgency 0..1: el volumen sube de 0.4 a 1.0 según baja el reloj.</summary>
    public void PlayClockBeep(float urgency)
    {
        PlaySFX(clockBeepSFX, Mathf.Lerp(0.4f, 1f, urgency));
    }

    public void PlayHitSFX()
    {
        PlaySFX(impactSFX, hitVolume * Random.Range(0.8f, 1f));
    }

    public void PlayHurtSFX()
    {
        PlaySFX(playerHurtSFX != null ? playerHurtSFX : impactSFX, hurtVolume);
    }

    public void PlayPlayerDeathSFX()
    {
        PlaySFX(playerDeathSFX != null ? playerDeathSFX : eliteDeathSFX, 1f);
    }

    public void PlayReviveSFX()
    {
        PlaySFX(reviveSFX != null ? reviveSFX : upgradeAvailableSFX, 1f);
    }

    public void PlayUpgradeAvailableSFX()
    {
        PlaySFX(upgradeAvailableSFX != null ? upgradeAvailableSFX : upgradeSelectSFX, 0.9f);
    }

    public void PlayTimeGainSFX()
    {
        if (timeGainSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(timeGainSFX, 0.8f);
        }
    }

    public void PlayPickupSFX()
    {
        PlaySFX(pickupSFX != null ? pickupSFX : timeGainSFX, 0.8f);
    }

    public void PlayEnemyDeathSFX(bool isElite)
    {
        float baseVolume = isElite ? eliteDeathVolume : enemyDeathVolume;
        float volume = baseVolume * Random.Range(0.9f, 1.05f);
        AudioClip clip = isElite && eliteDeathSFX != null ? eliteDeathSFX : enemyDeathSFX;
        PlaySFX(clip, volume);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource != null && clip != null)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void PlayMainMusic()
    {
        if (mainMusic != null && (musicSource == null || musicSource.clip != mainMusic))
            PlayMusic(mainMusic);
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private void HandleTimeCritical()
    {
        if (tensionMusic != null && musicSource != null && musicSource.clip != tensionMusic)
        {
            PlayMusic(tensionMusic);
        }
    }

    private void HandleTimeCriticalEnded()
    {
        // Con un jefe de Overtime vivo la tensión sigue aunque el reloj se recupere.
        if (!overtimeTension) PlayMainMusic();
    }

    private bool overtimeTension;

    /// <summary>Música de tensión mientras haya un jefe de Overtime vivo.</summary>
    public void SetOvertimeTension(bool active)
    {
        if (overtimeTension == active) return;
        overtimeTension = active;

        if (active)
        {
            if (tensionMusic != null && musicSource != null && musicSource.clip != tensionMusic)
                PlayMusic(tensionMusic);
            PlaySFX(upgradeMissedSFX, 0.8f); // alarma de aviso
        }
        else
        {
            PlayMainMusic();
        }
    }

    public void PlayBarrierCrackSFX()
    {
        PlaySFX(barrierCrackSFX, 1f);
    }

    public void PlayBarrierBrokenSFX()
    {
        PlaySFX(barrierShatterSFX, 1f);
        PlayEnemyDeathSFX(true);
        PlayUpgradeAvailableSFX();
    }

    /// <summary>
    /// Fade relativo al volumen de música del jugador: 1 = su volumen, 0.3 = 30% de él.
    /// Antes era absoluto y cada ventana de upgrade devolvía la música a 1.0 aunque
    /// el jugador la tuviera bajada o silenciada.
    /// </summary>
    public void FadeMusicTo(float volumeFraction, float duration)
    {
        if (musicSource == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeMusicCoroutine(volumeFraction, duration));
    }

    private IEnumerator FadeMusicCoroutine(float targetFraction, float duration)
    {
        float startFraction = musicFade;
        float elapsed = 0f;

        // Tiempo real: el hit-stop cambia timeScale y no debe frenar el fade.
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicFade = Mathf.Lerp(startFraction, targetFraction, elapsed / duration);
            musicSource.volume = musicVolume * musicFade;
            yield return null;
        }

        musicFade = targetFraction;
        musicSource.volume = musicVolume * musicFade;
        fadeRoutine = null;
    }
}
