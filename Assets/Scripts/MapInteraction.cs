using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Handles interact input, maps grid cells to POIs and location clues, and drives the interaction UI and journal discoveries.
/// </summary>
public class MapInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridCursor cursor;

    [SerializeField] private InteractionPanelUI interactionPanel;

    [SerializeField] private JournalDiscoveryLog journalDiscoveryLog;

    [SerializeField] private MapTravel mapTravel;

    private readonly Dictionary<Vector3Int, MapInteractable> _interactablesByCell = new Dictionary<Vector3Int, MapInteractable>();
    private readonly Dictionary<Vector3Int, LocationClue> _cluesByCell = new Dictionary<Vector3Int, LocationClue>();
    private MapArea _activeArea;

    /// <summary>Only POIs and clues whose Map field is this area can be interacted with.</summary>
    public void SetActiveArea(MapArea area)
    {
        _activeArea = area;

        if (isActiveAndEnabled)
            BuildInteractableLookup();
    }

    private void BuildInteractableLookup()
    {
        _interactablesByCell.Clear();
        _cluesByCell.Clear();

        if (_activeArea == null)
            return;

        MapInteractable[] interactables = FindObjectsByType<MapInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MapInteractable interactable in interactables)
        {
            if (interactable == null || interactable.Map != _activeArea)
                continue;

            Vector3Int gridCell = _activeArea.WorldToCell(interactable.transform.position);
            _interactablesByCell[gridCell] = interactable;
        }

        LocationClue[] clues = FindObjectsByType<LocationClue>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LocationClue clue in clues)
        {
            if (clue == null || clue.Map != _activeArea)
                continue;

            Vector3Int gridCell = _activeArea.WorldToCell(clue.transform.position);
            if (_interactablesByCell.ContainsKey(gridCell))
            {
                Debug.LogWarning(
                    $"LocationClue \"{clue.name}\" is on the same cell as a POI. The POI is used.",
                    clue
                );
            }

            _cluesByCell[gridCell] = clue;
        }
    }

    private void Update()
    {
        if (GameplayInput.IsBlocked)
            return;

        if (!cursor.isActiveAndEnabled)
            return;

        if (cursor.IsJournalOpen)
            return;

        if (!WasInteractPressed())
            return;

        if (interactionPanel.IsVisible)
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

        if (mapTravel.TryEnter(selectedCell))
            return;

        if (_interactablesByCell.TryGetValue(selectedCell, out MapInteractable interactable))
        {
            bool firstVisit = !interactable.Explored;
            interactable.Explored = true;

            if (firstVisit)
                journalDiscoveryLog.RecordDiscovery(interactable);

            interactionPanel.Show(interactable);
            cursor.CanMove = false;
            return;
        }

        if (!_cluesByCell.TryGetValue(selectedCell, out LocationClue clue))
            return;

        bool firstClueVisit = !clue.Explored;
        clue.Explored = true;

        if (firstClueVisit)
            journalDiscoveryLog.RecordLocationClue(clue);

        interactionPanel.Show(clue);
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
#else
        return false;
#endif
    }
}
