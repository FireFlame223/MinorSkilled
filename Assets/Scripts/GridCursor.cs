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
    [Header("Movement")]
    [Tooltip("Seconds the cursor takes to slide from one cell to the next. Once continuous movement has started, further cells follow this pace. Zero snaps instantly.")]
    [SerializeField] private float moveDuration = 0.15f;

    [Tooltip("Seconds a direction must be held before the cursor keeps moving. A shorter press moves one cell. Zero repeats as soon as each slide finishes.")]
    [SerializeField] private float holdDelay = 0.4f;

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

    private bool isSliding;
    private float slideElapsed;
    private Vector3 slideStart;
    private Vector3 slideEnd;
    private Vector3Int queuedDirection;
    private Vector3Int repeatDirection;
    private float holdTimer;
    private bool repeating;

    private void Update()
    {
        if (grid == null || walkableTilemap == null)
            return;

        if (isSliding)
            AdvanceSlide();

        if (!CanMove || IsJournalBlockingMovement())
        {
            ClearHeldMovement();
            return;
        }

        bool stepFromPress = false;
        Vector3Int pressed = ReadPressedDirection();
        if (pressed != Vector3Int.zero)
        {
            repeatDirection = pressed;
            holdTimer = 0f;
            repeating = holdDelay <= 0f;
            if (isSliding)
                queuedDirection = pressed;
            else
                stepFromPress = true;
        }
        else if (IsDirectionHeld(repeatDirection))
        {
            holdTimer += Time.deltaTime;
            if (holdDelay <= 0f || holdTimer >= holdDelay)
                repeating = true;
        }
        else
        {
            repeatDirection = ReadHeldDirection();
            holdTimer = 0f;
            repeating = false;
        }

        if (isSliding)
            return;

        Vector3Int direction = Vector3Int.zero;
        if (stepFromPress)
            direction = repeatDirection;
        else if (queuedDirection != Vector3Int.zero)
            direction = queuedDirection;
        else if (repeating && IsDirectionHeld(repeatDirection))
            direction = repeatDirection;

        queuedDirection = Vector3Int.zero;
        TryStep(direction);
    }

    /// <summary>Switches the cursor onto another grid and places it on <paramref name="cell"/>.</summary>
    public void UseMap(Grid newGrid, Tilemap walkable, Vector2Int newMinCell, Vector2Int newMapSize, Vector3Int cell)
    {
        grid = newGrid;
        walkableTilemap = walkable;
        minCell = newMinCell;
        mapSize = newMapSize;
        SelectedCell = ClampToMap(cell);
        isSliding = false;
        ClearHeldMovement();
        UpdatePosition();
    }

    private void ClearHeldMovement()
    {
        queuedDirection = Vector3Int.zero;
        repeatDirection = Vector3Int.zero;
        holdTimer = 0f;
        repeating = false;
    }

    private void TryStep(Vector3Int direction)
    {
        if (direction == Vector3Int.zero)
            return;

        Vector3Int nextCell = SelectedCell + direction;
        if (!IsInsideMap(nextCell) || !IsWalkable(nextCell))
            return;

        SelectedCell = nextCell;
        Vector3 target = CellWorldPosition(nextCell);

        if (moveDuration <= 0f)
        {
            transform.position = target;
            return;
        }

        slideStart = transform.position;
        slideStart.z = target.z;
        slideEnd = target;
        slideElapsed = 0f;
        isSliding = true;
    }

    private void AdvanceSlide()
    {
        slideElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(slideElapsed / moveDuration);
        t = t * t * (3f - 2f * t);

        Vector3 position = Vector3.Lerp(slideStart, slideEnd, t);
        position.z = slideEnd.z;
        transform.position = position;

        if (slideElapsed < moveDuration)
            return;

        transform.position = slideEnd;
        isSliding = false;
    }

    private bool IsJournalBlockingMovement()
    {
        return lockMovementWhileJournalOpen && journal.IsOpen;
    }

    private Vector3Int ReadPressedDirection()
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

    private Vector3Int ReadHeldDirection()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector3Int.zero;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            return Vector3Int.up;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            return Vector3Int.down;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            return Vector3Int.left;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            return Vector3Int.right;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            return Vector3Int.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            return Vector3Int.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            return Vector3Int.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            return Vector3Int.right;
#endif

        return Vector3Int.zero;
    }

    private bool IsDirectionHeld(Vector3Int direction)
    {
        if (direction == Vector3Int.zero)
            return false;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        if (direction == Vector3Int.up)
            return keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        if (direction == Vector3Int.down)
            return keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        if (direction == Vector3Int.left)
            return keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        if (direction == Vector3Int.right)
            return keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (direction == Vector3Int.up)
            return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        if (direction == Vector3Int.down)
            return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        if (direction == Vector3Int.left)
            return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        if (direction == Vector3Int.right)
            return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
#endif

        return false;
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
        transform.position = CellWorldPosition(SelectedCell);
    }

    private Vector3 CellWorldPosition(Vector3Int cell)
    {
        Vector3 worldPosition = grid.GetCellCenterWorld(cell);
        worldPosition.z = transform.position.z;
        return worldPosition;
    }

    private bool IsWalkable(Vector3Int cell)
    {
        return walkableTilemap.HasTile(cell);
    }
}
