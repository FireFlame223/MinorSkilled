#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared switch for gameplay keys (movement, interact, journal, deduction).
/// Pause stacks on top of the normal block, so closing the pause menu restores whatever was blocked before.
/// A new scene clears both.
/// </summary>
public static class GameplayInput
{
    static bool permanent;
    static bool blocked;

    public static bool IsPaused { get; private set; }

    public static bool IsBlocked => permanent || blocked || IsPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        permanent = false;
        blocked = false;
        IsPaused = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ListenForSceneLoads()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        permanent = false;
        blocked = false;
        IsPaused = false;
    }

    public static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
    }

    public static void SetBlocked(bool value)
    {
        if (permanent)
            return;

        blocked = value;
    }

    public static void BlockPermanently()
    {
        permanent = true;
        blocked = true;
    }
}
