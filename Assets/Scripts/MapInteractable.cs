using UnityEngine;

/// <summary>
/// Place on a character, prop, or empty grid marker. Interaction uses the grid cell under
/// <see cref="interactionPoint"/> (or this transform). Tilemap art can stay painted; snap this object to the cell center.
/// </summary>
public class MapInteractable : MonoBehaviour
{
    [Tooltip("Optional. If unset, this object's position defines the interaction cell.")]
    [SerializeField] private Transform interactionPoint;

    [Header("Interaction")]
    [SerializeField] private string displayName;

    [TextArea(3, 8)]
    [SerializeField] private string description;

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

    private void OnDrawGizmosSelected()
    {
        Vector2 p = InteractionWorldPosition;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(p.x, p.y, 0f), 0.2f);
    }
}
