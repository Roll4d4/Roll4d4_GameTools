using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Handles the left-hand sidebar UI for layout management in Tile Foundery.
/// Displays existing saved layouts with previews, and allows users to load, delete, or refresh them.
/// </summary>
public static class TileFoundryLeftSidebar_V3
{
    // Scroll position for the layout list view
    private static Vector2 scrollPos;

    // Cached layout preview data
    private static List<LayoutPreviewInfo> layoutPreviewsCache = new();

    // Deletion staging variables
    private static string folderToDelete = null;
    private static string layoutNameToDelete = null;

    /// <summary>
    /// Draws the left sidebar panel, including layout thumbnails and action buttons.
    /// </summary>
    public static void Draw(Rect position, TileFoundryCore_V3 core)
    {
        GUILayout.BeginArea(position, EditorStyles.helpBox);
        GUILayout.Label("Layouts", EditorStyles.boldLabel);

        // Refresh button to re-read layout folders
        if (GUILayout.Button("↻ Refresh Layouts"))
        {
            RefreshLayoutPreviews();
        }

        // Manual redraw of the grid (useful after undo operations or updates)
        if (GUILayout.Button("🔄 Redraw Grid"))
        {
            RedrawGrid(core);
        }

        // Scrollable list of layouts
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var info in layoutPreviewsCache)
        {
            GUILayout.Label(info.layoutName, EditorStyles.boldLabel);

            if (info.preview != null)
            {
                GUILayout.Box(info.preview, GUILayout.Width(core.PreviewSize), GUILayout.Height(core.PreviewSize));
            }

            // Load selected layout
            if (GUILayout.Button("Load", GUILayout.Width(core.PreviewSize)))
            {
                core.GridController.ClearAllGrids();

                string layoutJsonPath = Path.Combine(info.folderPath, "layout.json");
                BuildingLayoutData loadedData = TileFounderyIO_V3.LoadLayout(layoutJsonPath);

                if (loadedData != null)
                {
                    core.GridController.SetGridState(
                        loadedData.ToGroundGrid(),
                        loadedData.topEdgeToggles ?? new bool[loadedData.width],
                        loadedData.bottomEdgeToggles ?? new bool[loadedData.width],
                        loadedData.leftEdgeToggles ?? new bool[loadedData.height],
                        loadedData.rightEdgeToggles ?? new bool[loadedData.height]
                    );

                    core.GridController.WallsGrid = loadedData.ToWallGrid();
                    core.GridController.FurnitureGrid = loadedData.ToFurnitureGrid();
                    core.GridController.ItemGrid = loadedData.ToItemGrid();
                    core.GridController.OverlayGrid = loadedData.ToOverlayGrid();
                    core.GridController.NodeGrid = loadedData.ToNodeGrid();

                    core.CurrentLayoutName = info.layoutName;
                    core.Repaint();
                }
                else
                {
                    Debug.LogError("Failed to load layout from " + layoutJsonPath);
                }
            }

            // Delete layout (with confirmation dialog)
            if (GUILayout.Button("Delete", GUILayout.Width(core.PreviewSize)))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Delete Layout",
                    $"Are you sure you want to permanently delete '{info.layoutName}'?",
                    "Delete", "Cancel"
                );

                if (confirm)
                {
                    folderToDelete = info.folderPath;
                    layoutNameToDelete = info.layoutName;
                }
            }

            GUILayout.Space(10);
        }
        EditorGUILayout.EndScrollView();

        // Deferred layout deletion logic
        if (!string.IsNullOrEmpty(folderToDelete))
        {
            if (Directory.Exists(folderToDelete))
            {
                Directory.Delete(folderToDelete, true);
                Debug.Log($"Deleted layout: {layoutNameToDelete}");
            }

            string metaPath = folderToDelete + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
                Debug.Log($"Deleted meta file: {metaPath}");
            }

            folderToDelete = null;
            layoutNameToDelete = null;
            AssetDatabase.Refresh();

            RefreshLayoutPreviews();
            core.Repaint();
        }

        GUILayout.EndArea();
    }

    /// <summary>
    /// Scans the layout directory and rebuilds the preview cache.
    /// </summary>
    public static void RefreshLayoutPreviews()
    {
        layoutPreviewsCache.Clear();
        string path = "Assets/Resources/BuildingLayouts";

        if (!Directory.Exists(path))
            return;

        foreach (var folder in Directory.GetDirectories(path))
        {
            string layoutName = Path.GetFileName(folder);
            string previewPath = Path.Combine(folder, "preview.png");

            Texture2D preview = null;
            if (File.Exists(previewPath))
            {
                byte[] imageBytes = File.ReadAllBytes(previewPath);
                preview = new Texture2D(2, 2);
                preview.LoadImage(imageBytes);
            }

            layoutPreviewsCache.Add(new LayoutPreviewInfo
            {
                layoutName = layoutName,
                folderPath = folder,
                preview = preview
            });
        }
    }

    /// <summary>
    /// Forces a redraw of the central grid panel.
    /// </summary>
    private static void RedrawGrid(TileFoundryCore_V3 core)
    {
        Debug.Log("Redrawing Grid...");
        core.Repaint();
    }

    /// <summary>
    /// Internal data structure for layout metadata and thumbnail previews.
    /// </summary>
    private class LayoutPreviewInfo
    {
        public string layoutName;
        public string folderPath;
        public Texture2D preview;
    }
}
