using UnityEngine;

/// <summary>
/// Pressing interact on this cell switches to another <see cref="MapArea"/>. This is not a POI and does not write to the journal.
/// </summary>
public class LocationEntrance : MonoBehaviour
{
    [Tooltip("The grid this object is standing on. A door on the main map uses the main map, even when it leads into the Tower.")]
    [SerializeField] private MapArea map;

    [Tooltip("The grid the player moves to.")]
    [SerializeField] private MapArea destination;

    [Tooltip("Cell on the destination grid where the player appears.")]
    [SerializeField] private Vector2Int entryCell;

    [Tooltip("Optional. If unset, this object's position is the cell that opens the location.")]
    [SerializeField] private Transform interactionPoint;

    public MapArea Map => map;

    public MapArea Destination => destination;

    public Vector3Int EntryCell => new Vector3Int(entryCell.x, entryCell.y, 0);

    public Vector2 WorldPosition
    {
        get
        {
            Transform point = interactionPoint != null ? interactionPoint : transform;
            return point.position;
        }
    }
}
