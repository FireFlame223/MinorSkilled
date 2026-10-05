using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// POI on the map: interaction copy, token outcomes (single and pairs), and journal entry fields.
/// </summary>
public class MapInteractable : MonoBehaviour
{
    [Tooltip("Optional. If unset, this object's position defines the interaction cell.")]
    [SerializeField] private Transform interactionPoint;

    [Header("Interaction")]
    [SerializeField] private string displayName;

    [TextArea(3, 8)]
    [SerializeField] private string description;

    [Header("Tokens — single choice")]
    [TextArea(3, 8)]
    [SerializeField] private string token1;

    [TextArea(3, 8)]
    [SerializeField] private string token2;

    [TextArea(3, 8)]
    [SerializeField] private string token3;

    [TextArea(3, 8)]
    [SerializeField] private string token4;

    [Header("Tokens — pairs (order does not matter)")]
    [TextArea(3, 8)]
    [SerializeField] private string tokenPair1And2;

    [TextArea(3, 8)]
    [SerializeField] private string tokenPair1And3;

    [TextArea(3, 8)]
    [SerializeField] private string tokenPair1And4;

    [TextArea(3, 8)]
    [SerializeField] private string tokenPair2And3;

    [TextArea(3, 8)]
    [SerializeField] private string tokenPair2And4;

    [TextArea(3, 8)]
    [SerializeField] private string tokenPair3And4;

    [Header("Journal — first visit")]
    [Tooltip("Stable id for save data. If empty, the GameObject name is used.")]
    [SerializeField] private string journalId;

    [SerializeField] private string journalTitle;

    [Tooltip("First bullet on the POI journal page (first E interaction).")]
    [TextArea(3, 8)]
    [SerializeField] private string journalDescription;

    [Header("Journal — Token 1 alone")]
    [Tooltip("Appended as a new bullet the first time Token 1 is confirmed.")]
    [TextArea(2, 4)]
    [SerializeField] private string journalToken1Body;

    [Header("Journal — Token 2 alone")]
    [TextArea(2, 4)]
    [SerializeField] private string journalToken2Body;

    [Header("Journal — Token 3 alone")]
    [TextArea(2, 4)]
    [SerializeField] private string journalToken3Body;

    [Header("Journal — Token 4 alone")]
    [TextArea(2, 4)]
    [SerializeField] private string journalToken4Body;

    [Header("Journal — Tokens 1+2")]
    [TextArea(2, 4)]
    [SerializeField] private string journalPair1And2Body;

    [Header("Journal — Tokens 1+3")]
    [TextArea(2, 4)]
    [SerializeField] private string journalPair1And3Body;

    [Header("Journal — Tokens 1+4")]
    [TextArea(2, 4)]
    [SerializeField] private string journalPair1And4Body;

    [Header("Journal — Tokens 2+3")]
    [TextArea(2, 4)]
    [SerializeField] private string journalPair2And3Body;

    [Header("Journal — Tokens 2+4")]
    [TextArea(2, 4)]
    [SerializeField] private string journalPair2And4Body;

    [Header("Journal — Tokens 3+4")]
    [TextArea(2, 4)]
    [SerializeField] private string journalPair3And4Body;

    public string DisplayName => displayName;
    public string Description => description;
    public bool Explored { get; set; }

    public string JournalId => string.IsNullOrWhiteSpace(journalId) ? name : journalId;
    public string JournalTitle => journalTitle;
    public string JournalDescription => journalDescription;

    public Vector2 InteractionWorldPosition
    {
        get
        {
            Transform point = interactionPoint != null ? interactionPoint : transform;
            return point.position;
        }
    }

    public string GetMenuOptionText(int index)
    {
        switch (index)
        {
            case 0: return token1;
            case 1: return token2;
            case 2: return token3;
            case 3: return token4;
            default: return string.Empty;
        }
    }

