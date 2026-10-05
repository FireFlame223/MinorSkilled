using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Handles interact input, maps grid cells to <see cref="MapInteractable"/> POIs, and drives the interaction UI and journal discoveries.
/// </summary>
public class MapInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridCursor cursor;

    [SerializeField] private InteractionPanelUI interactionPanel;

    [SerializeField] private JournalDiscoveryLog journalDiscoveryLog;

    [Tooltip("If set, only MapInteractables under this transform are registered. Otherwise, all in the scene are used.")]
    [SerializeField] private Transform interactablesRoot;

    private Dictionary<Vector3Int, MapInteractable> _interactablesByCell;

    private void Awake()
    {
        if (cursor == null)
        {
            Debug.LogError("MapInteraction: assign the Grid Cursor.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!isActiveAndEnabled)
            return;

        BuildInteractableLookup();
    }

    private void BuildInteractableLookup()
    {
        _interactablesByCell = new Dictionary<Vector3Int, MapInteractable>();

        MapInteractable[] interactables = CollectInteractables();
        foreach (MapInteractable interactable in interactables)
        {
            if (interactable == null)
                continue;

            Vector3Int gridCell = cursor.WorldToCell(interactable.InteractionWorldPosition);

            if (_interactablesByCell.ContainsKey(gridCell))
            {
                Debug.LogWarning(
                    $"MapInteraction: multiple interactables share cell {gridCell}; using the last one registered.",
                    interactable
                );
            }

            _interactablesByCell[gridCell] = interactable;
        }
    }

    private MapInteractable[] CollectInteractables()
    {
        if (interactablesRoot != null)
            return interactablesRoot.GetComponentsInChildren<MapInteractable>(true);

        return FindObjectsByType<MapInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (cursor == null || !cursor.isActiveAndEnabled)
            return;

        if (!WasInteractPressed())
            return;

        if (interactionPanel != null && interactionPanel.IsVisible)
        {
            if (interactionPanel.BlocksDismissal)
                return;

            if (interactionPanel.HasPendingTokenSelection)
            {
                interactionPanel.ConfirmTokenSelection();
                return;
            }

            interactionPanel.Hide();
            cursor.CanMove = true;
            return;
        }

        Interact();
    }

    private void Interact()
    {
        Vector3Int selectedCell = cursor.SelectedCell;

        if (!_interactablesByCell.TryGetValue(selectedCell, out MapInteractable interactable))
        {
            Debug.Log("There is nothing to interact with here.", this);
            return;
        }

        bool firstVisit = !interactable.Explored;
        interactable.Explored = true;

        if (firstVisit && journalDiscoveryLog != null)
            journalDiscoveryLog.RecordDiscovery(interactable);

        if (interactionPanel == null)
        {
            Debug.LogError("MapInteraction: assign Interaction Panel UI.", this);
            return;
        }

        interactionPanel.Show(interactable);
        cursor.CanMove = false;
    }

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.eKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame);

#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E) ||
               Input.GetKeyDown(KeyCode.Return) ||
               Input.GetKeyDown(KeyCode.KeypadEnter);
#else
        return false;
#endif
    }
}
