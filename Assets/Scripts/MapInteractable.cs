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
    [SerializeField] private string menuOption1;

    [TextArea(3, 8)]
    [SerializeField] private string menuOption2;

    [TextArea(3, 8)]
    [SerializeField] private string menuOption3;

    [TextArea(3, 8)]
    [SerializeField] private string menuOption4;

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

    [Header("Journal")]
    [Tooltip("Stable id for save data. If empty, the GameObject name is used.")]
    [SerializeField] private string journalId;

    [SerializeField] private string journalTitle;

    [TextArea(3, 8)]
    [SerializeField] private string journalDescription;

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
            case 0: return menuOption1;
            case 1: return menuOption2;
            case 2: return menuOption3;
            case 3: return menuOption4;
            default: return string.Empty;
        }
    }

    public string GetTokenChoiceText(IReadOnlyList<int> tokenIndices)
    {
        if (tokenIndices == null || tokenIndices.Count == 0)
            return string.Empty;

        if (tokenIndices.Count == 1)
            return GetMenuOptionText(tokenIndices[0]);

        if (tokenIndices.Count != 2)
            return string.Empty;

        int a = tokenIndices[0];
        int b = tokenIndices[1];
        if (a > b)
            (a, b) = (b, a);

        return GetTokenPairText(a, b);
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

    private void OnDrawGizmosSelected()
    {
        Vector2 p = InteractionWorldPosition;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(p.x, p.y, 0f), 0.2f);
    }
}
