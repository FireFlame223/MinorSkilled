using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which of the four tokens the player owns and unlocks more from designer-authored rules.
/// </summary>
public class TokenUnlockManager : MonoBehaviour
{
    public const int TokenCount = 4;

    public enum TokenRequirementKind
    {
        Single,
        Pair
    }

    [Serializable]
    public class TokenUnlockRule
    {
        [Tooltip("POI where the player must confirm the required token choice.")]
        public MapInteractable poi;

        public TokenRequirementKind requirement = TokenRequirementKind.Pair;

        [Tooltip("Token 1–4")]
        [Range(1, 4)]
        public int requiredToken1 = 1;

        [Tooltip("Second token for pair rules (Token 1–4). Ignored for single.")]
        [Range(1, 4)]
        public int requiredToken2 = 2;

        [Tooltip("Token 1–4 to unlock when this rule matches.")]
        [Range(1, 4)]
        public int unlockToken = 3;
    }

    [Header("Starting tokens")]
    [Tooltip("Tokens 1–4 the player has at the start of a session.")]
    [SerializeField] private int[] startingUnlockedTokens = { 1, 2 };

    [Header("Unlock rules")]
    [SerializeField] private TokenUnlockRule[] unlockRules = Array.Empty<TokenUnlockRule>();

    private readonly HashSet<int> _unlockedTokenIndices = new HashSet<int>();

    private void Awake()
    {
        ResetToStartingTokens();
    }

    public void ResetToStartingTokens()
    {
        _unlockedTokenIndices.Clear();

        if (startingUnlockedTokens == null)
            return;

        foreach (int tokenNumber in startingUnlockedTokens)
            UnlockTokenNumber(tokenNumber);
    }

    public bool IsTokenUnlocked(int tokenIndex)
    {
        return tokenIndex >= 0 && tokenIndex < TokenCount && _unlockedTokenIndices.Contains(tokenIndex);
    }

    /// <summary>
    /// Call after the player confirms a token choice at a POI. Returns true if at least one new token was unlocked.
    /// </summary>
    public bool EvaluateUnlocks(MapInteractable poi, IReadOnlyList<int> confirmedTokenIndices)
    {
        if (poi == null || confirmedTokenIndices == null || confirmedTokenIndices.Count == 0)
            return false;

        bool anyUnlocked = false;

        foreach (TokenUnlockRule rule in unlockRules)
        {
            if (rule == null || !IsRuleForPoi(rule, poi))
                continue;

            if (!RuleMatches(rule, confirmedTokenIndices))
                continue;

            if (UnlockTokenNumber(rule.unlockToken))
                anyUnlocked = true;
        }

        return anyUnlocked;
    }

    private static bool IsRuleForPoi(TokenUnlockRule rule, MapInteractable poi)
    {
        if (rule.poi == null || poi == null)
            return false;

        if (ReferenceEquals(rule.poi, poi))
            return true;

        if (rule.poi.gameObject == poi.gameObject)
            return true;

        return string.Equals(rule.poi.JournalId, poi.JournalId, StringComparison.Ordinal);
    }

    private bool RuleMatches(TokenUnlockRule rule, IReadOnlyList<int> confirmedTokenIndices)
    {
        if (rule.requirement == TokenRequirementKind.Single)
        {
            if (confirmedTokenIndices.Count != 1)
                return false;

            return confirmedTokenIndices[0] == ToTokenIndex(rule.requiredToken1);
        }

        if (confirmedTokenIndices.Count != 2)
            return false;

        int a = confirmedTokenIndices[0];
        int b = confirmedTokenIndices[1];
        if (a > b)
            (a, b) = (b, a);

        int reqA = ToTokenIndex(rule.requiredToken1);
        int reqB = ToTokenIndex(rule.requiredToken2);
        if (reqA > reqB)
            (reqA, reqB) = (reqB, reqA);

        return a == reqA && b == reqB;
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
