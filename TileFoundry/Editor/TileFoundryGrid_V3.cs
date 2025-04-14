using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
/// <summary>
/// Available grid layers. These map to the layer index used in the editor and saved data.
/// </summary>
public enum GridLayer
{
    Ground = 0,
    Walls = 1,
    Furniture = 2,
    Item = 3,
    Overlay = 4,
    Node = 5
}

/// <summary>
/// Handles drawing and interaction logic for the central grid area of the Tile Foundery editor.
/// This includes zooming, panning, rendering cell layers, drawing edge toggles, and capturing mouse input.
/// </summary>
public static class TileFoundryGrid_V3
{
    // The current grid cell the mouse is hovering over.
    private static Vector2Int? hoverCoord = null;

    // The starting cell for shape tools like line, square, circle, and hex.
    private static Vector2Int? shapeCenter = null;

    // Tracks whether the user is currently dragging the mouse to paint.
    private static bool isDragging = false;

    // Caches the last painted cell to avoid re-painting the same one during drag.
    private static Vector2Int? lastPaintCoord = null;

    // Local texture preview cache for tiles (used in drawing tile previews on grid).
    private static Dictionary<string, Texture2D> tilePreviewCache = new();

    /// <summary>
    /// Main entry point for drawing the grid editor panel inside the Unity Editor window.
    /// </summary>
    /// <param name="position">Rect of the grid editor's screen area.</param>
    /// <param name="core">The main Tile Foundery window instance, providing access to settings and grid data.</param>
    public static void Draw(Rect position, TileFoundryCore_V3 core)
    {
        // Begin the entire grid editing area using a help box style.
        GUILayout.BeginArea(position, EditorStyles.helpBox);
        {
            // ───────────────────────────────────────
            // Zoom Slider at the Top
            // ───────────────────────────────────────
            float zoomHeight = 30f;
            Rect zoomRect = new Rect(0, 0, position.width, zoomHeight);
            core.ZoomFactor = EditorGUI.Slider(zoomRect, "Zoom Factor", core.ZoomFactor, 0.5f, 3f);

            // ───────────────────────────────────────
            // Calculate Scaled Cell and Toggle Sizes
            // ───────────────────────────────────────
            int gridSize = core.GridController.GridSize;
            float scaledCellSize = core.GridController.CellSize * core.ZoomFactor;
            float scaledToggleSize = core.GridController.ToggleSize * core.ZoomFactor;

            // Determine total content size including toggles on all sides.
            float contentWidth = gridSize * scaledCellSize + 2 * scaledToggleSize;
            float contentHeight = gridSize * scaledCellSize + 2 * scaledToggleSize;

            // ───────────────────────────────────────
            // Define Viewport (scrollable area)
            // ───────────────────────────────────────
            float vScrollWidth = 20f; // Reserve space for the vertical scrollbar.
            Rect viewportRect = new Rect(0, zoomHeight, position.width - vScrollWidth, position.height - zoomHeight);

            // Clamp vertical pan within the scrollable range.
            float maxVerticalPan = Mathf.Max(0, contentHeight - viewportRect.height);
            core.VerticalPan = Mathf.Clamp(core.VerticalPan, 0, maxVerticalPan);

            // ───────────────────────────────────────
            // Begin Scrollable Group
            // ───────────────────────────────────────
            GUI.BeginGroup(viewportRect);
            {
                // Offset content vertically to implement panning.
                Rect contentRect = new Rect(0, -core.VerticalPan, contentWidth, contentHeight);
                GUILayout.BeginArea(contentRect);
                {
                    EditorGUILayout.BeginVertical();
                    {
                        // Draw top edge toggles
                        DrawTopEdgeToggles(core, gridSize, scaledToggleSize, scaledCellSize);

                        // Draw core grid content (left toggles, cells, right toggles)
                        EditorGUILayout.BeginHorizontal();
                        {
                            DrawLeftEdgeToggles(core, gridSize, scaledToggleSize, scaledCellSize);
                            DrawGridCells(core, gridSize, scaledCellSize);
                            DrawRightEdgeToggles(core, gridSize, scaledToggleSize, scaledCellSize);
                        }
                        EditorGUILayout.EndHorizontal();

                        // Draw bottom edge toggles
                        DrawBottomEdgeToggles(core, gridSize, scaledToggleSize, scaledCellSize);
                    }
                    EditorGUILayout.EndVertical();
                }
                GUILayout.EndArea();
            }
            GUI.EndGroup();

            // ───────────────────────────────────────
            // Vertical Scrollbar (right of viewport)
            // ───────────────────────────────────────
            Rect vScrollbarRect = new Rect(position.width - vScrollWidth, zoomHeight, vScrollWidth, viewportRect.height);
            float newVerticalPan = GUI.VerticalScrollbar(
                vScrollbarRect,
                core.VerticalPan,             // current scroll
                viewportRect.height,          // thumb size
                0,                            // min scroll
                contentHeight                 // max scroll
            );
            core.VerticalPan = newVerticalPan;
        }

        // ───────────────────────────────────────
        // Repaint Optimization: Only repaint when mouse moves inside grid area
        // ───────────────────────────────────────
        if (Event.current.type == EventType.MouseMove)
        {
            if (position.Contains(Event.current.mousePosition))
            {
                core.Repaint();
            }
        }

        GUILayout.EndArea(); // End entire grid draw area
    }

