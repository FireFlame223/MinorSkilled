using UnityEngine;

/// <summary>
/// Pressing interact on this cell returns the player to the entrance they used.
/// The player also appears here when they enter from that entrance.
/// </summary>
public class LocationExit : MonoBehaviour
{
    [Tooltip("The grid this exit is standing on. An exit inside the Tower uses the Tower map.")]
    [SerializeField] private MapArea map;

    public MapArea Map => map;

    private void OnDrawGizmosSelected()
    {
        Vector2 p = transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(p.x, p.y, 0f), 0.2f);
    }
}
