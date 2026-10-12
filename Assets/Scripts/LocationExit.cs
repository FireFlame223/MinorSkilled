using UnityEngine;

/// <summary>
/// Walking onto this cell leaves the current location and returns the player to the entrance they used.
/// Place the object on the exit tile. A physics collider is not required.
/// </summary>
public class LocationExit : MonoBehaviour
{
    [Tooltip("The grid this exit is standing on. An exit inside the Tower uses the Tower map.")]
    [SerializeField] private MapArea map;

    [Tooltip("Optional. If unset, this object's position is the exit cell.")]
    [SerializeField] private Transform exitPoint;

    public MapArea Map => map;

    public Vector2 WorldPosition
    {
        get
        {
            Transform point = exitPoint != null ? exitPoint : transform;
            return point.position;
        }
    }
}
