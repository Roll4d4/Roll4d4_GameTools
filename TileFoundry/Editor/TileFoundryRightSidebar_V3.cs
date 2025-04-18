using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Responsible for displaying and managing the right sidebar in the Tile Foundery editor.
/// This sidebar shows available tile assets organized by categories (folders) and allows users to
/// select a tile asset as the current brush, refresh asset lists, and clear the selection.
/// </summary>
public static class TileFoundryRightSidebar_V3
{
    // Dictionary mapping category names to lists of assets (TileBase, Sprite, or GameObject)
    private static Dictionary<string, List<Object>> tileCategories = new();

    // The currently selected tile category. "Natural" is the default.
    private static string selectedCategory = "Natural";

    // Used for scrolling through the tiles in the category view.
    private static Vector2 rightScroll;

    // Preview size for displaying asset thumbnails in the sidebar.
    private static float previewSize = 64f;

    // Flag indicating if categories have been loaded (caching to avoid repeated processing).
    private static bool categoriesLoaded = false;

    /// <summary>
    /// Draws the right sidebar panel.
    /// The panel includes controls for refreshing tiles, selecting categories, and shows a scrollable list
    /// of asset previews. Clicking a preview sets it as the current tile in the main editor.
    /// </summary>
    public static void Draw(Rect position, TileFoundryCore_V3 core)
    {
        GUILayout.BeginArea(position, EditorStyles.helpBox);

        // Load tile categories once, if not already loaded.
        if (!categoriesLoaded)
            LoadTileCategories(core);

        // If the selected category is missing, set it to the first available key (or empty if none)
        if (!tileCategories.ContainsKey(selectedCategory))
        {
            if (tileCategories.Count > 0)
                selectedCategory = new List<string>(tileCategories.Keys)[0];
            else
                selectedCategory = "";
        }

        EditorGUILayout.LabelField("Tile Categories", EditorStyles.boldLabel);

        // Refresh button to update the asset lists from disk.
        if (GUILayout.Button("↻ Refresh Tiles"))
        {
            RefreshTileAssets(core);
        }

        // Display category buttons (one per key in the tileCategories dictionary).
        foreach (var category in tileCategories.Keys)
        {
            bool wasSelected = selectedCategory == category;
            bool nowSelected = GUILayout.Toggle(wasSelected, category, "Button");
            if (nowSelected && !wasSelected)
            {
                selectedCategory = category;
                core.Repaint();
                GUI.FocusControl(null); // Clear focus to avoid UI glitches
                break;
            }
        }

        GUILayout.Space(5);

        // Display the list of assets in the selected category, allowing for scrolling.
        Vector2 rightScroll = EditorGUILayout.BeginScrollView(new Vector2(0, 0));
        if (tileCategories.TryGetValue(selectedCategory, out var assets))
        {
            // Button to clear the current tile selection.
            if (GUILayout.Button("X (Clear Tile)", GUILayout.Height(previewSize)))
            {
                core.CurrentTileName = "None";
                core.SelectedTileAsset = null;
            }

            // Arrange asset previews in a grid with specified number of columns.
            int columnCount = 3;
            for (int i = 0; i < assets.Count; i += columnCount)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < columnCount; j++)
                {
                    int index = i + j;
                    if (index >= assets.Count) break;

                    Object assetObj = assets[index];
                    if (assetObj == null) continue;

                    string assetName = assetObj.name;
                    // Get or generate the preview texture for this asset.
                    Texture2D preview = GetAssetPreview(assetObj);
                    GUIContent content = new GUIContent(preview, assetName);

                    // On click, set this asset as the current tile asset.
                    if (GUILayout.Button(content, GUILayout.Width(previewSize), GUILayout.Height(previewSize)))
                    {
                        core.CurrentTileName = assetName;
                        core.SelectedTileAsset = assetObj; // This asset can later be cast as needed.
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    /// <summary>
    /// Forces a refresh of tile assets, clearing the core's asset lookup and reloading from disk.
    /// </summary>
    public static void ForceRefresh(TileFoundryCore_V3 core)
    {
        RefreshTileAssets(core);
    }

    /// <summary>
    /// Refreshes the tile asset lookup from a set of predefined directories.
    /// Searches for TileBase, Sprite, and Prefab assets with SpriteRenderer.
    /// Also repopulates tile categories after refreshing.
    /// </summary>
    private static void RefreshTileAssets(TileFoundryCore_V3 core)
    {
        // Clear any existing lookup entries.
        core.TileAssetLookup.Clear();

        // Predefined directories where tile assets can be found.
        string[] directories = new[]
        {
            "Assets/Resources/FurniturePalettes",
            "Assets/Resources/ItemPalettes",
            "Assets/Resources/NodePalettes",
            "Assets/Resources/OverlayPalettes",
            "Assets/Resources/GroundPalettes",
            "Assets/Resources/WallPalettes"
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Debug.LogWarning($"[TileFoundry] Directory '{directory}' does not exist.");
                continue;
            }

            // Load TileBase assets from the directory.
            string[] tileGUIDs = AssetDatabase.FindAssets("t:Tile", new[] { directory });
            foreach (var guid in tileGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (tile != null)
                {
                    string key = tile.name; // Use asset's raw name as key.
                    if (!core.TileAssetLookup.ContainsKey(key))
                    {
                        core.TileAssetLookup.Add(key, tile);
                    }
                }
            }
            // Load Sprite assets from the directory.
            string[] spriteGUIDs = AssetDatabase.FindAssets("t:Sprite", new[] { directory });
            foreach (var guid in spriteGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    string key = sprite.name;
                    if (!core.TileAssetLookup.ContainsKey(key))
                    {
                        core.TileAssetLookup.Add(key, sprite);
                    }
                }
            }
            // Load Prefab assets that include a SpriteRenderer.
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { directory });
            foreach (var guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<SpriteRenderer>() != null)
                {
                    string key = prefab.name;
                    if (!core.TileAssetLookup.ContainsKey(key))
                    {
                        core.TileAssetLookup.Add(key, prefab);
                    }
                }
            }

            Debug.Log($"[TileFoundry] Refreshed assets from '{directory}'. Total now: {core.TileAssetLookup.Count}");
        }

