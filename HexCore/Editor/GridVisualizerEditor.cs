using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GridSceneOverlay
{
    private static bool showUI = true; // Toggle for UI visibility
    private static GridVisualizer visualizer;
    private static GridGuide gridGuide;

    static GridSceneOverlay()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void TryFindComponents()
    {
        if (visualizer == null)
            visualizer = Object.FindObjectOfType<GridVisualizer>();

        if (gridGuide == null)
            gridGuide = Object.FindObjectOfType<GridGuide>();
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        TryFindComponents();

        // Check if the selected object is a child of GridGuide
        bool isSelectionValid = IsSelectionChildOfGridGuide();
        if (!isSelectionValid)
            return;

        Handles.BeginGUI();
        float panelWidth = 260;
        float panelHeight = showUI ? 390 : 55;
        float xPosition = 10;
        float yPosition = sceneView.position.height - panelHeight - 35;

        GUILayout.BeginArea(new Rect(xPosition, yPosition, panelWidth, panelHeight), "Grid Debug", GUI.skin.window);

        if (GUILayout.Button(showUI ? "Hide Grid Controls" : "Show Grid Controls", GUILayout.Height(30)))
        {
            showUI = !showUI;
            SceneView.RepaintAll();
        }

        if (showUI)  // Only show the UI if the selection is valid
        {
            GUILayout.Space(5);

            if (visualizer != null)
            {
                EditorGUILayout.LabelField("Grid Layers", EditorStyles.boldLabel);
                visualizer.showHexSpaceGrid = GUILayout.Toggle(visualizer.showHexSpaceGrid, "Show Hex Space Grid");
                visualizer.showChunkSpaceGrid = GUILayout.Toggle(visualizer.showChunkSpaceGrid, "Show Chunk Grid");
                visualizer.showMouseOverChunk = GUILayout.Toggle(visualizer.showMouseOverChunk, "Show Mouse Over Chunk");
                // If "Show Mouse Over Chunk" is turned off, disable "Show Mouse Over Chunk with Hexes"
                if (!visualizer.showMouseOverChunk)
                {
                    visualizer.showMouseOverChunkHexes = false;
                }

                visualizer.showMouseOverChunkHexes = GUILayout.Toggle(visualizer.showMouseOverChunkHexes, "Show Mouse Over Chunk with Hexes");

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Bubble View", EditorStyles.boldLabel);
                visualizer.showRealityBubbleHex = GUILayout.Toggle(visualizer.showRealityBubbleHex, "Reality Bubbles (Hex)");
                visualizer.showRealityBubbleChunk = GUILayout.Toggle(visualizer.showRealityBubbleChunk, "Reality Bubbles (Chunk)");

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Labels & Highlights", EditorStyles.boldLabel);
                visualizer.showHexLabels = GUILayout.Toggle(visualizer.showHexLabels, "Show Hex Labels");
                visualizer.showChunkLabels = GUILayout.Toggle(visualizer.showChunkLabels, "Show Chunk Labels");                
                visualizer.showMouseOverChunkHexLabels = GUILayout.Toggle(visualizer.showMouseOverChunkHexLabels, "Show Hovered Chunk Contained Hex Labels");

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
                visualizer.hexDrawRange = EditorGUILayout.IntSlider("Hex Draw Range", visualizer.hexDrawRange, 10, 100);
            }

            if (gridGuide != null)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Grid Config", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Hex Size:", GridGuide.gridConfig.hexSize.ToString("F2"));
                EditorGUILayout.LabelField("Chunk Size:", GridGuide.gridConfig.ChunkSize.ToString("F2"));
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Refresh Grid", GUILayout.Height(30)))
            {
                SceneView.RepaintAll();
                Debug.Log("[GridSceneOverlay] Grid refreshed.");
            }
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    /// <summary>
    /// Checks if the currently selected object is a child of the object holding GridGuide.
    /// </summary>
    private static bool IsSelectionChildOfGridGuide()
    {
        if (Selection.activeGameObject == null || gridGuide == null)
            return false;

        Transform selectedTransform = Selection.activeGameObject.transform;
        Transform guideTransform = gridGuide.transform;

        while (selectedTransform != null)
        {
            if (selectedTransform == guideTransform)
                return true;
            selectedTransform = selectedTransform.parent;
        }

        return false;
    }

}
