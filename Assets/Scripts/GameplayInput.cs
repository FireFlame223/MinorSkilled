#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Shared switch for gameplay keys (movement, interact, journal, hypothesis).
/// </summary>
public static class GameplayInput
{
    static bool permanent;
    static int escapeConsumedFrame = -1;

    public static bool IsBlocked { get; private set; }

    public static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    /// <summary>True for the first panel that closes with Escape this frame.</summary>
    public static bool TryConsumeEscape()
    {
        if (!WasEscapePressed() || escapeConsumedFrame == UnityEngine.Time.frameCount)
            return false;

        escapeConsumedFrame = UnityEngine.Time.frameCount;
        return true;
    }

    public static void SetBlocked(bool blocked)
    {
        if (permanent)
        {
            IsBlocked = true;
            return;
        }

        IsBlocked = blocked;
    }

    public static void BlockPermanently()
    {
        permanent = true;
        IsBlocked = true;
    }
}
