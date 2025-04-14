using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renders the bottom bar of the Tile Foundery editor.
/// Displays two columns:
/// - Left: All available tile keys found in the loaded asset lookup
/// - Right: All tile keys currently used in the layout's Ground layer
/// </summary>
public static class TileFoundryBottombar_V3
{
    private static Vector2 leftColumnScroll;
    private static Vector2 rightColumnScroll;

    /// <summary>
    /// Draws the bottom bar UI that shows available vs. used tile keys.
    /// Helps the user understand which assets are available and which ones are actually used in the current layout.
    /// </summary>
    public static void Draw(Rect position, TileFoundryCore_V3 core)
    {
        GUILayout.BeginArea(position, EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        // ──────────────────────────────────────────────────────
        // Left Column: All available keys from asset lookup
        // ──────────────────────────────────────────────────────
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 2));
        EditorGUILayout.LabelField("Available Tile Keys", EditorStyles.boldLabel);
        leftColumnScroll = EditorGUILayout.BeginScrollView(leftColumnScroll);

        if (core.TileAssetLookup != null && core.TileAssetLookup.Count > 0)
        {
            foreach (var key in core.TileAssetLookup.Keys)
            {
                EditorGUILayout.LabelField(key);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No tiles loaded.");
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // ──────────────────────────────────────────────────────
        // Right Column: Keys actually used in the Ground layer
        // ──────────────────────────────────────────────────────
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 2));
        EditorGUILayout.LabelField("Current Map Tile Keys", EditorStyles.boldLabel);
        rightColumnScroll = EditorGUILayout.BeginScrollView(rightColumnScroll);

        if (core.GridController?.GroundGrid != null)
        {
            HashSet<string> seenKeys = new();
            int gridSize = core.GridController.GridSize;
            string[,] grid = core.GridController.GroundGrid;

            // Scan the ground grid for all used tile keys
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    string key = grid[x, y];
                    if (!string.IsNullOrEmpty(key))
                        seenKeys.Add(key);
                }
            }

            // Report the keys used
            if (seenKeys.Count == 0)
            {
                EditorGUILayout.LabelField("No tiles placed.");
            }
            else
            {
                foreach (string key in seenKeys)
                {
                    bool exists = core.TileAssetLookup.ContainsKey(key);
                    Color original = GUI.color;

                    // Highlight missing keys in red
                    if (!exists)
                        GUI.color = Color.red;

                    EditorGUILayout.LabelField(key);

                    GUI.color = original;
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("Grid not initialized.");
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