    #region Toggle Drawing

    /// <summary>
    /// Draws the toggles along the top edge of the grid.
    /// A horizontal row is created with an initial spacer equal to toggle size (to align with left toggles),
    /// then one toggle per column is drawn, and finally a trailing spacer (to match right toggles).
    /// Each toggle corresponds to a cell along the top row of the grid.
    /// </summary>
    /// <param name="core">The main editor instance holding grid state.</param>
    /// <param name="gridSize">Number of cells per row (grid width).</param>
    /// <param name="toggleSize">Size of the toggles along the grid edges.</param>
    /// <param name="cellSize">The cell size, used to ensure proper spacing/scale.</param>
    private static void DrawTopEdgeToggles(TileFoundryCore_V3 core, int gridSize, float toggleSize, float cellSize)
    {
        EditorGUILayout.BeginHorizontal();
        // Add a spacer at the beginning to align with left-side toggles.
        GUILayout.Space(toggleSize);
        // Loop through each cell in the row and create a toggle control.
        for (int x = 0; x < gridSize; x++)
        {
            // Create a rectangle for the toggle; its width is scaled to cellSize and height to toggleSize.
            Rect toggleRect = GUILayoutUtility.GetRect(cellSize, toggleSize);
            // Set the current state of the toggle from the TopEdgeToggles array,
            // then update the value based on user interaction.
            core.GridController.TopEdgeToggles[x] = GUI.Toggle(toggleRect, core.GridController.TopEdgeToggles[x], GUIContent.none);
        }
        // Add an ending spacer for alignment with the right-side toggles.
        GUILayout.Space(toggleSize);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the toggles along the bottom edge of the grid.
    /// Similar to the top toggles, this creates a horizontal row with spacers on each end,
    /// and each toggle corresponds to a cell along the bottom row.
    /// </summary>
    /// <param name="core">The main editor instance holding grid state.</param>
    /// <param name="gridSize">Number of cells per row.</param>
    /// <param name="toggleSize">Size of the toggles.</param>
    /// <param name="cellSize">The cell size for spacing consistency.</param>
    private static void DrawBottomEdgeToggles(TileFoundryCore_V3 core, int gridSize, float toggleSize, float cellSize)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(toggleSize);
        for (int x = 0; x < gridSize; x++)
        {
            Rect toggleRect = GUILayoutUtility.GetRect(cellSize, toggleSize);
            // Update the bottom edge toggles based on user input.
            core.GridController.BottomEdgeToggles[x] = GUI.Toggle(toggleRect, core.GridController.BottomEdgeToggles[x], GUIContent.none);
        }
        GUILayout.Space(toggleSize);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the toggles along the left edge of the grid.
    /// Creates a vertical column of toggle controls, one for each cell along the left edge.
    /// The toggles are drawn from bottom to top for proper alignment with the grid.
    /// </summary>
    /// <param name="core">The main editor instance holding grid state.</param>
    /// <param name="gridSize">Number of cells in each column (grid height).</param>
    /// <param name="toggleSize">Size of the toggles.</param>
    /// <param name="cellSize">The cell size for proper proportioning.</param>
    private static void DrawLeftEdgeToggles(TileFoundryCore_V3 core, int gridSize, float toggleSize, float cellSize)
    {
        EditorGUILayout.BeginVertical();
        // Iterate from the bottom row to the top row.
        for (int y = gridSize - 1; y >= 0; y--)
        {
            Rect toggleRect = GUILayoutUtility.GetRect(toggleSize, cellSize);
            // Update the left edge toggles using the stored state.
            core.GridController.LeftEdgeToggles[y] = GUI.Toggle(toggleRect, core.GridController.LeftEdgeToggles[y], GUIContent.none);
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draws the toggles along the right edge of the grid.
    /// Like the left toggles, this creates a vertical column of toggle controls drawn from bottom to top.
    /// Each toggle corresponds to a cell along the right edge of the grid.
    /// </summary>
    /// <param name="core">The main editor instance holding grid state.</param>
    /// <param name="gridSize">Number of cells in the column (grid height).</param>
    /// <param name="toggleSize">Size of the toggles.</param>
    /// <param name="cellSize">The cell size for spacing consistency.</param>
    private static void DrawRightEdgeToggles(TileFoundryCore_V3 core, int gridSize, float toggleSize, float cellSize)
    {
        EditorGUILayout.BeginVertical();
        // Iterate over rows in reverse for proper alignment (bottom to top).
        for (int y = gridSize - 1; y >= 0; y--)
        {
            Rect toggleRect = GUILayoutUtility.GetRect(toggleSize, cellSize);
            // Update the right edge toggles based on the current state.
            core.GridController.RightEdgeToggles[y] = GUI.Toggle(toggleRect, core.GridController.RightEdgeToggles[y], GUIContent.none);
        }
        EditorGUILayout.EndVertical();
    }

    #endregion


    #region Grid Rendering

    /// <summary>
    /// Draws all the cells in the grid, handles user interactions for painting,
    /// wetting the brush, and shape tool previews (line, square, circle, hexagon).
    /// This method also renders existing tile previews from the various layers and applies visual effects like hover highlights.
    /// </summary>
    /// <param name="core">The core editor window providing state and grid data.</param>
    /// <param name="gridSize">The number of cells along each grid dimension.</param>
    /// <param name="cellSize">The current size of each grid cell.</param>
    private static void DrawGridCells(TileFoundryCore_V3 core, int gridSize, float cellSize)
    {
        // Get the current GUI event (mouse, key, etc.)
        Event e = Event.current;
        // Begin a vertical layout for all grid rows.
        EditorGUILayout.BeginVertical();

        // Determine the alpha (opacity) level for each layer depending on if it is the active layer.
        float alphaGround = (core.SelectedLayer == 0) ? 1f : core.InactiveLayerAlpha;
        float alphaWalls = (core.SelectedLayer == 1) ? 1f : core.InactiveLayerAlpha;
        float alphaFurniture = (core.SelectedLayer == 2) ? 1f : core.InactiveLayerAlpha;
        float alphaItem = (core.SelectedLayer == 3) ? 1f : core.InactiveLayerAlpha;
        float alphaOverlay = (core.SelectedLayer == 4) ? 1f : core.InactiveLayerAlpha;
        float alphaNode = (core.SelectedLayer == 5) ? 1f : core.InactiveLayerAlpha;

        // Loop over the grid rows in reverse order so that the top row is rendered at the top.
        for (int y = gridSize - 1; y >= 0; y--)
        {
            // Begin a horizontal layout for each row.
            EditorGUILayout.BeginHorizontal();

            // Loop through each cell in the current row.
            for (int x = 0; x < gridSize; x++)
            {
                // Get a rect for the current cell.
                Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize);
                Vector2Int coord = new Vector2Int(x, y);

                // Check if the cell is under the mouse pointer to handle interactivity.
                if (cellRect.Contains(e.mousePosition))
                {
                    hoverCoord = coord; // Store the current hovered cell.

                    // RIGHT-CLICK: "Wet the brush" – capture the asset at this cell and update the current brush.
                    if (e.type == EventType.MouseDown && e.button == 1)
                    {
                        string tileAtCell = null;
                        // Determine which layer is active and retrieve the corresponding tile asset name.
                        switch ((GridLayer)core.SelectedLayer)
                        {
                            case GridLayer.Ground:
                                tileAtCell = core.GridController.GroundGrid[x, y];
                                break;
                            case GridLayer.Walls:
                                tileAtCell = core.GridController.WallsGrid[x, y];
                                break;
                            case GridLayer.Furniture:
                                tileAtCell = core.GridController.FurnitureGrid[x, y];
                                break;
                            case GridLayer.Item:
                                tileAtCell = core.GridController.ItemGrid[x, y];
                                break;
                            case GridLayer.Overlay:
                                tileAtCell = core.GridController.OverlayGrid[x, y];
                                break;
                            case GridLayer.Node:
                                tileAtCell = core.GridController.NodeGrid[x, y];
                                break;
                        }
                        // If a tile is found, update the current brush selection.
                        if (!string.IsNullOrEmpty(tileAtCell))
                        {
                            core.CurrentTileName = tileAtCell;
                            // Look up the asset from the global asset lookup.
                            if (core.TileAssetLookup.TryGetValue(tileAtCell, out Object asset))
                            {
                                if (asset is TileBase tile)
                                {
                                    core.SelectedTileAsset = tile;
                                }
                                else if (asset is Sprite sprite)
                                {
                                    core.SelectedTileAsset = sprite;
                                }
                                else if (asset is GameObject go)
                                {
                                    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                                    core.SelectedTileAsset = (sr != null && sr.sprite != null) ? sr.sprite : null;
                                }
                                else
                                {
                                    core.SelectedTileAsset = null;
                                }
                            }
                            Debug.Log($"Wetted brush with tile '{tileAtCell}' from cell ({x},{y}) on layer {core.SelectedLayer}.");
                        }
                        else
                        {
                            Debug.Log($"No tile found at cell ({x},{y}) on layer {core.SelectedLayer} to wet the brush.");
                        }
                        e.Use(); // Mark event as used to prevent further propagation.
                    }
                    // LEFT-CLICK: Handle painting and shape tool operations.
                    else if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        // Choose the operation based on the selected tool.
                        switch (core.SelectedTool)
                        {
                            case 0: // Brush tool: start painting and enable dragging.
                                isDragging = true;
                                PaintCell(core, x, y, coord);
                                break;
                            case 1: // Flood Fill tool: fill contiguous region.
                                FloodFillTool(core, x, y, coord);
                                break;
                            case 2: // Line tool: set start point then draw a line.
                                if (!shapeCenter.HasValue)
                                    shapeCenter = coord;
                                else
                                {
                                    List<Vector2Int> linePoints = core.GridController.BresenhamLine(shapeCenter.Value, coord);
                                    foreach (Vector2Int pt in linePoints)
                                        PaintCell(core, pt.x, pt.y, pt);
                                    shapeCenter = null;
                                }
                                break;
                            case 3: // Square tool: select two corners to fill a square.
                                if (!shapeCenter.HasValue)
                                    shapeCenter = coord;
                                else
                                {
                                    int r = Mathf.Max(Mathf.Abs(hoverCoord.Value.x - shapeCenter.Value.x),
                                                      Mathf.Abs(hoverCoord.Value.y - shapeCenter.Value.y));
                                    for (int i = 0; i < gridSize; i++)
                                    {
                                        for (int j = 0; j < gridSize; j++)
                                        {
                                            if (Mathf.Abs(i - shapeCenter.Value.x) <= r && Mathf.Abs(j - shapeCenter.Value.y) <= r)
                                                PaintCell(core, i, j, new Vector2Int(i, j));
                                        }
                                    }
                                    shapeCenter = null;
                                }
                                break;
                            case 4: // Circle tool: fill cells within a certain radius.
                                if (!shapeCenter.HasValue)
                                    shapeCenter = coord;
                                else
                                {
                                    float r = Vector2.Distance(shapeCenter.Value, hoverCoord.Value);
                                    for (int i = 0; i < gridSize; i++)
                                    {
                                        for (int j = 0; j < gridSize; j++)
                                        {
                                            if (Vector2.Distance(new Vector2(i, j), shapeCenter.Value) <= r)
                                                PaintCell(core, i, j, new Vector2Int(i, j));
                                        }
                                    }
                                    shapeCenter = null;
                                }
                                break;
                            case 5: // Hexagon tool: use hexagon geometry to fill cells.
                                if (!shapeCenter.HasValue)
                                    shapeCenter = coord;
                                else
                                {
                                    float hexRadius = Vector2.Distance(shapeCenter.Value, hoverCoord.Value);
                                    Vector2[] hexVertices = GetHexagonVertices(shapeCenter.Value, hexRadius);
                                    for (int i = 0; i < gridSize; i++)
                                    {
                                        for (int j = 0; j < gridSize; j++)
                                        {
                                            if (IsPointInPolygon(new Vector2(i, j), hexVertices))
                                                PaintCell(core, i, j, new Vector2Int(i, j));
                                        }
                                    }
                                    shapeCenter = null;
                                }
                                break;
                        }
                        e.Use(); // Consume the event.
                    }
                    // Continues painting on drag if using the Brush tool.
                    else if (e.type == EventType.MouseDrag && isDragging && core.SelectedTool == 0)
                    {
                        PaintCell(core, x, y, coord);
                        e.Use();
                    }
                    // Reset dragging state on mouse up.
                    else if (e.type == EventType.MouseUp)
                    {
                        isDragging = false;
                        lastPaintCoord = null;
                    }
                }

                // Draw overlays for shape tool previews based on tool type:
                if (core.SelectedTool == 2 && shapeCenter.HasValue && hoverCoord.HasValue)
                {
                    // Line tool preview: highlight cells on the line.
                    if (core.GridController.IsPointOnLine(coord, shapeCenter.Value, hoverCoord.Value))
                        EditorGUI.DrawRect(cellRect, new Color(1f, 1f, 0f, 0.25f));
                }
                if (core.SelectedTool == 3 && shapeCenter.HasValue && hoverCoord.HasValue)
                {
                    // Square tool preview: highlight cells within the square.
                    int r = Mathf.Max(Mathf.Abs(hoverCoord.Value.x - shapeCenter.Value.x),
                                      Mathf.Abs(hoverCoord.Value.y - shapeCenter.Value.y));
                    if (Mathf.Abs(coord.x - shapeCenter.Value.x) <= r && Mathf.Abs(coord.y - shapeCenter.Value.y) <= r)
                        EditorGUI.DrawRect(cellRect, new Color(0f, 1f, 1f, 0.25f));
                }
                if (core.SelectedTool == 4 && shapeCenter.HasValue && hoverCoord.HasValue)
                {
                    // Circle tool preview: highlight cells inside the circle.
                    float r = Vector2.Distance(shapeCenter.Value, hoverCoord.Value);
                    if (Vector2.Distance(coord, shapeCenter.Value) <= r)
                        EditorGUI.DrawRect(cellRect, new Color(1f, 0f, 1f, 0.25f));
                }
                if (core.SelectedTool == 5 && shapeCenter.HasValue && hoverCoord.HasValue)
                {
                    // Hexagon tool preview: highlight cells within the hexagon.
                    float hexRadius = Vector2.Distance(shapeCenter.Value, hoverCoord.Value);
                    Vector2[] hexVertices = GetHexagonVertices(shapeCenter.Value, hexRadius);
                    if (IsPointInPolygon(new Vector2(coord.x, coord.y), hexVertices))
                        EditorGUI.DrawRect(cellRect, new Color(0.5f, 0f, 0.5f, 0.25f));
                }

                // Draw the existing tiles for each layer onto the cell.
                DrawLayerTile(core, cellRect, core.GridController.GroundGrid[x, y], alphaGround, core);
                DrawLayerTile(core, cellRect, core.GridController.WallsGrid[x, y], alphaWalls, core);
                DrawLayerTile(core, cellRect, core.GridController.FurnitureGrid[x, y], alphaFurniture, core);
                DrawLayerTile(core, cellRect, core.GridController.ItemGrid[x, y], alphaItem, core);
                DrawLayerTile(core, cellRect, core.GridController.OverlayGrid[x, y], alphaOverlay, core);
                DrawLayerTile(core, cellRect, core.GridController.NodeGrid[x, y], alphaNode, core);

                // Draw hover overlay on the cell currently under the mouse.
                if (hoverCoord.HasValue && hoverCoord.Value == coord)
                    EditorGUI.DrawRect(cellRect, new Color(0f, 1f, 0f, 0.25f));

                // Draw a red highlight on the selected shape's center cell.
                if (shapeCenter.HasValue && shapeCenter.Value == coord)
                    EditorGUI.DrawRect(cellRect, new Color(1f, 0f, 0f, 0.5f));

                // During repaint events, draw a border around the cell.
                if (e.type == EventType.Repaint)
                {
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.xMax, cellRect.y));
                    Handles.DrawLine(new Vector3(cellRect.xMax, cellRect.y), new Vector3(cellRect.xMax, cellRect.yMax));
                    Handles.DrawLine(new Vector3(cellRect.xMax, cellRect.yMax), new Vector3(cellRect.x, cellRect.yMax));
                    Handles.DrawLine(new Vector3(cellRect.x, cellRect.yMax), new Vector3(cellRect.x, cellRect.y));
                }
            }
            // End the current row.
            EditorGUILayout.EndHorizontal();
        }
        // End the vertical block for grid cells.
        EditorGUILayout.EndVertical();
    }

    private static void PaintCell(TileFoundryCore_V3 core, int x, int y, Vector2Int coord)
    {
        // Avoid repainting the same cell repeatedly during drag.
        if (lastPaintCoord.HasValue && lastPaintCoord.Value == coord)
            return;

        lastPaintCoord = coord;

        string currentAsset = core.CurrentTileName;
        // Treat "None" or empty as clearing the tile.
        if (string.IsNullOrEmpty(currentAsset) || currentAsset.Equals("none", System.StringComparison.OrdinalIgnoreCase))
            currentAsset = null;

        // Set the tile value for the currently selected layer.
        switch ((GridLayer)core.SelectedLayer)
        {
            case GridLayer.Ground:
                core.GridController.GroundGrid[x, y] = currentAsset;
                break;
            case GridLayer.Walls:
                core.GridController.WallsGrid[x, y] = currentAsset;
                break;
            case GridLayer.Furniture:
                core.GridController.FurnitureGrid[x, y] = currentAsset;
                break;
            case GridLayer.Item:
                core.GridController.ItemGrid[x, y] = currentAsset;
                break;
            case GridLayer.Overlay:
                core.GridController.OverlayGrid[x, y] = currentAsset;
                break;
            case GridLayer.Node:
                core.GridController.NodeGrid[x, y] = currentAsset;
                break;
        }

        core.Repaint();
    }

    /// <summary>
    /// Performs a flood fill operation starting at (x, y),
    /// replacing all contiguous cells with the same tile as the start cell
    /// with the currently selected tile.
    /// </summary>
    private static void FloodFillTool(TileFoundryCore_V3 core, int x, int y, Vector2Int coord)
    {
        string newAsset = core.CurrentTileName;
        string[,] grid = null;

        // Select the target grid based on the currently active layer.
        switch (core.SelectedLayer)
        {
            case (int)GridLayer.Ground: grid = core.GridController.GroundGrid; break;
            case (int)GridLayer.Walls: grid = core.GridController.WallsGrid; break;
            case (int)GridLayer.Furniture: grid = core.GridController.FurnitureGrid; break;
            case (int)GridLayer.Item: grid = core.GridController.ItemGrid; break;
            case (int)GridLayer.Overlay: grid = core.GridController.OverlayGrid; break;
            case (int)GridLayer.Node: grid = core.GridController.NodeGrid; break;
        }

        if (grid == null) return;

        string targetAsset = grid[x, y];
        if (targetAsset == newAsset) return;

        // Standard iterative flood fill using a queue.
        Queue<Vector2Int> nodes = new Queue<Vector2Int>();
        nodes.Enqueue(coord);

        while (nodes.Count > 0)
        {
            Vector2Int p = nodes.Dequeue();

            if (p.x < 0 || p.x >= core.GridController.GridWidth ||
                p.y < 0 || p.y >= core.GridController.GridHeight)
                continue;

            if (grid[p.x, p.y] != targetAsset)
                continue;

            grid[p.x, p.y] = newAsset;

            // Enqueue neighboring cells.
            nodes.Enqueue(new Vector2Int(p.x + 1, p.y));
            nodes.Enqueue(new Vector2Int(p.x - 1, p.y));
            nodes.Enqueue(new Vector2Int(p.x, p.y + 1));
            nodes.Enqueue(new Vector2Int(p.x, p.y - 1));
        }

        core.Repaint();
    }

    #endregion

    #region Preview & Tile Loading

    /// <summary>
    /// Draws the tile preview texture on a specific cell rectangle.
    /// Falls back to a generated preview if not already cached.
    /// </summary>
    private static void DrawLayerTile(TileFoundryCore_V3 core, Rect cellRect, string asset, float alpha, TileFoundryCore_V3 currentCore)
    {
        if (string.IsNullOrEmpty(asset))
            return;

        string key = asset;
        Texture2D tex = null;

        // Use preview cache if available.
        if (core.TilePreviewCache.TryGetValue(key, out tex))
        {
            // Already cached
        }
        // If not cached, attempt to find the asset and generate a preview.
        else if (currentCore.TileAssetLookup.TryGetValue(key, out Object obj))
        {
            if (obj is TileBase tile)
            {
                Sprite sprite = (tile as Tile)?.sprite;
                if (sprite != null)
                    tex = SpriteToTexture(sprite);
            }
            else if (obj is Sprite spr)
            {
                tex = SpriteToTexture(spr);
            }
            else if (obj is GameObject go)
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    tex = SpriteToTexture(sr.sprite);
            }

            if (tex != null)
            {
                tex.hideFlags = HideFlags.HideAndDontSave;
                core.TilePreviewCache[key] = tex;
            }
        }

        // Draw the preview texture with appropriate transparency.
        if (tex != null)
        {
            Color origColor = GUI.color;
            GUI.color = new Color(origColor.r, origColor.g, origColor.b, alpha);
            GUI.DrawTexture(cellRect, tex, ScaleMode.ScaleToFit);
            GUI.color = origColor;
        }
    }

    /// <summary>
    /// Converts a Sprite to a Texture2D for use in preview drawing.
    /// Uses the sprite’s rect to extract the correct subregion from its texture.
    /// </summary>
    private static Texture2D SpriteToTexture(Sprite sprite)
    {
        Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
        texture.SetPixels(sprite.texture.GetPixels(
            (int)sprite.rect.x, (int)sprite.rect.y,
            (int)sprite.rect.width, (int)sprite.rect.height));
        texture.Apply();
        return texture;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Computes the 6 corner vertices of a regular hexagon (pointy-topped)
    /// given a center position and a radius.
    /// Used for hexagon shape tool previews.
    /// </summary>
    private static Vector2[] GetHexagonVertices(Vector2 center, float radius)
    {
        Vector2[] vertices = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 90 - 60 * i;
            float angleRad = Mathf.Deg2Rad * angleDeg;
            vertices[i] = new Vector2(
                center.x + radius * Mathf.Cos(angleRad),
                center.y + radius * Mathf.Sin(angleRad));
        }
        return vertices;
    }

    /// <summary>
    /// Determines whether a point lies within a polygon using a ray-casting algorithm.
    /// Useful for checking if a cell should be painted when using the hexagon tool.
    /// </summary>
    private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int n = polygon.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    #endregion
}