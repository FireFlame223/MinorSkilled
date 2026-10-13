using UnityEngine;
using UnityEngine.Tilemaps;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Grid cursor: WASD/arrows move one cell on the walkable tilemap; exposes <see cref="SelectedCell"/> for interactions.
/// </summary>
public class GridCursor : MonoBehaviour
{
    [Header("Journal focus")]
    [Tooltip("While the journal is open, grid movement is disabled so A/D and arrows flip pages.")]
    [SerializeField] private bool lockMovementWhileJournalOpen = true;

    [SerializeField] private JournalUI journal;

    public Vector3Int SelectedCell { get; private set; }

    /// <summary>When false, movement input is ignored (e.g. while the interaction panel is open).</summary>
    public bool CanMove { get; set; } = true;

    /// <summary>True when WASD and arrow keys are currently used to move the cursor.</summary>
    public bool AcceptsMovementInput => isActiveAndEnabled && CanMove && !IsJournalBlockingMovement();

    private Grid grid;
    private Tilemap walkableTilemap;
    private Vector2Int minCell;
    private Vector2Int mapSize;

    private void Update()
    {
        if (grid == null || walkableTilemap == null)
            return;

        if (!CanMove || IsJournalBlockingMovement())
            return;

        Vector3Int direction = ReadDirection();
        if (direction == Vector3Int.zero)
            return;

        Vector3Int nextCell = SelectedCell + direction;
        if (!IsInsideMap(nextCell) || !IsWalkable(nextCell))
            return;

        SelectedCell = nextCell;
        UpdatePosition();
    }

    /// <summary>Switches the cursor onto another grid and places it on <paramref name="cell"/>.</summary>
    public void UseMap(Grid newGrid, Tilemap walkable, Vector2Int newMinCell, Vector2Int newMapSize, Vector3Int cell)
    {
        grid = newGrid;
        walkableTilemap = walkable;
        minCell = newMinCell;
        mapSize = newMapSize;
        SelectedCell = ClampToMap(cell);
        UpdatePosition();
    }

    private bool IsJournalBlockingMovement()
    {
        return lockMovementWhileJournalOpen && journal.IsOpen;
    }

    private Vector3Int ReadDirection()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector3Int.zero;

        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            return Vector3Int.up;
        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            return Vector3Int.down;
        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            return Vector3Int.left;
        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            return Vector3Int.right;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            return Vector3Int.up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            return Vector3Int.down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            return Vector3Int.left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            return Vector3Int.right;
#endif

        return Vector3Int.zero;
    }

    private Vector3Int ClampToMap(Vector3Int cell)
    {
        return new Vector3Int(
            Mathf.Clamp(cell.x, minCell.x, minCell.x + mapSize.x - 1),
            Mathf.Clamp(cell.y, minCell.y, minCell.y + mapSize.y - 1),
            0
        );
    }

    private bool IsInsideMap(Vector3Int cell)
    {
        return cell.x >= minCell.x &&
               cell.y >= minCell.y &&
               cell.x < minCell.x + mapSize.x &&
               cell.y < minCell.y + mapSize.y;
    }

    private void UpdatePosition()
    {
        Vector3 worldPosition = grid.GetCellCenterWorld(SelectedCell);
        worldPosition.z = transform.position.z;
        transform.position = worldPosition;
    }

    private bool IsWalkable(Vector3Int cell)
    {
        return walkableTilemap.HasTile(cell);
    }
}
