using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script provides real-time visualization of the chunk under the mouse cursor 
/// in Unity's SceneView. It allows highlighting the hovered chunk, drawing contained hexes, 
/// and displaying labels based on user settings in GridVisualizer.
/// </summary>
[InitializeOnLoad]
public static class SceneMouseChunkVisualizer
{
    private static Vector2Int? hoveredChunkCoords = null; // Stores the hovered chunk coordinates

    /// <summary>
    /// Static constructor subscribes to SceneView GUI updates when Unity starts.
    /// </summary>
    static SceneMouseChunkVisualizer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    /// <summary>
    /// Handles the SceneView GUI event, updating and rendering hovered chunk information.
    /// </summary>
    private static void OnSceneGUI(SceneView sceneView)
    {
        // Check if mouse-over visualization is enabled
        if (!IsMouseOverChunkEnabled())
        {
            hoveredChunkCoords = null; // Clear visualization if disabled
            return;
        }

        Event e = Event.current;
        if (e.type == EventType.MouseMove)
        {
            UpdateHoveredChunk(sceneView);
            SceneView.RepaintAll(); // Force redraw to keep the visualization updated
        }

        // Draw hovered chunk visualization if a chunk is detected
        if (hoveredChunkCoords.HasValue)
        {
            DrawHoveredChunk(hoveredChunkCoords.Value);
        }
    }

    /// <summary>
    /// Updates the hovered chunk coordinates based on the mouse position in SceneView.
    /// </summary>
    private static void UpdateHoveredChunk(SceneView sceneView)
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition(sceneView);   

        hoveredChunkCoords = ChunkUtilities.WorldToChunk(mouseWorldPos, GridGuide.gridConfig);
    }

    /// <summary>
    /// Converts the mouse position in SceneView to a world position on the y=0 plane.
    /// </summary>
    private static Vector3 GetMouseWorldPosition(SceneView sceneView)
    {
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        // Compute intersection with the y=0 plane (ground level)
        if (ray.direction.y == 0) return Vector3.zero; // Avoid division by zero
        float t = -ray.origin.y / ray.direction.y;
        return ray.origin + t * ray.direction;
    }

    /// <summary>
    /// Draws the hovered chunk, optionally filling it and displaying contained hexes and labels.
    /// </summary>
    private static void DrawHoveredChunk(Vector2Int chunkCoords)
    {       

        // Convert chunk coordinates to world position
        Vector3 chunkWorldPos = ChunkUtilities.ChunkToWorld(chunkCoords.x, chunkCoords.y, GridGuide.gridConfig);

        // Draw the chunk hex wireframe
        Handles.color = Color.green;
        HexUtilities.DrawHexagonHandles(chunkWorldPos, GridGuide.gridConfig.ChunkSize, GridGuide.gridConfig.overlayGridOrientation, Color.green);

        // Check if we should fill the hovered chunk
        if (IsChunkFillEnabled())
        {
            HexUtilities.DrawFilledHexagonHandles(chunkWorldPos, GridGuide.gridConfig.ChunkSize, GridGuide.gridConfig.overlayGridOrientation, new Color(0, 1, 0, 0.2f));
        }

        // Check if we should draw hexes contained within the chunk
        if (IsChunkHexesEnabled())
        {
            DrawContainedHexes(chunkCoords, GridGuide.gridConfig);
        }

        // Check if chunk label is enabled
        if (IsChunkLabelEnabled())
        {
            DrawChunkLabel(chunkWorldPos, chunkCoords);
        }
    }

    /// <summary>
    /// Draws hexes within the hovered chunk, optionally filling them and labeling them.
    /// </summary>
    private static void DrawContainedHexes(Vector2Int chunkCoords, GridConfig config)
    {
        // Get all hex positions within the chunk
        List<Vector3> hexCenters = ChunkUtilities.GetChunkHexesInWorldSpace(chunkCoords.x, chunkCoords.y, config);

        foreach (var hexPos in hexCenters)
        {
            // Draw hex wireframes inside the chunk
            Handles.color = Color.blue;
            HexUtilities.DrawHexagonHandles(hexPos, config.hexSize, config.baseGridOrientation, Color.blue);

            // Optional: Fill hexes with a transparent color
            HexUtilities.DrawFilledHexagonHandles(hexPos, config.hexSize, config.baseGridOrientation, new Color(0, 0, 1, 0.2f));

            // Check if hex labeling is enabled
            if (IsChunkHexLabelsEnabled())
            {
                Vector2Int axialCoords = HexUtilities.WorldToAxial(hexPos, config.baseGridOrientation, config.hexSize);
                DrawHexLabel(hexPos, axialCoords.x, axialCoords.y);
            }
        }
    }

    /// <summary>
    /// Draws a label displaying the chunk coordinates.
    /// </summary>
    private static void DrawChunkLabel(Vector3 chunkWorldPos, Vector2Int chunkCoords)
    {
        Handles.Label(chunkWorldPos + Vector3.up * 0.5f, $"Chunk: ({chunkCoords.x}, {chunkCoords.y})",
            new GUIStyle { normal = { textColor = Color.green } });
    }

    /// <summary>
    /// Draws a label for a hex with its axial coordinates.
    /// </summary>
    public static void DrawHexLabel(Vector3 hexPos, int q, int r)
    {
        Handles.color = Color.white;
        Handles.Label(hexPos + Vector3.up * 0.5f, $"({q},{r})");
    }

    /// <summary>
    /// Checks if the mouse-over chunk visualization is enabled.
    /// </summary>
    private static bool IsMouseOverChunkEnabled()
    {
        GridVisualizer visualizer = Object.FindObjectOfType<GridVisualizer>();
        return visualizer != null && visualizer.showMouseOverChunk;
    }

    /// <summary>
    /// Checks if the hovered chunk should be filled with a semi-transparent color.
    /// </summary>
    private static bool IsChunkFillEnabled()
    {
        GridVisualizer visualizer = Object.FindObjectOfType<GridVisualizer>();
        return visualizer != null && visualizer.showMouseOverChunkFill;
    }

    /// <summary>
    /// Checks if the hexes within the hovered chunk should be drawn.
    /// </summary>
    private static bool IsChunkHexesEnabled()
    {
        GridVisualizer visualizer = Object.FindObjectOfType<GridVisualizer>();
        return visualizer != null && visualizer.showMouseOverChunkHexes;
    }

    /// <summary>
    /// Checks if the hovered chunk should display its coordinate label.
    /// </summary>
    private static bool IsChunkLabelEnabled()
    {
        GridVisualizer visualizer = Object.FindObjectOfType<GridVisualizer>();
        return visualizer != null && visualizer.showMouseOverChunkLabel;
    }

    /// <summary>
    /// Checks if the hexes inside the hovered chunk should display their coordinate labels.
    /// </summary>
    private static bool IsChunkHexLabelsEnabled()
    {
        GridVisualizer visualizer = Object.FindObjectOfType<GridVisualizer>();
        return visualizer != null && visualizer.showMouseOverChunkHexLabels;
    }
}
