using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileFounderyIO_V3
{
    private static readonly string layoutsRoot = "Assets/Resources/BuildingLayouts";

    // All of your palette roots
    private static readonly string[] paletteRoots = new[]
    {
        "Assets/Resources/GroundPalettes",
        "Assets/Resources/WallPalettes",
        "Assets/Resources/FurniturePalettes",
        "Assets/Resources/ItemPalettes",
        "Assets/Resources/OverlayPalettes",
        "Assets/Resources/NodePalettes"
    };

    public static void SaveLayout(string layoutName, BuildingLayoutData data)
    {
        if (string.IsNullOrEmpty(layoutName))
        {
            Debug.LogError("[TileFounderyIO] Layout name is empty.");
            return;
        }

        string folderPath = Path.Combine(layoutsRoot, layoutName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string jsonPath = Path.Combine(folderPath, "layout.json");
        string previewPath = Path.Combine(folderPath, "preview.png");

        File.WriteAllText(jsonPath, JsonUtility.ToJson(data, true));
        GeneratePreviewTexture(data, previewPath);

        AssetDatabase.Refresh();
        Debug.Log($"[TileFounderyIO] Saved layout '{layoutName}' with preview at '{previewPath}'.");
    }

    public static BuildingLayoutData LoadLayout(string jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
        {
            Debug.LogError($"[TileFounderyIO] Cannot load layout. File not found: {jsonPath}");
            return null;
        }
        return JsonUtility.FromJson<BuildingLayoutData>(File.ReadAllText(jsonPath));
    }

    private static void GeneratePreviewTexture(BuildingLayoutData data, string outputPath)
    {
        int w = data.width, h = data.height;
        string[,] grid = data.ToGrid();

        const int cellSize = 32;
        Texture2D composite = new Texture2D(w * cellSize, h * cellSize, TextureFormat.RGBA32, false);

        // Grab the preview cache from the open editor window
        var core = EditorWindow.GetWindow<TileFoundryCore_V3>();
        var cache = core?.TilePreviewCache;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                string key = grid[x, y];
                Texture2D cellTex = null;

                if (!string.IsNullOrEmpty(key))
                {
                    // First check the cache
                    if (cache != null && cache.TryGetValue(key, out var cachedTex))
                    {
                        cellTex = cachedTex;
                    }
                    else
                    {
                        // Try to find any matching asset
                        var asset = FindAssetByName(key);
                        if (asset != null)
                        {
                            cellTex = ExtractTexture(asset);
                            if (cellTex != null && cache != null)
                            {
                                cellTex.hideFlags = HideFlags.HideAndDontSave;
                                cache[key] = cellTex;
                            }
                        }
                    }
                }

                if (cellTex == null)
                    cellTex = CreateEmptyPreview(cellSize);

                // resize (nearest‐neighbor)
                var resized = ResizeTexture(cellTex, cellSize, cellSize);
                var pixels = resized.GetPixels();

                int px = x * cellSize, py = (h - 1 - y) * cellSize;
                composite.SetPixels(px, py, cellSize, cellSize, pixels);

                Object.DestroyImmediate(resized);
            }
        }

        composite.Apply();
        File.WriteAllBytes(outputPath, composite.EncodeToPNG());
        Object.DestroyImmediate(composite);

        // tell the left sidebar to pick it up
        TileFoundryLeftSidebar_V3.RefreshLayoutPreviews();
    }

    private static Object FindAssetByName(string name)
    {
        // 1) TileBase
        foreach (var root in paletteRoots)
        {
            var guids = AssetDatabase.FindAssets($"t:TileBase {name}", new[] { root });
            foreach (var g in guids)
            {
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(g));
                if (tile != null && tile.name == name) return tile;
            }
        }

        // 2) Sprite
        foreach (var root in paletteRoots)
        {
            var guids = AssetDatabase.FindAssets($"t:Sprite {name}", new[] { root });
            foreach (var g in guids)
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g));
                if (spr != null && spr.name == name) return spr;
            }
        }

        // 3) Prefab w/ SpriteRenderer
        foreach (var root in paletteRoots)
        {
            var guids = AssetDatabase.FindAssets($"t:Prefab {name}", new[] { root });
            foreach (var g in guids)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                if (go != null && go.name == name && go.GetComponent<SpriteRenderer>() != null)
                    return go;
            }
        }

        return null;
    }

    private static Texture2D ExtractTexture(Object asset)
    {
        if (asset is TileBase tile && (tile as Tile)?.sprite != null)
            return SpriteToTexture((tile as Tile).sprite);

        if (asset is Sprite spr)
            return SpriteToTexture(spr);

        if (asset is GameObject go && go.GetComponent<SpriteRenderer>() is var sr && sr?.sprite != null)
            return SpriteToTexture(sr.sprite);

        return null;
    }

    private static Texture2D SpriteToTexture(Sprite s)
    {
        var tex = new Texture2D((int)s.rect.width, (int)s.rect.height, TextureFormat.RGBA32, false);
        tex.SetPixels(s.texture.GetPixels(
            (int)s.rect.x, (int)s.rect.y,
            (int)s.rect.width, (int)s.rect.height));
        tex.Apply();
        return tex;
    }

    private static Texture2D ResizeTexture(Texture2D src, int w, int h)
    {
        var dst = new Texture2D(w, h, src.format, false);
        var srcPixels = src.GetPixels();
        int sw = src.width, sh = src.height;
        var dstPixels = new Color[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int sx = Mathf.Clamp((int)(x * (sw / (float)w)), 0, sw - 1);
                int sy = Mathf.Clamp((int)(y * (sh / (float)h)), 0, sh - 1);
                dstPixels[y * w + x] = srcPixels[sy * sw + sx];
            }

        dst.SetPixels(dstPixels);
        dst.Apply();
        return dst;
    }

    private static Texture2D CreateEmptyPreview(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var gray = new Color(0.5f, 0.5f, 0.5f, 1f);
        var pixels = Enumerable.Repeat(gray, size * size).ToArray();
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
