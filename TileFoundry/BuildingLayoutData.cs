using System;
using UnityEngine;

/// <summary>
/// Serializable data structure for storing a building layout,
/// including all tile layers and edge toggle data.
/// This is saved as JSON and reloaded to reconstruct grid state.
/// </summary>
[Serializable]
public class BuildingLayoutData
{
    // Grid dimensions (width * height = total number of cells).
    public int width;
    public int height;

    // One-dimensional arrays used for serializing each layer.
    // These are flattened from [x,y] to [y * width + x] for easy JSON storage.
    public string[] groundtiles;     // Ground layer
    public string[] itemTiles;       // Item layer
    public string[] overlayTiles;    // Overlay layer
    public string[] wallTiles;       // Wall layer
    public string[] nodeTiles;       // Node (logic) layer
    public string[] furnitureTiles;  // Furniture/prop layer

    // Optional category tag (enum stored as string).
    public string layoutCategory;

    // Border toggle arrays (used to define open connections on edges).
    public bool[] topEdgeToggles;
    public bool[] bottomEdgeToggles;
    public bool[] leftEdgeToggles;
    public bool[] rightEdgeToggles;

    // Legacy field for backward compatibility with older layouts.
    public string[] tiles;

    /// <summary>
    /// Creates a new BuildingLayoutData object and flattens all grid layers into 1D arrays.
    /// </summary>
    public BuildingLayoutData(int width, int height, string[,] groundGrid,
        LayoutCategory category,
        bool[] top, bool[] bottom, bool[] left, bool[] right,
        string[,] itemGrid = null, string[,] overlayGrid = null,
        string[,] wallGrid = null, string[,] nodeGrid = null, string[,] furnitureGrid = null)
    {
        this.width = width;
        this.height = height;
        int totalCells = width * height;

        // Initialize layer arrays to match flattened dimensions.
        groundtiles = new string[totalCells];
        itemTiles = new string[totalCells];
        overlayTiles = new string[totalCells];
        wallTiles = new string[totalCells];
        nodeTiles = new string[totalCells];
        furnitureTiles = new string[totalCells];

        layoutCategory = category.ToString();

        // Clone edge toggles to avoid reference issues.
        topEdgeToggles = (bool[])top.Clone();
        bottomEdgeToggles = (bool[])bottom.Clone();
        leftEdgeToggles = (bool[])left.Clone();
        rightEdgeToggles = (bool[])right.Clone();

        // Flatten all layer data into 1D arrays.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                groundtiles[index] = groundGrid != null ? groundGrid[x, y] : "";
                itemTiles[index] = itemGrid != null ? itemGrid[x, y] : "";
                overlayTiles[index] = overlayGrid != null ? overlayGrid[x, y] : "";
                wallTiles[index] = wallGrid != null ? wallGrid[x, y] : "";
                nodeTiles[index] = nodeGrid != null ? nodeGrid[x, y] : "";
                furnitureTiles[index] = furnitureGrid != null ? furnitureGrid[x, y] : "";
            }
        }
    }

    // ───────────────────────────────────────────────────────────────
    // Grid Conversion Methods
    // Convert flattened tile arrays back into [x,y] 2D arrays.
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts the flattened groundtiles array into a 2D [x,y] grid.
    /// </summary>
    public string[,] ToGroundGrid()
    {
        var grid = new string[width, height];
        if (groundtiles == null || groundtiles.Length != width * height)
        {
            Debug.LogWarning("Groundtiles array is not valid. Initializing empty grid.");
            return EmptyGrid(grid);
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = groundtiles[y * width + x];

        return grid;
    }

    /// <summary>
    /// Converts the item layer into a 2D grid.
    /// </summary>
    public string[,] ToItemGrid()
    {
        var grid = new string[width, height];
        if (itemTiles == null || itemTiles.Length != width * height)
        {
            Debug.LogWarning("itemTiles array is not valid. Initializing empty grid.");
            return EmptyGrid(grid);
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = itemTiles[y * width + x];

        return grid;
    }

    /// <summary>
    /// Converts the overlay layer into a 2D grid.
    /// </summary>
    public string[,] ToOverlayGrid()
    {
        var grid = new string[width, height];
        if (overlayTiles == null || overlayTiles.Length != width * height)
        {
            Debug.LogWarning("overlayTiles array is not valid. Initializing empty grid.");
            return EmptyGrid(grid);
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = overlayTiles[y * width + x];

        return grid;
    }

    /// <summary>
    /// Converts the wall layer into a 2D grid.
    /// </summary>
    public string[,] ToWallGrid()
    {
        var grid = new string[width, height];
        if (wallTiles == null || wallTiles.Length != width * height)
        {
            Debug.LogWarning("wallTiles array is not valid. Initializing empty grid.");
            return EmptyGrid(grid);
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = wallTiles[y * width + x];

        return grid;
    }

    /// <summary>
    /// Converts the node layer into a 2D grid.
    /// </summary>
    public string[,] ToNodeGrid()
    {
        var grid = new string[width, height];
        if (nodeTiles == null || nodeTiles.Length != width * height)
        {
            Debug.LogWarning("nodeTiles array is not valid. Initializing empty grid.");
            return EmptyGrid(grid);
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = nodeTiles[y * width + x];

        return grid;
    }

    /// <summary>
    /// Converts the furniture layer into a 2D grid.
    /// </summary>
    public string[,] ToFurnitureGrid()
    {
        var grid = new string[width, height];
        if (furnitureTiles == null || furnitureTiles.Length != width * height)
        {
            Debug.LogWarning("furnitureTiles array is not valid. Initializing empty grid.");
            return EmptyGrid(grid);
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = furnitureTiles[y * width + x];

        return grid;
    }

    /// <summary>
    /// Legacy fallback: returns ground layer grid, or an empty one if unavailable.
    /// </summary>
    public string[,] ToGrid()
    {
        if (groundtiles != null && groundtiles.Length == width * height)
            return ToGroundGrid();

        Debug.LogWarning("No valid groundtiles array found. Returning empty grid.");
        var grid = new string[width, height];
        return EmptyGrid(grid);
    }

    /// <summary>
    /// Utility function to zero out a 2D grid with empty strings.
    /// </summary>
    private static string[,] EmptyGrid(string[,] grid)
    {
        for (int y = 0; y < grid.GetLength(1); y++)
            for (int x = 0; x < grid.GetLength(0); x++)
                grid[x, y] = "";
        return grid;
    }
}

/// <summary>
/// Category used to tag building layouts for filtering or theming purposes.
/// Stored as a string inside BuildingLayoutData for serialization convenience.
/// </summary>
public enum LayoutCategory
{
    Residential,
    Commercial,
    Industrial,
    Natural,
    Unique,
    Military,
    Infrastructure,
    Abandoned
}
