using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Main editor window for the Tile Foundery V3 system.
/// This window handles layout, toolbars, and coordination between sidebar tools and the central grid editor.
/// </summary>
public class TileFoundryCore_V3 : EditorWindow
{
    // Layout constants for UI structure
    private const float sidebarWidth = 220f;
    private const float topbarHeight = 160f;
    private const float bottombarHeight = 100f;

    // Currently selected tile state
    public string CurrentTileName;
    public Object SelectedTileAsset;

    // Optional layer-specific tile state (for future expansion)
    public string CurrentItemTileName;
    public TileBase SelectedItemTileAsset;
    public string CurrentOverlayTileName;
    public TileBase SelectedOverlayTileAsset;

    // Editor state variables
    public string CurrentLayoutName = "NewLayout";
    public int SelectedLayer = 0; // 0: Ground, 1: Walls, etc.
    public int SelectedTool = 0;  // 0: Brush, 1: FloodFill, etc.
    public float PreviewSize = 64f;
    public float InactiveLayerAlpha = 0.3f; // Alpha multiplier for unselected layers
    public Vector2 ScrollPosition = Vector2.zero;
    public float VerticalPan = 0f;
    public float ZoomFactor = 1f;

    // Core data and caches
    public GridController GridController;
    public Dictionary<string, Object> TileAssetLookup { get; private set; } = new();
    public Dictionary<string, Texture2D> TilePreviewCache { get; private set; } = new();

    /// <summary>
    /// Menu entry point for launching the editor window.
    /// </summary>
    [MenuItem("Tools/Tile Foundery V3")]
    public static void OpenWindow()
    {
        TileFoundryCore_V3 window = GetWindow<TileFoundryCore_V3>("Tile Foundery V3");
        window.minSize = new Vector2(800, 600);
    }

    /// <summary>
    /// Main GUI layout and delegation.
    /// </summary>
    private void OnGUI()
    {
        // Calculate sub-regions for each section of the editor
        Rect leftSidebarRect = new Rect(0, 0, sidebarWidth, position.height);
        Rect rightSidebarRect = new Rect(position.width - sidebarWidth, 0, sidebarWidth, position.height);
        Rect topbarRect = new Rect(sidebarWidth, 0, position.width - 2 * sidebarWidth, topbarHeight);
        Rect bottombarRect = new Rect(sidebarWidth, position.height - bottombarHeight, position.width - 2 * sidebarWidth, bottombarHeight);
        Rect gridRect = new Rect(sidebarWidth, topbarHeight, position.width - 2 * sidebarWidth, position.height - topbarHeight - bottombarHeight);

        // Draw each component of the UI
        TileFoundryLeftSidebar_V3.Draw(leftSidebarRect, this);
        TileFoundryRightSidebar_V3.Draw(rightSidebarRect, this);
        TileFoundryTopbar_V3.Draw(topbarRect, this);
        TileFoundryBottombar_V3.Draw(bottombarRect, this);
        TileFoundryGrid_V3.Draw(gridRect, this);
    }

    /// <summary>
    /// Initialize the window and load required data.
    /// </summary>
    private void OnEnable()
    {
        GridController = new GridController(20, 20);
        TileFoundryRightSidebar_V3.ForceRefresh(this);              // Load assets and populate sidebar
        TileFoundryLeftSidebar_V3.RefreshLayoutPreviews();          // Load available saved layouts
    }

    /// <summary>
    /// Clean up native objects and caches when the window is closed.
    /// </summary>
    private void OnDisable()
    {
        // Properly release all temporary preview textures
        foreach (var tex in TilePreviewCache.Values)
        {
            if (tex != null)
                Object.DestroyImmediate(tex);
        }
        TilePreviewCache.Clear();
    }
}