    public string GetTokenChoiceText(IReadOnlyList<int> tokenIndices)
    {
        if (!TryNormalizeTokenIndices(tokenIndices, out int a, out int b, out bool isPair))
            return string.Empty;

        if (!isPair)
            return GetMenuOptionText(a);

        return GetTokenPairText(a, b);
    }

    /// <summary>Unique key for this token outcome (for journal deduplication).</summary>
    public string GetTokenChoiceJournalKey(IReadOnlyList<int> tokenIndices)
    {
        if (!TryNormalizeTokenIndices(tokenIndices, out int a, out int b, out bool isPair))
            return string.Empty;

        if (!isPair)
            return $"{JournalId}:token:{a + 1}";

        return $"{JournalId}:pair:{a + 1}-{b + 1}";
    }

    /// <summary>Journal bullet line for this token outcome (separate from interaction panel text).</summary>
    public bool TryGetTokenChoiceJournalBullet(IReadOnlyList<int> tokenIndices, out string bulletLine)
    {
        bulletLine = string.Empty;

        if (!TryNormalizeTokenIndices(tokenIndices, out int a, out int b, out bool isPair))
            return false;

        bulletLine = isPair ? GetPairTokenJournalBullet(a, b) : GetSingleTokenJournalBullet(a);
        return !string.IsNullOrWhiteSpace(bulletLine);
    }

    private static bool TryNormalizeTokenIndices(
        IReadOnlyList<int> tokenIndices,
        out int lowerIndex,
        out int upperIndex,
        out bool isPair
    )
    {
        lowerIndex = 0;
        upperIndex = 0;
        isPair = false;

        if (tokenIndices == null || tokenIndices.Count == 0)
            return false;

        if (tokenIndices.Count == 1)
        {
            lowerIndex = tokenIndices[0];
            return lowerIndex >= 0 && lowerIndex <= 3;
        }

        if (tokenIndices.Count != 2)
            return false;

        int a = tokenIndices[0];
        int b = tokenIndices[1];
        if (a > b)
            (a, b) = (b, a);

        if (a < 0 || a > 3 || b < 0 || b > 3 || a == b)
            return false;

        lowerIndex = a;
        upperIndex = b;
        isPair = true;
        return true;
    }

    private string GetTokenPairText(int lowerIndex, int upperIndex)
    {
        if (lowerIndex == 0 && upperIndex == 1) return tokenPair1And2;
        if (lowerIndex == 0 && upperIndex == 2) return tokenPair1And3;
        if (lowerIndex == 0 && upperIndex == 3) return tokenPair1And4;
        if (lowerIndex == 1 && upperIndex == 2) return tokenPair2And3;
        if (lowerIndex == 1 && upperIndex == 3) return tokenPair2And4;
        if (lowerIndex == 2 && upperIndex == 3) return tokenPair3And4;
        return string.Empty;
    }

    private string GetSingleTokenJournalBullet(int tokenIndex)
    {
        switch (tokenIndex)
        {
            case 0: return journalToken1Body;
            case 1: return journalToken2Body;
            case 2: return journalToken3Body;
            case 3: return journalToken4Body;
            default: return string.Empty;
        }
    }

    private string GetPairTokenJournalBullet(int lowerIndex, int upperIndex)
    {
        if (lowerIndex == 0 && upperIndex == 1) return journalPair1And2Body;
        if (lowerIndex == 0 && upperIndex == 2) return journalPair1And3Body;
        if (lowerIndex == 0 && upperIndex == 3) return journalPair1And4Body;
        if (lowerIndex == 1 && upperIndex == 2) return journalPair2And3Body;
        if (lowerIndex == 1 && upperIndex == 3) return journalPair2And4Body;
        if (lowerIndex == 2 && upperIndex == 3) return journalPair3And4Body;
        return string.Empty;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 p = InteractionWorldPosition;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(p.x, p.y, 0f), 0.2f);
    }
}
