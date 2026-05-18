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
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
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
