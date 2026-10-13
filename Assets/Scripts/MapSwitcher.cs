using UnityEngine;

/// <summary>
/// Interact moves the player to <see cref="Destination"/> and shows that switcher's map.
/// If only one side of a pair assigns Destination, the link still works both ways.
/// </summary>
public class MapSwitcher : MonoBehaviour
{
    [Tooltip("The grid this object stands on.")]
    [SerializeField] private MapArea map;

    [Tooltip("The switcher the player moves to.")]
    [SerializeField] private MapSwitcher destination;

    public MapArea Map => map;

    public MapSwitcher Destination => destination;

    private void OnDrawGizmosSelected()
    {
        Vector2 p = transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(p.x, p.y, 0f), 0.2f);
    }
}