        // Reload the tile categories after the asset lookup has been refreshed.
        LoadTileCategories(core);
        core.Repaint();
    }

    /// <summary>
    /// Loads tile categories based on the currently selected layer.
    /// Each category corresponds to a subfolder within the designated palette folder.
    /// </summary>
    public static void LoadTileCategories(TileFoundryCore_V3 core)
    {
        tileCategories.Clear();
        string palletFolder = "";

        // Determine the palette folder based on the selected layer.
        switch (core.SelectedLayer)
        {
            case 0: palletFolder = "GroundPalettes"; break;
            case 1: palletFolder = "WallPalettes"; break;
            case 2: palletFolder = "FurniturePalettes"; break;
            case 3: palletFolder = "ItemPalettes"; break;
            case 4: palletFolder = "OverlayPalettes"; break;
            case 5: palletFolder = "NodePalettes"; break;
            default: palletFolder = "GroundPalettes"; break;
        }

        string palletsRoot = $"Assets/Resources/{palletFolder}";
        if (!Directory.Exists(palletsRoot))
        {
            Debug.LogWarning($"[TileFoundry] {palletFolder} directory not found.");
            return;
        }

        // For each subfolder (category) in the palette folder, load matching assets.
        foreach (var categoryPath in Directory.GetDirectories(palletsRoot))
        {
            string categoryName = Path.GetFileName(categoryPath);
            var assets = new List<Object>();

            // Load asset files (.asset) which can be TileBase or Sprite.
            foreach (var assetPath in Directory.GetFiles(categoryPath, "*.asset"))
            {
                Object assetObj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (assetObj != null && (assetObj is TileBase || assetObj is Sprite))
                    assets.Add(assetObj);
            }

            // Load prefabs that might be GameObjects with a SpriteRenderer.
            foreach (var prefabPath in Directory.GetFiles(categoryPath, "*.prefab"))
            {
                Object assetObj = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
                if (assetObj != null && assetObj is GameObject go && go.GetComponent<SpriteRenderer>() != null)
                    assets.Add(assetObj);
            }

            tileCategories[categoryName] = assets;
        }
        categoriesLoaded = true;
    }

    /// <summary>
    /// Returns a preview Texture2D for the given asset (TileBase, Sprite, or GameObject with a SpriteRenderer).
    /// If the texture has already been generated and cached in the core's TilePreviewCache, that value is returned.
    /// Otherwise, a new texture is created (and any prior cached texture for the same asset is destroyed).
    /// </summary>
    private static Texture2D GetAssetPreview(Object assetObj)
    {
        if (assetObj == null)
            return null;

        // Get a reference to the main editor window to access the shared preview cache.
        TileFoundryCore_V3 core = EditorWindow.GetWindow<TileFoundryCore_V3>();
        if (core == null)
            return null;

        string key = assetObj.name;
        if (core.TilePreviewCache.TryGetValue(key, out var cached))
            return cached;

        Texture2D preview = null;

        // Determine the asset type and generate a preview accordingly.
        if (assetObj is TileBase tile)
        {
            Sprite sprite = (tile as Tile)?.sprite;
            if (sprite != null)
                preview = SpriteToTexture(sprite);
        }
        else if (assetObj is Sprite spr)
        {
            preview = SpriteToTexture(spr);
        }
        else if (assetObj is GameObject go)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                preview = SpriteToTexture(sr.sprite);
        }

        // Cache the generated texture.
        if (preview != null)
        {
            preview.hideFlags = HideFlags.HideAndDontSave;

            // If an old texture exists in the cache, destroy it before replacing.
            if (core.TilePreviewCache.TryGetValue(key, out var existingTex) && existingTex != null)
            {
                Object.DestroyImmediate(existingTex);
            }

            core.TilePreviewCache[key] = preview;
        }

        return preview;
    }

    /// <summary>
    /// Converts a Sprite to a new Texture2D.
    /// This function extracts the relevant pixels from the sprite's texture using its rect.
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
}
