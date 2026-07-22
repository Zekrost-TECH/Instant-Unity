using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Top Display")]
    public TextMeshProUGUI bestTimeText;
    public TextMeshProUGUI walletCronosText;

    private void Start()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (SaveManager.Instance == null) return;

        if (bestTimeText != null)
            bestTimeText.text = $"{SaveManager.Instance.BestTime:F1}s";

        if (walletCronosText != null)
            walletCronosText.text = SaveManager.Instance.Cronos.ToString();
    }

    public void ActionPlay()
    {
        SceneManager.LoadScene("1_Game");
    }

    public void ActionExit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void OpenSettings()
    {
        Debug.Log("Settings: TODO");
    }
}
