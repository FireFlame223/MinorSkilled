using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Switches the cursor between map areas. Interact on an entrance appears at that map's exit; interact on the exit returns to the entrance.
/// </summary>
[DefaultExecutionOrder(100)]
public class MapTravel : MonoBehaviour
{
    private struct ReturnPoint
    {
        public MapArea Area;
        public Vector3Int Cell;
    }

    [SerializeField] private GridCursor cursor;

    [SerializeField] private MapInteraction mapInteraction;

    [SerializeField] private MapArea startingArea;

    private readonly Dictionary<Vector3Int, LocationEntrance> _entrancesByCell = new Dictionary<Vector3Int, LocationEntrance>();
    private readonly HashSet<Vector3Int> _exitCells = new HashSet<Vector3Int>();
    private readonly Stack<ReturnPoint> _returnPoints = new Stack<ReturnPoint>();

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

    /// <summary>Enters or leaves through the door on this cell. Returns false when the cell has no door.</summary>
    public bool TryEnter(Vector3Int cell)
    {
        if (_exitCells.Contains(cell) && _returnPoints.Count > 0)
        {
            ReturnPoint returnPoint = _returnPoints.Peek();
            if (SwitchTo(returnPoint.Area, returnPoint.Cell))
                _returnPoints.Pop();

            return true;
        }

        if (!_entrancesByCell.TryGetValue(cell, out LocationEntrance entrance))
            return false;

        if (entrance.Destination == null)
        {
            Debug.LogError("LocationEntrance: assign a destination map.", entrance);
            return true;
        }

        if (entrance.Destination == _currentArea)
        {
            Debug.LogError("LocationEntrance: destination is the map the player is already on.", entrance);
            return true;
        }

        if (!TryGetExitCell(entrance.Destination, out Vector3Int arrivalCell))
            return true;

        MapArea returnArea = _currentArea;
        if (!SwitchTo(entrance.Destination, arrivalCell))
            return true;

        _returnPoints.Push(new ReturnPoint
        {
            Area = returnArea,
            Cell = cell
        });

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

        RebuildTravelPoints();
        return true;
    }

    private static bool TryGetExitCell(MapArea destination, out Vector3Int cell)
    {
        cell = default;
        if (destination == null)
            return false;

        LocationExit[] exits = FindObjectsByType<LocationExit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        LocationExit found = null;
        int count = 0;
        for (int i = 0; i < exits.Length; i++)
        {
            if (exits[i].Map != destination)
                continue;

            count++;
            found = exits[i];
        }

        if (count != 1)
        {
            Debug.LogError(
                count == 0
                    ? "MapTravel: the destination needs one Location Exit. The player appears on that object."
                    : "MapTravel: the destination has more than one Location Exit, so the arrival cell is ambiguous.",
                destination
            );
            return false;
        }

        cell = destination.WorldToCell(found.transform.position);
        return true;
    }

    private bool IsUnderArea(Transform target)
    {
        if (target == null)
            return false;

        if (_currentArea != null && target.IsChildOf(_currentArea.transform))
            return true;

        return false;
    }

    private void RebuildTravelPoints()
    {
        _entrancesByCell.Clear();
        _exitCells.Clear();

        if (_currentArea == null || cursor == null)
            return;

        LocationEntrance[] entrances = FindObjectsByType<LocationEntrance>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < entrances.Length; i++)
        {
            LocationEntrance entrance = entrances[i];
            if (entrance.Map != _currentArea)
                continue;

            Vector3Int cell = _currentArea.WorldToCell(entrance.transform.position);

            if (_entrancesByCell.ContainsKey(cell))
            {
                Debug.LogWarning(
                    $"MapTravel: multiple entrances share cell {cell}; using the last one registered.",
                    entrance
                );
            }

            _entrancesByCell[cell] = entrance;
        }

        LocationExit[] exits = FindObjectsByType<LocationExit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < exits.Length; i++)
        {
            if (exits[i].Map != _currentArea)
                continue;

            _exitCells.Add(_currentArea.WorldToCell(exits[i].transform.position));
        }
    }

    private static void WarnAboutMissingMaps()
    {
        LocationEntrance[] entrances = FindObjectsByType<LocationEntrance>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < entrances.Length; i++)
        {
            if (entrances[i].Map == null)
                Debug.LogError("LocationEntrance: assign the map this object stands on.", entrances[i]);
        }

        LocationExit[] exits = FindObjectsByType<LocationExit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < exits.Length; i++)
        {
            if (exits[i].Map == null)
                Debug.LogError("LocationExit: assign the map this object stands on.", exits[i]);
        }
    }
}
