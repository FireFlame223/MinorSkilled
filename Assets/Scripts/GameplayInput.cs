/// <summary>
/// Shared switch for gameplay keys (movement, interact, journal, hypothesis).
/// </summary>
public static class GameplayInput
{
    static bool permanent;

    public static bool IsBlocked { get; private set; }

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
