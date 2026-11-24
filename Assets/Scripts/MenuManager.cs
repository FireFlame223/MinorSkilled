using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Menu buttons load the Tutorial, Level, or Main Menu scene, or leave the game.
/// The Game button stays non-interactable until this play session unlocks it.
/// </summary>
public class MenuManager : MonoBehaviour
{
    /// <summary>True after the tutorial ending, or after the unlock shortcut. Clears when play mode starts.</summary>
    public static bool LevelUnlocked { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        LevelUnlocked = false;
    }

    public static void UnlockLevel()
    {
        LevelUnlocked = true;
    }

    [Header("Scenes")]
    [SerializeField] private string tutorialScene = "Tutorial";
    [SerializeField] private string levelScene = "Level";
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("Buttons")]
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button levelButton;
    [SerializeField] private Button exitButton;

#if ENABLE_INPUT_SYSTEM
    [Header("Unlock shortcut")]
    [Tooltip("Held together with the unlock key. None means the unlock key works alone. Left or right Ctrl, Shift, and Alt each accept the other side as well.")]
    [SerializeField] private Key modifierKey = Key.LeftCtrl;

    [Tooltip("Key pressed while the modifier is held.")]
    [SerializeField] private Key unlockKey = Key.U;
#endif

    private bool isLoading;

    private void OnEnable()
    {
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(OpenTutorial);
        if (levelButton != null)
            levelButton.onClick.AddListener(OpenLevel);
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        ApplyLevelLock();
    }

    private void OnDisable()
    {
        if (tutorialButton != null)
            tutorialButton.onClick.RemoveListener(OpenTutorial);
        if (levelButton != null)
            levelButton.onClick.RemoveListener(OpenLevel);
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

    private void Update()
    {
        if (!WasLevelUnlockPressed())
            return;

        UnlockLevel();
        ApplyLevelLock();
    }

    private void ApplyLevelLock()
    {
        if (levelButton != null)
            levelButton.interactable = LevelUnlocked;
    }

    private bool WasLevelUnlockPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || unlockKey == Key.None)
            return false;

        return IsHeld(keyboard, modifierKey) && keyboard[unlockKey].wasPressedThisFrame;
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool IsHeld(Keyboard keyboard, Key key)
    {
        if (key == Key.None)
            return true;

        if (keyboard[key].isPressed)
            return true;

        Key otherSide = OtherSide(key);
        return otherSide != Key.None && keyboard[otherSide].isPressed;
    }

    private static Key OtherSide(Key key)
    {
        switch (key)
        {
            case Key.LeftCtrl: return Key.RightCtrl;
            case Key.RightCtrl: return Key.LeftCtrl;
            case Key.LeftShift: return Key.RightShift;
            case Key.RightShift: return Key.LeftShift;
            case Key.LeftAlt: return Key.RightAlt;
            case Key.RightAlt: return Key.LeftAlt;
            default: return Key.None;
        }
    }
#endif

    private void LoadScene(string sceneName)
    {
        if (isLoading || string.IsNullOrWhiteSpace(sceneName))
            return;

        isLoading = true;
        SceneManager.LoadScene(sceneName);
    }
}
