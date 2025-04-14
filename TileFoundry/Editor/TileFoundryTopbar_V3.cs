using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Renders the top bar UI of the Tile Foundery editor window.
/// Handles layout saving, brush settings, layer selection, and tool mode switching.
/// </summary>
public static class TileFoundryTopbar_V3
{
    /// <summary>
    /// Updates the editor's selected tile and name, normalizing for consistency.
    /// </summary>
    public static void SetSelectedTile(string tileName, TileBase tile, TileFoundryCore_V3 core)
    {
        string normalized = tileName.Trim().ToLowerInvariant();
        core.CurrentTileName = normalized;
        core.SelectedTileAsset = tile;
    }

    /// <summary>
    /// Draws the top bar UI section including:
    /// - Layout save button
    /// - Layer selector
    /// - Brush preview
    /// - Display options
    /// - Tool selector
    /// </summary>
    public static void Draw(Rect position, TileFoundryCore_V3 core)
    {
        GUILayout.BeginArea(position, EditorStyles.helpBox);
        {
            EditorGUILayout.BeginVertical();
            {
                // ──────────────────────────────────────────────────────
                // Row 1: Layout name input and save button
                // ──────────────────────────────────────────────────────
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Layout Name:", GUILayout.Width(90));
                    core.CurrentLayoutName = EditorGUILayout.TextField(core.CurrentLayoutName ?? "NewLayout");

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("💾 Save", GUILayout.Width(80)))
                    {
                        // Serialize the current grid state into a layout object
                        var data = new BuildingLayoutData(
                            core.GridController.GridWidth,
                            core.GridController.GridHeight,
                            core.GridController.GroundGrid,
                            LayoutCategory.Residential, // Could be exposed via dropdown
                            core.GridController.TopEdgeToggles,
                            core.GridController.BottomEdgeToggles,
                            core.GridController.LeftEdgeToggles,
                            core.GridController.RightEdgeToggles,
                            core.GridController.ItemGrid,
                            core.GridController.OverlayGrid,
                            core.GridController.WallsGrid,
                            core.GridController.NodeGrid,
                            core.GridController.FurnitureGrid
                        );

                        TileFounderyIO_V3.SaveLayout(core.CurrentLayoutName, data);
                        Debug.Log($"[TileFoundry] Saved layout '{core.CurrentLayoutName}'");
                    }
                }
                EditorGUILayout.EndHorizontal();

                // ──────────────────────────────────────────────────────
                // Row 2: Layer selection toolbar
                // ──────────────────────────────────────────────────────
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Layer:", GUILayout.Width(40));
                    int newLayer = GUILayout.Toolbar(core.SelectedLayer, new[] { "Ground", "Walls", "Furniture", "Item", "Overlay", "Node" });

                    if (newLayer != core.SelectedLayer)
                    {
                        core.SelectedLayer = newLayer;
                        core.CurrentTileName = null;
                        core.SelectedTileAsset = null;

                        // Refresh palette view when changing layers
                        TileFoundryRightSidebar_V3.LoadTileCategories(core);
                        core.Repaint();
                    }
                }
                EditorGUILayout.EndHorizontal();

                // ──────────────────────────────────────────────────────
                // Row 3: Brush preview and layer display options
                // ──────────────────────────────────────────────────────
                EditorGUILayout.BeginHorizontal();
                {
                    // Left: Active brush info and preview
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("Current Brush:", GUILayout.Width(90));
                        EditorGUILayout.LabelField(core.CurrentTileName ?? "None", GUILayout.Width(100));

                        Rect previewRect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                        if (core.SelectedTileAsset != null)
                        {
                            Texture2D preview = AssetPreview.GetAssetPreview(core.SelectedTileAsset);
                            if (preview != null)
                                GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
                            else
                                EditorGUI.DrawRect(previewRect, Color.gray * 0.5f);
                        }
                        else
                        {
                            EditorGUI.DrawRect(previewRect, Color.gray * 0.25f);
                        }
                    }
                    EditorGUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    // Right: Display settings (preview size and inactive layer transparency)
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("Preview Size:", GUILayout.Width(90));
                        core.PreviewSize = EditorGUILayout.Slider(core.PreviewSize, 32f, 128f, GUILayout.Width(200));

                        EditorGUILayout.LabelField("Inactive Alpha:", GUILayout.Width(90));
                        core.InactiveLayerAlpha = EditorGUILayout.Slider(core.InactiveLayerAlpha, 0f, 1f, GUILayout.Width(200));
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                // ──────────────────────────────────────────────────────
                // Row 4: Tool selection toolbar
                // ──────────────────────────────────────────────────────
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Tool:", GUILayout.Width(30));
                    core.SelectedTool = GUILayout.Toolbar(core.SelectedTool, new[] { "Brush", "FloodFill", "Line", "Square", "Circle", "Hex" });
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        GUILayout.EndArea();
    }
}
