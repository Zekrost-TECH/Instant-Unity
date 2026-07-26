using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Raíz del panel de ajustes. Se activa y desactiva al abrir y cerrar.")]
    public GameObject settingsPanel;
    public Button closeButton;

    [Header("Controles")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle vibrationToggle;

    [Header("Etiquetas de valor (opcionales)")]
    public TextMeshProUGUI musicValueText;
    public TextMeshProUGUI sfxValueText;
    public TextMeshProUGUI vibrationValueText;

    private bool openRequested = false;
    private bool applyingValues = false;

    public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

    private void Awake()
    {
        SaveManager.Ensure();
        AudioManager.Ensure();   // el menú no lleva managers; sin esto no hay dónde aplicar el volumen
    }

    private void Start()
    {
        // Si este componente vive dentro del propio panel, Start corre justo DESPUÉS de
        // que Open() lo active: cerrar a ciegas aquí lo volvería a ocultar.
        if (settingsPanel != null && !openRequested) settingsPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(HandleMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(HandleVibrationChanged);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.RemoveListener(HandleVibrationChanged);
    }

    // ── Abrir / cerrar ───────────────────────────────────────────────────────

    public void Open()
    {
        openRequested = true;
        SaveManager.Ensure();

        LoadValuesIntoControls();

        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void Close()
    {
        openRequested = false;

        // El volumen se persiste aquí, no en cada frame del arrastre: SaveManager.SetVolume
        // llama a PlayerPrefs.Save(), que escribe el fichero entero en disco.
        PersistVolumes();

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    // ── Carga y guardado ─────────────────────────────────────────────────────

    private void LoadValuesIntoControls()
    {
        if (SaveManager.Instance == null) return;

        // applyingValues evita que rellenar los controles dispare los callbacks
        // y se reescriba lo que acabamos de leer.
        applyingValues = true;

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(SaveManager.Instance.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SaveManager.Instance.SFXVolume);
        if (vibrationToggle != null) vibrationToggle.SetIsOnWithoutNotify(SaveManager.Instance.VibrationEnabled);

        applyingValues = false;

        RefreshLabels();
    }

    private void PersistVolumes()
    {
        if (SaveManager.Instance == null) return;

        float music = musicSlider != null ? musicSlider.value : SaveManager.Instance.MusicVolume;
        float sfx = sfxSlider != null ? sfxSlider.value : SaveManager.Instance.SFXVolume;

        SaveManager.Instance.SetVolume(music, sfx);
    }

    // ── Callbacks de los controles ───────────────────────────────────────────

    private void HandleMusicChanged(float value)
    {
        if (applyingValues) return;

        // Preview en vivo sin tocar disco; el guardado va en Close().
        ApplyVolumesToAudio();
        RefreshLabels();
    }

    private void HandleSfxChanged(float value)
    {
        if (applyingValues) return;

        ApplyVolumesToAudio();
        RefreshLabels();
    }

    private void HandleVibrationChanged(bool value)
    {
        if (applyingValues) return;

        // Es un cambio discreto, no un arrastre: se puede persistir al momento.
        SaveManager.Instance?.SetVibration(value);
        RefreshLabels();

        if (value) HapticManager.Instance?.TriggerEliteKill();   // que se note al activarlo
    }

    private void ApplyVolumesToAudio()
    {
        if (AudioManager.Instance == null) return;

        float music = musicSlider != null ? musicSlider.value : 0.8f;
        float sfx = sfxSlider != null ? sfxSlider.value : 0.8f;
        AudioManager.Instance.SetVolume(music, sfx);
    }

    private void RefreshLabels()
    {
        if (musicValueText != null && musicSlider != null)
            musicValueText.SetText("{0}%", Mathf.RoundToInt(musicSlider.value * 100f));

        if (sfxValueText != null && sfxSlider != null)
            sfxValueText.SetText("{0}%", Mathf.RoundToInt(sfxSlider.value * 100f));

        if (vibrationValueText != null && vibrationToggle != null)
            vibrationValueText.SetText(vibrationToggle.isOn ? "SÍ" : "NO");
    }
}
