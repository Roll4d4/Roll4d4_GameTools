using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a grid of string-based tile references and associated metadata like edge toggles.
/// This class supports multiple logical layers (ground, items, overlays, etc.)
/// and provides utility functions for painting, clearing, and path-based painting (line/fill).
/// </summary>
public class GridController
{
    // Grid dimensions
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }

    /// <summary>
    /// Returns the larger of width or height, useful for tools that assume square logic.
    /// </summary>
    public int GridSize => Mathf.Max(GridWidth, GridHeight);

    // Named layers using 2D string arrays for tile keys
    public string[,] GroundGrid { get; set; }
    public string[,] ItemGrid { get; set; }
    public string[,] OverlayGrid { get; set; }
    public string[,] WallsGrid { get; set; }
    public string[,] FurnitureGrid { get; set; }
    public string[,] NodeGrid { get; set; }

    // Rendering config
    public float CellSize { get; set; } = 20f;
    public float ToggleSize { get; private set; } = 20f;

    // Edge connectivity toggles
    public bool[] TopEdgeToggles { get; private set; }
    public bool[] BottomEdgeToggles { get; private set; }
    public bool[] LeftEdgeToggles { get; private set; }
    public bool[] RightEdgeToggles { get; private set; }

    /// <summary>
    /// Constructs a new grid with the specified dimensions.
    /// </summary>
    public GridController(int gridWidth = 20, int gridHeight = 20)
    {
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        InitializeGrid();
    }

    /// <summary>
    /// Initializes all grid layers and edge toggle arrays.
    /// </summary>
    public void InitializeGrid()
    {
        GroundGrid = new string[GridWidth, GridHeight];
        OverlayGrid = new string[GridWidth, GridHeight];
        ItemGrid = new string[GridWidth, GridHeight];
        WallsGrid = new string[GridWidth, GridHeight];
        FurnitureGrid = new string[GridWidth, GridHeight];
        NodeGrid = new string[GridWidth, GridHeight];

        // Default all cells to empty strings
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                GroundGrid[x, y] = "";
                ItemGrid[x, y] = "";
                OverlayGrid[x, y] = "";
            }
        }

        // Initialize edge toggles
        TopEdgeToggles = new bool[GridWidth];
        BottomEdgeToggles = new bool[GridWidth];
        LeftEdgeToggles = new bool[GridHeight];
        RightEdgeToggles = new bool[GridHeight];
    }

    /// <summary>
    /// Clears all grid data and reinitializes everything to blank state.
    /// </summary>
    public void ClearAllGrids()
    {
        InitializeGrid();
    }

    /// <summary>
    /// Sets a specific tile in the Ground layer. Other layers are unaffected.
    /// </summary>
    public void SetCell(int x, int y, string assetName)
    {
        if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
            GroundGrid[x, y] = string.IsNullOrEmpty(assetName) ? "" : assetName;
    }

    /// <summary>
    /// Replaces the grid and edge toggle arrays with new values (usually from deserialized layout data).
    /// </summary>
    public void SetGridState(string[,] newGrid, bool[] topToggles, bool[] bottomToggles, bool[] leftToggles, bool[] rightToggles)
    {
        GroundGrid = newGrid;

        GridWidth = newGrid.GetLength(0);
        GridHeight = newGrid.GetLength(1);

        TopEdgeToggles = topToggles;
        BottomEdgeToggles = bottomToggles;
        LeftEdgeToggles = leftToggles;
        RightEdgeToggles = rightToggles;
    }

    /// <summary>
    /// Returns a list of all grid coordinates along a straight line between two points using Bresenham's algorithm.
    /// </summary>
    public List<Vector2Int> BresenhamLine(Vector2Int start, Vector2Int end)
    {
        var points = new List<Vector2Int>();
        int gridSize = GridWidth; // assuming square for clamping

        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;
        int dx = Mathf.Abs(x1 - x0), sx = (x0 < x1) ? 1 : -1;
        int dy = Mathf.Abs(y1 - y0), sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < gridSize && y0 >= 0 && y0 < gridSize)
                points.Add(new Vector2Int(x0, y0));

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        return points;
    }

    /// <summary>
    /// Checks if a given point lies on the line between start and end using Bresenham’s algorithm.
    /// </summary>
    public bool IsPointOnLine(Vector2Int point, Vector2Int start, Vector2Int end)
    {
        return BresenhamLine(start, end).Contains(point);
    }

    /// <summary>
    /// Performs recursive 4-directional flood fill on a grid of tile strings.
    /// Fills all contiguous tiles matching the target asset with the replacement asset.
    /// </summary>
    public static void FloodFill(int x, int y, string targetAsset, string replacementAsset, int gridSize, string[,] grid)
    {
        if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return;
        if (targetAsset == replacementAsset) return;
        if (grid[x, y] != targetAsset) return;

        grid[x, y] = replacementAsset;

        FloodFill(x + 1, y, targetAsset, replacementAsset, gridSize, grid);
        FloodFill(x - 1, y, targetAsset, replacementAsset, gridSize, grid);
        FloodFill(x, y + 1, targetAsset, replacementAsset, gridSize, grid);
        FloodFill(x, y - 1, targetAsset, replacementAsset, gridSize, grid);
    }
}
