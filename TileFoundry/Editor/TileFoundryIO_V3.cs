using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileFounderyIO_V3
{
    private static readonly string layoutsRoot = "Assets/Resources/BuildingLayouts";

    /// <summary>
    /// Saves the building layout data as JSON and generates a preview thumbnail based on the actual tile asset images.
    /// </summary>
    public static void SaveLayout(string layoutName, BuildingLayoutData data)
    {
        if (string.IsNullOrEmpty(layoutName))
        {
            Debug.LogError("[TileFoundryIO] Layout name is empty.");
            return;
        }

        string folderPath = Path.Combine(layoutsRoot, layoutName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string jsonPath = Path.Combine(folderPath, "layout.json");
        string previewPath = Path.Combine(folderPath, "preview.png");

        // Serialize data to JSON
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(jsonPath, json);

        // Generate preview thumbnail using actual tile asset images.
        GeneratePreviewTexture(data, previewPath);

        AssetDatabase.Refresh();
        Debug.Log($"[TileFoundryIO] Saved layout '{layoutName}' to {folderPath}");
    }

    /// <summary>
    /// Loads a layout from the specified JSON path.
    /// </summary>
    public static BuildingLayoutData LoadLayout(string jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
        {
            Debug.LogError($"[TileFoundryIO] Cannot load layout — file not found at: {jsonPath}");
            return null;
        }
        string json = File.ReadAllText(jsonPath);
        var data = JsonUtility.FromJson<BuildingLayoutData>(json);
        return data;
    }

    public static BuildingLayoutData LoadLayoutFromPath(string jsonPath)
    {
        // jsonPath should point to the layout.json file.
        return LoadLayout(jsonPath);
    }

    /// <summary>
    /// Searches for a TileBase asset in "Assets/Resources/TilePalettes" whose name matches the normalized key.
    /// </summary>
    private static TileBase FindTileAssetByName(string normalizedKey)
    {
        // Search for all TileBase assets under TilePalettes.
        string[] guids = AssetDatabase.FindAssets("t:TileBase", new[] { "Assets/Resources/TilePalettes" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null)
            {
                string key = tile.name;
                if (key == normalizedKey)
                {
                    return tile;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Generates a composite preview texture (PNG) using each cell’s tile asset image.
    /// </summary>
    private static void GeneratePreviewTexture(BuildingLayoutData data, string outputPath)
    {
        int width = data.width;
        int height = data.height;
        string[,] grid = data.ToGrid();

        int cellPreviewSize = 32;
        int compositeWidth = width * cellPreviewSize;
        int compositeHeight = height * cellPreviewSize;
        Texture2D composite = new Texture2D(compositeWidth, compositeHeight, TextureFormat.RGBA32, false);

        // Get a reference to the open window to access the shared preview cache
        TileFoundryCore_V3 core = EditorWindow.GetWindow<TileFoundryCore_V3>();
        var cache = core?.TilePreviewCache;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                string assetName = grid[x, y];
                Texture2D cellTex = null;

                if (!string.IsNullOrEmpty(assetName))
                {
                    string key = assetName;
                    TileBase tile = FindTileAssetByName(key);
                    if (tile != null)
                    {
                        if (cache != null && cache.TryGetValue(key, out cellTex))
                        {
                            // Reuse cached texture
                        }
                        else
                        {
                            Sprite sprite = (tile as Tile)?.sprite;
                            if (sprite != null)
                            {
                                cellTex = SpriteToTexture(sprite);
                                if (cache != null && cellTex != null)
                                {
                                    cellTex.hideFlags = HideFlags.HideAndDontSave;
                                    cache[key] = cellTex;
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"No sprite found for tile '{assetName}' at cell ({x},{y}). Using empty preview.");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No asset found for '{assetName}' in TilePalettes at cell ({x},{y}).");
                    }
                }

                if (cellTex == null)
                    cellTex = CreateEmptyPreview();

                Texture2D resized = ResizeTexture(cellTex, cellPreviewSize, cellPreviewSize);
                Color[] cellPixels = resized.GetPixels();

                int destX = x * cellPreviewSize;
                int destY = (height - 1 - y) * cellPreviewSize;
                composite.SetPixels(destX, destY, cellPreviewSize, cellPreviewSize, cellPixels);

                Object.DestroyImmediate(resized); // Explicitly destroy resized temp texture
            }
        }

        composite.Apply();
        byte[] png = composite.EncodeToPNG();
        File.WriteAllBytes(outputPath, png);
        Object.DestroyImmediate(composite); // Clean up composite texture

        Debug.Log($"Preview texture saved to {outputPath}");
        TileFoundryLeftSidebar_V3.RefreshLayoutPreviews();
    }


    /// <summary>
    /// Converts a sprite into a Texture2D.
    /// </summary>
    private static Texture2D SpriteToTexture(Sprite sprite)
    {
        Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
        texture.SetPixels(sprite.texture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height));
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Resizes a texture to new dimensions using nearest-neighbor scaling.
    /// </summary>
    private static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        Texture2D newTex = new Texture2D(newWidth, newHeight, source.format, false);
        Color[] newPixels = new Color[newWidth * newHeight];
        Color[] sourcePixels = source.GetPixels();
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                float gx = (float)x / newWidth;
                float gy = (float)y / newHeight;
                int sx = Mathf.Clamp(Mathf.FloorToInt(gx * sourceWidth), 0, sourceWidth - 1);
                int sy = Mathf.Clamp(Mathf.FloorToInt(gy * sourceHeight), 0, sourceHeight - 1);
                newPixels[y * newWidth + x] = sourcePixels[sy * sourceWidth + sx];
            }
        }
        newTex.SetPixels(newPixels);
        newTex.Apply();
        return newTex;
    }

    /// <summary>
    /// Returns a default (empty) preview texture.
    /// </summary>
    private static Texture2D CreateEmptyPreview()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 1f); // medium gray
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = emptyColor;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
