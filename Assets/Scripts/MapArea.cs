using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One grid the cursor can occupy. Each place (the world map, a tower, a house) is its own Map Area.
/// This object is hidden while the player is on a different grid. Travel points and POIs do not need to be children of it.
/// </summary>
public class MapArea : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Grid grid;

    [SerializeField] private Tilemap walkableTilemap;

    [SerializeField] private Vector2Int minCell = Vector2Int.zero;

    [SerializeField] private Vector2Int mapSize = new Vector2Int(10, 10);

    [Header("Start")]
    [Tooltip("Cell used when this map is the one the game starts on. Other maps place the player with a Map Switcher instead.")]
    [SerializeField] private Vector2Int startCell;

    public void GetMovement(out Grid mapGrid, out Tilemap walkable, out Vector2Int origin, out Vector2Int size)
    {
        mapGrid = grid;
        walkable = walkableTilemap;
        origin = minCell;
        size = mapSize;
    }

    public Vector3Int StartCell => new Vector3Int(startCell.x, startCell.y, 0);

    public Vector3Int WorldToCell(Vector2 worldPosition)
    {
        return grid.WorldToCell(new Vector3(worldPosition.x, worldPosition.y, 0f));
    }
}
