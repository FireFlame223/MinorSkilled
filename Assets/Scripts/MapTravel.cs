using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Switches the cursor between map areas when the player interacts with a <see cref="MapSwitcher"/>.
/// </summary>
[DefaultExecutionOrder(100)]
public class MapTravel : MonoBehaviour
{
    [SerializeField] private GridCursor cursor;

    [SerializeField] private MapInteraction mapInteraction;

    [SerializeField] private MapArea startingArea;

    private readonly Dictionary<Vector3Int, MapSwitcher> _switchersByCell = new Dictionary<Vector3Int, MapSwitcher>();

    private MapArea _currentArea;

    private void Awake()
    {
        if (cursor == null)
            cursor = FindFirstObjectByType<GridCursor>();

        if (mapInteraction == null)
            mapInteraction = FindFirstObjectByType<MapInteraction>();

        if (cursor == null)
        {
            Debug.LogError("MapTravel: assign the Grid Cursor.", this);
            enabled = false;
            return;
        }

        if (startingArea == null)
        {
            Debug.LogError("MapTravel: assign the starting map.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!isActiveAndEnabled)
            return;

        MapArea[] areas = FindObjectsByType<MapArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < areas.Length; i++)
        {
            if (areas[i] != startingArea)
                areas[i].gameObject.SetActive(false);
        }

        SwitchTo(startingArea, startingArea.StartCell);
        WarnAboutMissingMaps();
    }

    /// <summary>Switches maps through the door on this cell. Returns false when the cell has no switcher.</summary>
    public bool TryEnter(Vector3Int cell)
    {
        if (!_switchersByCell.TryGetValue(cell, out MapSwitcher here))
            return false;

        if (!TryGetDestination(here, out MapSwitcher destination))
            return true;

        if (destination.Map == null)
        {
            Debug.LogError("MapSwitcher: assign the map this object stands on.", destination);
            return true;
        }

        Vector3Int arrivalCell = destination.Map.WorldToCell(destination.transform.position);
        SwitchTo(destination.Map, arrivalCell);
        return true;
    }

    private bool SwitchTo(MapArea area, Vector3Int cell)
    {
        if (area == null || cursor == null)
            return false;

        if (!area.TryGetMovement(out Grid grid, out Tilemap walkable, out Vector2Int origin, out Vector2Int size))
        {
            Debug.LogError("MapTravel: assign the grid, walkable tilemap, and map size.", area);
            return false;
        }

        if (IsUnderArea(cursor.transform) ||
            IsUnderArea(transform) ||
            (mapInteraction != null && IsUnderArea(mapInteraction.transform)))
        {
            Debug.LogError("MapTravel: keep the cursor, Map Travel, and Map Interaction off the map roots, or those roots cannot be turned off.", this);
            return false;
        }

        if (_currentArea != null && _currentArea != area)
            _currentArea.gameObject.SetActive(false);

        area.gameObject.SetActive(true);
        cursor.UseMap(grid, walkable, origin, size, cell);
        _currentArea = area;

        if (mapInteraction != null)
            mapInteraction.SetActiveArea(area);

        RebuildSwitchers();
        return true;
    }

    private static bool TryGetDestination(MapSwitcher here, out MapSwitcher destination)
    {
        destination = here.Destination;
        if (destination != null && destination != here)
            return true;

        MapSwitcher[] switchers = FindObjectsByType<MapSwitcher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        MapSwitcher partner = null;
        int count = 0;
        for (int i = 0; i < switchers.Length; i++)
        {
            if (switchers[i] == here || switchers[i].Destination != here)
                continue;

            count++;
            partner = switchers[i];
        }

        if (count == 1)
        {
            destination = partner;
            return true;
        }

        Debug.LogError(
            count == 0
                ? "MapSwitcher: assign a destination."
                : "MapSwitcher: more than one switcher points here, so the destination is ambiguous.",
            here
        );
        destination = null;
        return false;
    }

    private bool IsUnderArea(Transform target)
    {
        if (target == null || _currentArea == null)
            return false;

        return target.IsChildOf(_currentArea.transform);
    }

    private void RebuildSwitchers()
    {
        _switchersByCell.Clear();

        if (_currentArea == null)
            return;

        MapSwitcher[] switchers = FindObjectsByType<MapSwitcher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < switchers.Length; i++)
        {
            MapSwitcher switcher = switchers[i];
            if (switcher.Map != _currentArea)
                continue;

            Vector3Int cell = _currentArea.WorldToCell(switcher.transform.position);
            if (_switchersByCell.ContainsKey(cell))
            {
                Debug.LogWarning(
                    $"MapTravel: multiple switchers share cell {cell}; using the last one registered.",
                    switcher
                );
            }

            _switchersByCell[cell] = switcher;
        }
    }

    private static void WarnAboutMissingMaps()
    {
        MapSwitcher[] switchers = FindObjectsByType<MapSwitcher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < switchers.Length; i++)
        {
            if (switchers[i].Map == null)
                Debug.LogError("MapSwitcher: assign the map this object stands on.", switchers[i]);
        }
    }
}
