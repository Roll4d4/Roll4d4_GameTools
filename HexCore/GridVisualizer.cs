using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    // Settings for drawing grids
    public int hexDrawRange = 50;
    public bool testSingleChunk = false;
    public Vector2Int testChunkCoords = new Vector2Int(0, 0);

    // NEW: Toggle for highlighting chunk under mouse
    public bool highlightMouseOverChunk = false;
    private Vector2Int? hoveredChunkCoords = null; // Stores last hovered chunk
    [Header("Chunk Visualization")]
    public bool showMouseOverChunk = true; // Toggle for chunk wireframe
    public bool showMouseOverChunkFill = false; // Toggle for filled chunk hex
    public bool showMouseOverChunkHexes = false; // Toggle for drawing contained hexes

    // Grid components
    [SerializeField] private GridGuide gridGuide;
    [SerializeField] private RealityBubbleSystem realityBubbleSystem;
    public bool showMouseOverChunkLabel = true;
    public bool showMouseOverChunkHexLabels = false;

    // Toggle grid visualizations
    public bool showHexSpaceGrid = true;
    public bool showChunkSpaceGrid = true;
    public bool showRealityBubbleChunk = true;
    public bool showRealityBubbleHex = true;
    public bool showChunkContainedHexes = false;
    public bool showOuterEdges = true;

    // Gizmo colors for different visual elements
    public Color chunkBorderColor = Color.red;
    public Color hexBorderColor = Color.green;
    public Color realityBubbleChunkColor = Color.blue;
    public Color realityBubbleHexColor = Color.cyan;
    public Color outerEdgeColor = Color.yellow;
    public Color mouseOverChunkColor = Color.magenta; // NEW: Highlight color for hovered chunk

    [Header("Labeling")]
    public bool showHexLabels = false;
    public bool showChunkLabels = false;
    public bool showChunkContainedHexesLabels = false;
    [Header("Highlighting")]
    public bool highlightSharedBorderHexes = false;
    public Color sharedHexColor = Color.red;

    private void OnEnable()
    {
        SceneView.duringSceneGui += DetectMouseOverChunk;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DetectMouseOverChunk;
    }

    private void OnDrawGizmos()
    {
        if (gridGuide == null) return;
        

        if (showHexSpaceGrid)
        {
            Gizmos.color = hexBorderColor;
            DrawHexSpaceGrid();
        }

        if (showChunkSpaceGrid)
        {
            Gizmos.color = chunkBorderColor;
            DrawChunkSpaceGrid(GridGuide.gridConfig.ChunkSize);
        }


        if (showRealityBubbleChunk && realityBubbleSystem != null)
        {
            Gizmos.color = realityBubbleChunkColor;
            HighlightRealityBubbleChunk(GridGuide.gridConfig.ChunkSize);
        }

        if (showRealityBubbleHex && realityBubbleSystem != null)
        {
            Gizmos.color = realityBubbleChunkColor;
            HighlightRealityBubbleHexes(GridGuide.gridConfig.hexSize);
        }

        if (showOuterEdges && testSingleChunk)
        {
            Gizmos.color = outerEdgeColor;
            DrawDrunkWalkPerimeter(GridGuide.gridConfig.hexSize, GridGuide.gridConfig.baseGridOrientation);
        }

        if (highlightMouseOverChunk && hoveredChunkCoords.HasValue)
        {
            Gizmos.color = mouseOverChunkColor;
          
            DrawChunkHighlight(hoveredChunkCoords.Value);
        }
    }

    /// <summary>
    /// Draws a highlighted hexagon for the hovered chunk.
    /// </summary>
    private void DrawChunkHighlight(Vector2Int chunkCoords)
    {
        Vector3 chunkPos = ChunkUtilities.ChunkToWorld(chunkCoords.x, chunkCoords.y, GridGuide.gridConfig);
        HexUtilities.DrawHexagon(chunkPos, GridGuide.gridConfig.ChunkSize, GridGuide.gridConfig.overlayGridOrientation);
    }

    /// <summary>
    /// Detects the chunk under the mouse cursor in world space.
    /// </summary>
    private void DetectMouseOverChunk(SceneView sceneView)
    {
        Event e = Event.current;
        
        if (e.type != EventType.MouseMove)
            return; // Only process mouse move events

        // Get mouse position in GUI space and convert it to world space
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        // Find intersection with the Y=0 plane
        if (ray.direction.y == 0) return; // Avoid division by zero

        float t = -ray.origin.y / ray.direction.y; // Distance to Y=0 plane
        Vector3 worldPoint = ray.origin + t * ray.direction; // Intersection point

        // Convert world position to chunk coordinates
        Vector2Int chunkCoords = ChunkUtilities.WorldToChunk(worldPoint, GridGuide.gridConfig);
        hoveredChunkCoords = chunkCoords;
        Debug.Log(worldPoint);
        SceneView.RepaintAll(); // Force SceneView to refresh the gizmos
    }

    private void DrawHexSpaceGrid()
    {
        float hexSize = GridGuide.gridConfig.hexSize;
        HexOrientation orientation = GridGuide.gridConfig.baseGridOrientation;

        for (int q = -hexDrawRange; q <= hexDrawRange; q++)
        {
            int rMin = Mathf.Max(-hexDrawRange, -q - hexDrawRange);
            int rMax = Mathf.Min(hexDrawRange, -q + hexDrawRange);

            for (int r = rMin; r <= rMax; r++)
            {
                Vector3 hexPos = HexUtilities.AxialToWorld(q, r, orientation, hexSize);

                // Draw the hex outline
                HexUtilities.DrawHexagon(hexPos, hexSize, orientation);

#if UNITY_EDITOR
                // If the user wants to see hex labels, draw them
                if (showHexLabels)
                {
                    HexUtilities.DrawHexLabel(hexPos, q, r);
                }
#endif
            }
        }
    }

    private void DrawChunkSpaceGrid(float chunkSize)
    {
        for (int chunkQ = -10; chunkQ <= 10; chunkQ++)
        {
            int chunkRMin = Mathf.Max(-10, -chunkQ - 10);
            int chunkRMax = Mathf.Min(10, -chunkQ + 10);

            for (int chunkR = chunkRMin; chunkR <= chunkRMax; chunkR++)
            {
                // 1) Draw the chunk outline
                Vector3 chunkPos = HexUtilities.AxialToWorld(
                    chunkQ,
                    chunkR,
                    GridGuide.gridConfig.overlayGridOrientation,
                    chunkSize
                );
                HexUtilities.DrawHexagon(chunkPos, chunkSize, GridGuide.gridConfig.overlayGridOrientation);

#if UNITY_EDITOR
                // 2) Optionally label the chunk
                if (showChunkLabels)
                {
                    HexUtilities.DrawHexLabel(chunkPos, chunkQ, chunkR);
                }
#endif

                // 3) Conditionally draw contained hexes
                //    - If showChunkContainedHexes is off, skip
                //    - If testSingleChunk is on, only draw if (chunkQ, chunkR) == testChunkCoords
                if (showChunkContainedHexes)
                {
                    // Only draw the chunk's contained hexes if either:
                    //   - testSingleChunk is false (so we want all)
                    //   - or it's the test chunk
                    bool isTestChunk = (chunkQ == testChunkCoords.x && chunkR == testChunkCoords.y);

                    if (!testSingleChunk || isTestChunk)
                    {
                        DrawSingleChunk(chunkQ, chunkR, chunkSize);
                    }
                }
            }
        }
    }
   
    private void DrawSingleChunk(int chunkQ, int chunkR, float chunkSize)
    {
        // 1) Draw the chunk outline
        Vector3 chunkPos = HexUtilities.AxialToWorld(
            chunkQ,
            chunkR,
            GridGuide.gridConfig.overlayGridOrientation,
            chunkSize
        );
        HexUtilities.DrawHexagon(chunkPos, chunkSize, GridGuide.gridConfig.overlayGridOrientation);

#if UNITY_EDITOR
        // 2) Label the chunk if toggled on
        if (showChunkLabels)
        {
            HexUtilities.DrawHexLabel(chunkPos, chunkQ, chunkR);
        }
#endif

        // 3) Draw all the small hexes inside the chunk (if toggled on)
        if (showChunkContainedHexes)
        {
            DrawContainedHexesLocal(chunkQ, chunkR, chunkSize);
        }

        // 4) If showOuterEdges is true, highlight chunk borders 
        //    (i.e., edges with no neighboring chunk in your -10..10 space).
        if (showOuterEdges)
        {
            HighlightChunkBorderEdges(chunkQ, chunkR, chunkPos, chunkSize);
        }
    }

    private void HighlightChunkBorderEdges(int chunkQ, int chunkR, Vector3 chunkPos, float chunkSize)
    {
        // 1) Get the chunk's 6 corner vertices
        Vector3[] chunkVerts = HexUtilities.GetHexVertices(chunkPos, chunkSize, GridGuide.gridConfig.overlayGridOrientation);

        // 2) Get neighbor offsets for the chunk's orientation
        Vector2Int[] offsets = HexUtilities.GetNeighborOffsets(GridGuide.gridConfig.overlayGridOrientation);

        // 3) For each edge, check if there's a neighbor chunk in the same range
        for (int i = 0; i < 6; i++)
        {
            int nQ = chunkQ + offsets[i].x;
            int nR = chunkR + offsets[i].y;

            // If the neighbor chunk is out of range => highlight this edge
            if (!ChunkUtilities.IsChunkInRange(nQ, nR))
            {
                Vector3 start = chunkVerts[i];
                Vector3 end = chunkVerts[(i + 1) % 6];

                Gizmos.color = outerEdgeColor;
                Gizmos.DrawLine(start, end);
            }
        }
    }
  
    private void DrawContainedHexesLocal(int chunkQ, int chunkR, float chunkSize)
    {
        // 1) Get the chunk’s root world position
        //    (the center of this chunk in "overlay" orientation)
        Vector3 chunkRootPos = HexUtilities.AxialToWorld(
            chunkQ,
            chunkR,
            GridGuide.gridConfig.overlayGridOrientation,
            chunkSize
        );

        // 2) Generate local offsets for all hexes in a chunk of radius chunkRadius
        //    This is "relative axial" coords from -r..r
        List<Vector2Int> containedHexes = ChunkUtilities.GetChunkHexes(GridGuide.gridConfig.chunkRadius);
        // e.g. from -5..5 if chunkRadius = 5

        // 3) Convert each local offset to world space (in your "base" orientation)
        //    Then add chunkRootPos to position them inside the chunk.
        foreach (Vector2Int offset in containedHexes)
        {
            // local offset -> local position in world space
            Vector3 localPos = HexUtilities.AxialToWorld(
                offset.x,
                offset.y,
                GridGuide.gridConfig.baseGridOrientation,
                GridGuide.gridConfig.hexSize
            );

            Vector3 hexPos = chunkRootPos + localPos;

            // 4) Optional: Check if offset is on the "border" of this chunk
            //    (meaning offset is exactly radius away from (0,0))
            if (highlightSharedBorderHexes && HexUtilities.IsBorderHex(offset.x, offset.y, GridGuide.gridConfig.chunkRadius))
            {
                Gizmos.color = sharedHexColor;
            }
            else
            {
                Gizmos.color = hexBorderColor;
            }

            // 5) Draw the hex
            HexUtilities.DrawHexagon(hexPos, GridGuide.gridConfig.hexSize, GridGuide.gridConfig.baseGridOrientation);

#if UNITY_EDITOR
            // 6) Label if desired
            if (showHexLabels || showChunkContainedHexesLabels)
            {              
                HexUtilities.DrawHexLabel(hexPos, offset.x, offset.y);
            }
#endif
        }
    }

    private void HighlightRealityBubbleChunk(float chunkSize)
    {
        foreach (var bubble in realityBubbleSystem.bubbles)
        {
            if (bubble == null) continue;

            Vector3 bubblePosition = bubble.Position;
            Vector2Int chunkAxial = HexUtilities.WorldToAxial(bubblePosition, GridGuide.gridConfig.overlayGridOrientation, chunkSize);
            Debug.Log($"Bubble Position: {bubblePosition} converted to Chunk Axial Coordinates: ({chunkAxial.x}, {chunkAxial.y})");

            Vector3 chunkPos = HexUtilities.AxialToWorld(chunkAxial.x, chunkAxial.y, GridGuide.gridConfig.overlayGridOrientation, chunkSize);
            Debug.Log($"Highlighting Chunk at Axial: ({chunkAxial.x}, {chunkAxial.y}) which converts to World Position: {chunkPos}");

            HexUtilities.DrawHexagon(chunkPos, chunkSize, GridGuide.gridConfig.overlayGridOrientation);
        }
    }

    private void HighlightRealityBubbleHexes(float hexSize)
    {
        foreach (var bubble in realityBubbleSystem.bubbles)
        {
            if (bubble == null) continue;

            Vector3 bubblePosition = bubble.Position;
            Vector2Int centerAxial = HexUtilities.WorldToAxial(bubblePosition, GridGuide.gridConfig.baseGridOrientation, hexSize);
            Debug.Log($"Bubble Position: {bubblePosition} converted to Axial Coordinates: ({centerAxial.x}, {centerAxial.y})");

            Vector3 hexPos = HexUtilities.AxialToWorld(centerAxial.x, centerAxial.y, GridGuide.gridConfig.baseGridOrientation, hexSize);
            Debug.Log($"Highlighting Hex at Axial: ({centerAxial.x}, {centerAxial.y}) which converts to World Position: {hexPos}");

            HexUtilities.DrawHexagon(hexPos, hexSize, GridGuide.gridConfig.baseGridOrientation);
        }
    }
       
    private void DrawDrunkWalkPerimeter(float hexSize, HexOrientation orientation) //DEBUG TEST FUNCTION
    {
        Vector2Int current = new Vector2Int(0, 0);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { current };
        Vector2Int[] neighborDirections = HexUtilities.GetNeighborOffsets(orientation);

        int steps = 20;
        for (int i = 0; i < steps; i++)
        {
            int dirIndex = Random.Range(0, neighborDirections.Length);
            Vector2Int stepDir = neighborDirections[dirIndex];
            current = new Vector2Int(current.x + stepDir.x, current.y + stepDir.y);
            visited.Add(current);
        }

        HexUtilities.DrawCollectionOuterEdges(visited, hexSize, orientation);
    }
       
}
