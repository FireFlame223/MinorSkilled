using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Escape opens and closes this menu and blocks gameplay keys while it is open. Resume closes it. Exit loads the main menu.
/// Mouse clicks on these two buttons stay available.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Tooltip("Game theme volume while this menu is open. 1 is the normal level.")]
    [Range(0f, 1f)]
    [SerializeField] private float gameThemeVolume = 0.4f;

    private bool open;

    private void Awake()
    {
        if (resumeButton == null)
            resumeButton = transform.Find("State1/PlayButton")?.GetComponent<Button>();

        if (exitButton == null)
            exitButton = transform.Find("State1/Exit")?.GetComponent<Button>();

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToMainMenu);

        SetOpen(false);
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitToMainMenu);

        if (open)
        {
            GameplayInput.SetPaused(false);
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetGameThemeQuieted(false, gameThemeVolume);
        }
    }

    private void Update()
    {
        if (!GameplayInput.WasEscapePressed())
            return;

        SetOpen(!open);
    }

    public void Resume()
    {
        SetOpen(false);
    }

    public void ExitToMainMenu()
    {
        SetOpen(false);
        if (!string.IsNullOrWhiteSpace(mainMenuScene))
            SceneManager.LoadScene(mainMenuScene);
    }

    private void SetOpen(bool value)
    {
        bool changed = open != value;
        open = value;

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(value);

        if (value)
            transform.SetAsLastSibling();

        GameplayInput.SetPaused(value);

        if (!changed)
            return;

        PlayPauseSound();
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetGameThemeQuieted(value, gameThemeVolume);
    }

    private static void PlayPauseSound()
    {
        if (FMODEvents.Instance == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pauseMenu);
    }
}
