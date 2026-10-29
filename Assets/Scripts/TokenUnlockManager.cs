using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which of the four tokens the player owns. Deduction puzzles award new tokens.
/// </summary>
public class TokenUnlockManager : MonoBehaviour
{
    public const int TokenCount = 4;

    [Header("Names")]
    [Tooltip("Display names for tokens 1–4, in order.")]
    [SerializeField] private string[] tokenNames = { "Seen", "Heard", "Done", "Told" };

    [Header("Starting tokens")]
    [Tooltip("Tokens 1–4 the player has at the start of a session.")]
    [SerializeField] private int[] startingUnlockedTokens = { 1, 2 };

    [Tooltip("Start with all four tokens so interactions can be checked without solving the deduction puzzles. Leave this off for a normal session.")]
    [SerializeField] private bool unlockAllTokens;

    private readonly HashSet<int> _unlockedTokenIndices = new HashSet<int>();
    private bool _appliedUnlockAll;

    private void Awake()
    {
        ResetToStartingTokens();
        _appliedUnlockAll = unlockAllTokens;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || _appliedUnlockAll == unlockAllTokens)
            return;

        _appliedUnlockAll = unlockAllTokens;
        ResetToStartingTokens();

        InteractionPanelUI panel = FindFirstObjectByType<InteractionPanelUI>();
        if (panel != null)
            panel.RefreshUnlockedTokens();
    }

    public void ResetToStartingTokens()
    {
        _unlockedTokenIndices.Clear();

        if (unlockAllTokens)
        {
            for (int tokenNumber = 1; tokenNumber <= TokenCount; tokenNumber++)
                UnlockTokenNumber(tokenNumber);

            return;
        }

        if (startingUnlockedTokens == null)
            return;

        foreach (int tokenNumber in startingUnlockedTokens)
            UnlockTokenNumber(tokenNumber);
    }

    public bool IsTokenUnlocked(int tokenIndex)
    {
        return tokenIndex >= 0 && tokenIndex < TokenCount && _unlockedTokenIndices.Contains(tokenIndex);
    }

    /// <summary>Display name for token number 1–4.</summary>
    public string GetTokenName(int tokenNumber)
    {
        int index = ToTokenIndex(tokenNumber);
        if (index < 0)
            return string.Empty;

        if (tokenNames != null && index < tokenNames.Length && !string.IsNullOrWhiteSpace(tokenNames[index]))
            return tokenNames[index];

        return tokenNumber.ToString();
    }

    /// <summary>Awards token number 1–4. Returns true only the first time that token is acquired.</summary>
    public bool UnlockToken(int tokenNumber)
    {
        return UnlockTokenNumber(tokenNumber);
    }

    private bool UnlockTokenNumber(int tokenNumber)
    {
        int index = ToTokenIndex(tokenNumber);
        if (index < 0)
            return false;

        return _unlockedTokenIndices.Add(index);
    }

    private static int ToTokenIndex(int tokenNumber)
    {
        int index = tokenNumber - 1;
        if (index < 0 || index >= TokenCount)
        {
            Debug.LogWarning($"TokenUnlockManager: token number {tokenNumber} is out of range (1–4).");
            return -1;
        }

        return index;
    }
}
