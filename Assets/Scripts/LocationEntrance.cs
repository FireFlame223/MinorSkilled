using UnityEngine;

/// <summary>
/// Pressing interact on this cell switches to another <see cref="MapArea"/>. This is not a POI and does not write to the journal.
/// </summary>
public class LocationEntrance : MonoBehaviour
{
    [Tooltip("The grid this object is standing on. A door on the main map uses the main map, even when it leads into the Tower.")]
    [SerializeField] private MapArea map;

    [Tooltip("The grid the player moves to. The player appears on that grid's Location Exit.")]
    [SerializeField] private MapArea destination;

    public MapArea Map => map;

    public MapArea Destination => destination;

    private void OnDrawGizmosSelected()
    {
        Vector2 p = transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(p.x, p.y, 0f), 0.2f);
    }
}
