using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu buttons load the Tutorial, Level, or Main Menu scene, or leave the game.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string tutorialScene = "Tutorial";
    [SerializeField] private string levelScene = "Level";
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("Buttons")]
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button levelButton;
    [SerializeField] private Button exitButton;
    private Button mainMenuButton;

    private bool isLoading;

    private void OnEnable()
    {
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(OpenTutorial);
        if (levelButton != null)
            levelButton.onClick.AddListener(OpenLevel);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OpenMainMenu);
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    private void OnDisable()
    {
        if (tutorialButton != null)
            tutorialButton.onClick.RemoveListener(OpenTutorial);
        if (levelButton != null)
            levelButton.onClick.RemoveListener(OpenLevel);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OpenMainMenu);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }

    public void OpenTutorial()
    {
        LoadScene(tutorialScene);
    }

    public void OpenLevel()
    {
        LoadScene(levelScene);
    }

    public void OpenMainMenu()
    {
        LoadScene(mainMenuScene);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadScene(string sceneName)
    {
        if (isLoading || string.IsNullOrWhiteSpace(sceneName))
            return;

        isLoading = true;
        SceneManager.LoadScene(sceneName);
    }
}
