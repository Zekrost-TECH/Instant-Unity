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

    /// <summary>
    /// Crea el AudioManager si no existe. No está puesto en ninguna escena, así que sin
    /// esto los ajustes de volumen no tendrían a quién aplicarse.
    /// </summary>
    public static AudioManager Ensure()
    {
        if (Instance != null) return Instance;

        AudioManager existing = FindAnyObjectByType<AudioManager>();
        if (existing != null) return existing;

        GameObject go = new GameObject("AudioManager");
        AudioManager manager = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
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
        }
        
        PlayMusic(mainMusic);
    }

    public void SetVolume(float music, float sfx)
    {
        if (musicSource != null)
        {
            musicSource.volume = music;
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

    public void PlayClockBeep()
    {
        if (clockBeepSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clockBeepSFX, 0.8f);
        }
    }

    public void PlayTimeGainSFX()
    {
        if (timeGainSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(timeGainSFX, 0.8f);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource != null && clip != null)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
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

    public void FadeMusicTo(float targetVolume, float duration)
    {
        if (musicSource == null) return;
        StartCoroutine(FadeMusicCoroutine(targetVolume, duration));
    }

    private IEnumerator FadeMusicCoroutine(float targetVolume, float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }
}
