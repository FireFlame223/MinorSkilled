using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Hides each tutorial line the first time the player does the action it describes.
/// Put this on the Tutorial object; hint objects are found by name when not assigned.
/// </summary>
public class TutorialHints : MonoBehaviour
{
    [Header("Hint objects")]
    [SerializeField] private GameObject title;
    [SerializeField] private GameObject movementText;
    [SerializeField] private GameObject interactionText;
    [SerializeField] private GameObject journalText;
    [SerializeField] private GameObject hypothesisText;

    [Header("Sources")]
    [SerializeField] private GridCursor cursor;
    [SerializeField] private MapInteraction mapInteraction;
    [SerializeField] private JournalUI journal;

    private bool _pressedUp;
    private bool _pressedDown;
    private bool _pressedLeft;
    private bool _pressedRight;

    private void Awake()
    {
        title = ResolveHint(title, "Title");
        movementText = ResolveHint(movementText, "MovementText");
        interactionText = ResolveHint(interactionText, "InteractionText");
        journalText = ResolveHint(journalText, "JournalText");
        hypothesisText = ResolveHint(hypothesisText, "HypothesisText");

        if (cursor == null)
            cursor = FindFirstObjectByType<GridCursor>();

        if (mapInteraction == null)
            mapInteraction = FindFirstObjectByType<MapInteraction>();

        if (journal == null)
            journal = FindFirstObjectByType<JournalUI>();

        if (cursor == null)
            Debug.LogError("TutorialHints: assign the Grid Cursor.", this);

        if (mapInteraction == null)
            Debug.LogError("TutorialHints: assign Map Interaction.", this);

        if (journal == null)
            Debug.LogError("TutorialHints: assign Journal UI.", this);
    }

    private void OnEnable()
    {
        if (mapInteraction != null)
            mapInteraction.SuccessfulInteraction += HideInteractionHint;

        if (journal != null)
            journal.Opened += HideJournalHint;
    }

    private void OnDisable()
    {
        if (mapInteraction != null)
            mapInteraction.SuccessfulInteraction -= HideInteractionHint;

        if (journal != null)
            journal.Opened -= HideJournalHint;
    }

    private void Update()
    {
        TrackMovementPresses();

        if (hypothesisText != null && hypothesisText.activeSelf && WasSpacePressed())
            HideHint(hypothesisText);
    }

    private void TrackMovementPresses()
    {
        if (movementText == null || !movementText.activeSelf)
            return;

        if (cursor != null && !cursor.AcceptsMovementInput)
            return;

        ReadMovementPresses();

        if (_pressedUp && _pressedDown && _pressedLeft && _pressedRight)
            HideHint(movementText);
    }

    private void ReadMovementPresses()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            _pressedUp = true;
        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            _pressedDown = true;
        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            _pressedLeft = true;
        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            _pressedRight = true;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            _pressedUp = true;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            _pressedDown = true;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            _pressedLeft = true;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            _pressedRight = true;
#endif
    }

    private void HideInteractionHint()
    {
        HideHint(interactionText);
    }

    private void HideJournalHint()
    {
        HideHint(journalText);
    }

    private void HideHint(GameObject hint)
    {
        if (hint == null || !hint.activeSelf)
            return;

        hint.SetActive(false);

        if (title == null || !title.activeSelf || AnyHintVisible())
            return;

        title.SetActive(false);
    }

    private bool AnyHintVisible()
    {
        return IsVisible(movementText) ||
               IsVisible(interactionText) ||
               IsVisible(journalText) ||
               IsVisible(hypothesisText);
    }

    private static bool IsVisible(GameObject hint)
    {
        return hint != null && hint.activeSelf;
    }

    private static bool WasSpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    private GameObject ResolveHint(GameObject assigned, string objectName)
    {
        if (assigned != null)
            return assigned;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
                return children[i].gameObject;
        }

        GameObject found = GameObject.Find(objectName);
        if (found != null)
            return found;

        Debug.LogError($"TutorialHints: assign {objectName}.", this);
        return null;
    }
}
