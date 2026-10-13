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

    [SerializeField] private MapTravel mapTravel;

    private Dictionary<Vector3Int, MapInteractable> _interactablesByCell;
    private MapArea _activeArea;

    /// <summary>Raised when interact input hits a POI (E or Enter).</summary>
    public event System.Action SuccessfulInteraction;

    private void Awake()
    {
        if (cursor == null)
        {
            Debug.LogError("MapInteraction: assign the Grid Cursor.", this);
            enabled = false;
        }

        if (mapTravel == null)
            mapTravel = FindFirstObjectByType<MapTravel>();
    }

    /// <summary>Only POIs whose Map field is this area can be interacted with.</summary>
    public void SetActiveArea(MapArea area)
    {
        _activeArea = area;

        if (cursor != null && isActiveAndEnabled)
            BuildInteractableLookup();
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

        if (_activeArea == null)
            return;

        MapInteractable[] interactables = FindObjectsByType<MapInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MapInteractable interactable in interactables)
        {
            if (interactable == null)
                continue;

            if (interactable.Map == null)
            {
                Debug.LogError("MapInteractable: assign the map this POI stands on.", interactable);
                continue;
            }

            if (interactable.Map != _activeArea)
                continue;

            Vector3Int gridCell = _activeArea.WorldToCell(interactable.transform.position);

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

            if (interactionPanel.IsTypewriterRunning)
            {
                interactionPanel.CompleteTypewriter();
                return;
            }

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

        if (mapTravel != null && mapTravel.TryEnter(selectedCell))
            return;

        if (!_interactablesByCell.TryGetValue(selectedCell, out MapInteractable interactable))
        {
            Debug.Log("There is nothing to interact with here.", this);
            return;
        }

        SuccessfulInteraction?.Invoke();

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
