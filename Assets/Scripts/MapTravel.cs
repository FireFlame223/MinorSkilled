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
    }

    /// <summary>Switches maps through the door on this cell. Returns false when the cell has no switcher.</summary>
    public bool TryEnter(Vector3Int cell)
    {
        if (!_switchersByCell.TryGetValue(cell, out MapSwitcher here))
            return false;

        MapSwitcher destination = here.Destination;
        Vector3Int arrivalCell = destination.Map.WorldToCell(destination.transform.position);
        SwitchTo(destination.Map, arrivalCell);
        return true;
    }

    private void SwitchTo(MapArea area, Vector3Int cell)
    {
        area.GetMovement(out Grid grid, out Tilemap walkable, out Vector2Int origin, out Vector2Int size);

        if (_currentArea != null && _currentArea != area)
            _currentArea.gameObject.SetActive(false);

        area.gameObject.SetActive(true);
        cursor.UseMap(grid, walkable, origin, size, cell);
        _currentArea = area;

        mapInteraction.SetActiveArea(area);

        RebuildSwitchers();
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
            _switchersByCell[cell] = switcher;
        }
    }
}
