using UnityEngine;

/// <summary>
/// Map object with no tokens. The first interaction files one journal line on the location page named in <see cref="LocationPage"/>.
/// </summary>
public class LocationClue : MonoBehaviour
{
    [Tooltip("The grid this clue is standing on. It can only be used while the player is on this map. This does not choose the journal page.")]
    [SerializeField] private MapArea map;

    [Tooltip("Journal page this clue is filed under. Type the same text as that location's row in the journal roster, such as Clinic or Seth's House.")]
    [SerializeField] private string locationPage;

    [Header("Interaction")]
    [Tooltip("Title shown in the interaction panel.")]
    [SerializeField] private string displayName;

    [Tooltip("Body shown in the interaction panel.")]
    [TextArea(3, 8)]
    [SerializeField] private string description;

    [Header("Journal")]
    [Tooltip("Bullet added to the location page the first time the player reads this clue.")]
    [TextArea(3, 8)]
    [SerializeField] private string journalLine;

    public MapArea Map => map;

    public string LocationPage => locationPage;

    public string DisplayName => displayName;

    public string Description => description;

    public string JournalLine => journalLine;

    public bool Explored { get; set; }

    private void OnDrawGizmosSelected()
    {
        Vector2 p = transform.position;
        Gizmos.color = new Color(0.3f, 0.85f, 1f);
        Gizmos.DrawWireSphere(new Vector3(p.x, p.y, 0f), 0.2f);
    }
}
