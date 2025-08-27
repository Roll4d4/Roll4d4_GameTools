// Editor/AutoLayoutTool.cs
// Adds fraction/weight-based division for columns/rows.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class AutoLayoutTool : EditorWindow
{
    private enum LayoutMode { Grid, Horizontal, Vertical }

    [Header("Target")]
    private RectTransform parent;
    private bool includeInactive = true;
    private bool sortByHierarchyOrder = true;

    [Header("Resolution / Basis")]
    private bool useScreenOrCanvasResolution = true;
    private bool preferCanvasScaler = true;
    private Vector2 overrideResolution = new Vector2(1920, 1080);

    [Header("Layout")]
    private LayoutMode mode = LayoutMode.Grid;

    // Equal-division fallback controls
    private int columns = 3;
    private int rows = 0; // 0 = auto

    // FRACTIONS
    private bool useFractions = true;
    private string columnFractionsCsv = "1,1,1"; // example: "1,2,1" -> 25/50/25
    private string rowFractionsCsv = "1";        // example: "2,1"   -> 66/33
    private bool normalizeFractions = true;      // scale to sum=1

    private bool autoCellFromResolution = true;
    private Vector2 cellSize = new Vector2(100, 100);
    private Vector2 spacing = new Vector2(10, 10);
    private Vector4 padding = new Vector4(10, 10, 10, 10); // L,T,R,B
    private bool setCellSize = true;
    private bool anchorTopLeft = true;
    private bool zeroRotationScale = true;

    [Header("Utilities")]
    private bool autoDetectParentFromSelection = true;
    private string renamePrefix = "Item";
    private Color placeholderColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);

    [MenuItem("Tools/UI/Auto Layout Tool %#L")]
    public static void Open()
    {
        var w = GetWindow<AutoLayoutTool>("Auto Layout");
        w.minSize = new Vector2(380, 560);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        autoDetectParentFromSelection = EditorGUILayout.Toggle("Auto Detect from Selection", autoDetectParentFromSelection);
        using (new EditorGUI.DisabledScope(autoDetectParentFromSelection))
            parent = (RectTransform)EditorGUILayout.ObjectField("Parent (RectTransform)", parent, typeof(RectTransform), true);

        includeInactive = EditorGUILayout.Toggle("Include Inactive Children", includeInactive);
        sortByHierarchyOrder = EditorGUILayout.Toggle("Sort by Hierarchy Order (else Name)", sortByHierarchyOrder);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Resolution / Basis", EditorStyles.boldLabel);
        useScreenOrCanvasResolution = EditorGUILayout.Toggle("Use Screen/Reference Resolution", useScreenOrCanvasResolution);
        if (useScreenOrCanvasResolution)
            preferCanvasScaler = EditorGUILayout.Toggle("Prefer CanvasScaler.referenceResolution", preferCanvasScaler);
        else
            overrideResolution = EditorGUILayout.Vector2Field("Manual Resolution (W,H)", overrideResolution);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        mode = (LayoutMode)EditorGUILayout.EnumPopup("Mode", mode);

        // Fractions block
        useFractions = EditorGUILayout.Toggle(new GUIContent("Use Fractions (weights)"), useFractions);
        if (useFractions)
        {
            if (mode != LayoutMode.Vertical)
                columnFractionsCsv = EditorGUILayout.TextField(new GUIContent("Column Fractions (CSV)"), columnFractionsCsv);
            if (mode != LayoutMode.Horizontal)
                rowFractionsCsv = EditorGUILayout.TextField(new GUIContent("Row Fractions (CSV)"), rowFractionsCsv);
            normalizeFractions = EditorGUILayout.Toggle(new GUIContent("Normalize Fractions (sum=1)"), normalizeFractions);
        }
        else
        {
            if (mode != LayoutMode.Vertical)
                columns = Mathf.Max(1, EditorGUILayout.IntField("Columns (H/Grid)", columns));
            if (mode != LayoutMode.Horizontal)
                rows = Mathf.Max(0, EditorGUILayout.IntField("Rows (V/Grid, 0=auto)", rows));
        }

        autoCellFromResolution = EditorGUILayout.Toggle("Auto cell from resolution", autoCellFromResolution);
        using (new EditorGUI.DisabledScope(autoCellFromResolution))
            cellSize = EditorGUILayout.Vector2Field("Cell Size", cellSize);

        setCellSize = EditorGUILayout.Toggle("Force Cell Size", setCellSize);
        spacing = EditorGUILayout.Vector2Field("Spacing (x,y)", spacing);
        padding = EditorGUILayout.Vector4Field("Padding (L,T,R,B)", padding);
        anchorTopLeft = EditorGUILayout.Toggle("Anchor Top-Left", anchorTopLeft);
        zeroRotationScale = EditorGUILayout.Toggle("Zero Rotation/Scale", zeroRotationScale);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        renamePrefix = EditorGUILayout.TextField("Rename Prefix", renamePrefix);
        placeholderColor = EditorGUILayout.ColorField("Placeholder Color", placeholderColor);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Panels for Division"))
                CreatePanelsForDivision();
            if (GUILayout.Button("Rename Children"))
                RenameChildren();
        }

        if (GUILayout.Button("Apply Layout", GUILayout.Height(32)))
            ApplyLayout();

        if (GUILayout.Button("Select Children"))
            SelectChildren();
    }

    // ---------- Helpers ----------
    private RectTransform GetParent()
    {
        if (!autoDetectParentFromSelection) return parent;
        var sel = Selection.activeTransform;
        if (!sel) return parent;
        return sel as RectTransform ?? sel.GetComponentInParent<RectTransform>();
    }

    private List<RectTransform> GetChildren(RectTransform p)
    {
        var list = new List<RectTransform>();
        foreach (Transform c in p)
        {
            if (!includeInactive && !c.gameObject.activeInHierarchy) continue;
            if (c is RectTransform rt) list.Add(rt);
        }
        return sortByHierarchyOrder ? list.OrderBy(t => t.GetSiblingIndex()).ToList()
                                    : list.OrderBy(t => t.name).ToList();
    }

    private Vector2 GetParentContentSize(RectTransform p)
    {
        if (p && p.rect.width > 0.1f && p.rect.height > 0.1f)
            return new Vector2(p.rect.width, p.rect.height);
        return GetBasisResolution(p);
    }

    private Vector2 GetBasisResolution(RectTransform p)
    {
        if (!useScreenOrCanvasResolution)
            return SafeResolution(overrideResolution);

        if (preferCanvasScaler)
        {
            var scaler = p ? p.GetComponentInParent<CanvasScaler>() : null;
            if (scaler && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                var rr = scaler.referenceResolution;
                if (rr.x > 0.1f && rr.y > 0.1f) return SafeResolution(rr);
            }
        }
        var gv = GetMainGameViewSize();
        if (gv.x > 0.1f && gv.y > 0.1f) return SafeResolution(gv);
        return SafeResolution(overrideResolution);
    }

    private static Vector2 SafeResolution(Vector2 v) => new Vector2(Mathf.Max(1, v.x), Mathf.Max(1, v.y));

    private static Vector2 GetMainGameViewSize()
    {
        var T = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (T == null) return Vector2.zero;
        var m = T.GetMethod("GetMainGameViewTargetSize", BindingFlags.NonPublic | BindingFlags.Static);
        if (m == null) return Vector2.zero;
        return (Vector2)m.Invoke(null, null);
    }

    private static List<float> ParseFractions(string csv, bool normalize, int minCountIfEmpty = 1)
    {
        var list = new List<float>();
        if (!string.IsNullOrWhiteSpace(csv))
        {
            foreach (var token in csv.Split(','))
            {
                if (float.TryParse(token.Trim(), out var f) && f > 0f)
                    list.Add(f);
            }
        }
        if (list.Count == 0) list.AddRange(Enumerable.Repeat(1f, minCountIfEmpty));

        if (normalize)
        {
            var sum = list.Sum();
            if (sum > 0f)
                for (int i = 0; i < list.Count; i++)
                    list[i] = list[i] / sum; // sums to 1
        }
        return list;
    }

    // Returns per-column widths and per-row heights (already spacing-aware).
    private void ComputeSegmentSizes(RectTransform p, out float[] colW, out float[] rowH)
    {
        var parentSize = GetParentContentSize(p);
        float innerW = Mathf.Max(1, parentSize.x - (padding.x + padding.z));
        float innerH = Mathf.Max(1, parentSize.y - (padding.y + padding.w));

        if (useFractions)
        {
            // Fractions
            var cols = (mode != LayoutMode.Vertical) ? ParseFractions(columnFractionsCsv, normalizeFractions) : new List<float> { 1f };
            var rowsL = (mode != LayoutMode.Horizontal) ? ParseFractions(rowFractionsCsv, normalizeFractions) : new List<float> { 1f };

            int c = Mathf.Max(1, cols.Count);
            int r = Mathf.Max(1, rowsL.Count);

            // If normalized, sums are 1. If not, scale by sum.
            float colSum = cols.Sum();
            float rowSum = rowsL.Sum();

            float wSpaceTotal = (c - 1) * spacing.x;
            float hSpaceTotal = (r - 1) * spacing.y;

            float usableW = Mathf.Max(0, innerW - wSpaceTotal);
            float usableH = Mathf.Max(0, innerH - hSpaceTotal);

            colW = new float[c];
            rowH = new float[r];

            for (int i = 0; i < c; i++)
                colW[i] = usableW * (normalizeFractions ? cols[i] : (cols[i] / Mathf.Max(0.0001f, colSum)));

            for (int j = 0; j < r; j++)
                rowH[j] = usableH * (normalizeFractions ? rowsL[j] : (rowsL[j] / Mathf.Max(0.0001f, rowSum)));
        }
        else
        {
            // Equal division behavior
            int c = (mode != LayoutMode.Vertical) ? Mathf.Max(1, columns) : 1;
            int r = (mode != LayoutMode.Horizontal) ? (rows > 0 ? rows : 1) : 1;

            float wSpaceTotal = (c - 1) * spacing.x;
            float hSpaceTotal = (r - 1) * spacing.y;

            float usableW = Mathf.Max(0, innerW - wSpaceTotal);
            float usableH = Mathf.Max(0, innerH - hSpaceTotal);

            float cw = usableW / c;
            float rh = usableH / r;

            colW = Enumerable.Repeat(cw, c).ToArray();
            rowH = Enumerable.Repeat(rh, r).ToArray();
        }
    }

    // ---------- Creation ----------
    private void CreatePanelsForDivision()
    {
        var p = GetParent();
        if (!p) { ShowNotification(new GUIContent("No parent RectTransform selected.")); return; }

        ComputeSegmentSizes(p, out var colW, out var rowH);

        int c = colW.Length;
        int r = rowH.Length;
        int toCreate = mode switch
        {
            LayoutMode.Horizontal => c,
            LayoutMode.Vertical => r,
            _ => c * r
        };

        Undo.RegisterFullObjectHierarchyUndo(p.gameObject, "Create Panels for Division");

        int startIndex = p.childCount;
        for (int i = 0; i < toCreate; i++)
        {
            var go = new GameObject($"{renamePrefix}_{startIndex + i:00}", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(p, false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            var img = go.GetComponent<Image>();
            img.color = placeholderColor;
        }

        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        ShowNotification(new GUIContent($"Created {toCreate} panel(s)."));
    }

    private void RenameChildren()
    {
        var p = GetParent();
        if (!p) { ShowNotification(new GUIContent("No parent RectTransform selected.")); return; }
        var kids = GetChildren(p);
        Undo.RegisterFullObjectHierarchyUndo(p.gameObject, "Rename Children");
        for (int i = 0; i < kids.Count; i++)
            kids[i].name = $"{renamePrefix}_{i:00}";
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
    }

    private void SelectChildren()
    {
        var p = GetParent();
        if (!p) return;
        var kids = GetChildren(p).Select(rt => rt.gameObject).ToArray();
        Selection.objects = kids;
    }

    // ---------- Apply Layout ----------
    private void ApplyLayout()
    {
        var p = GetParent();
        if (!p) { ShowNotification(new GUIContent("No parent RectTransform selected.")); return; }

        var kids = GetChildren(p);
        if (kids.Count == 0) { ShowNotification(new GUIContent("No children to layout.")); return; }

        // Compute segments
        ComputeSegmentSizes(p, out var colW, out var rowH);

        // Optionally push cell sizes (for Horizontal/Vertical this is one axis only)
        Undo.RegisterFullObjectHierarchyUndo(p.gameObject, "Auto Layout");

        foreach (var rt in kids)
        {
            if (anchorTopLeft)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
            }
            if (zeroRotationScale)
            {
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }
        }

        // Layout origin inside parent: (L, -T)
        float startX = padding.x;
        float startY = -padding.y;

        int idx = 0;
        switch (mode)
        {
            case LayoutMode.Horizontal:
                {
                    float x = startX;
                    for (int i = 0; i < colW.Length && i < kids.Count; i++)
                    {
                        var rt = kids[i];
                        if (setCellSize)
                            rt.sizeDelta = new Vector2(colW[i], (rowH.Length > 0 ? rowH[0] : rt.sizeDelta.y));

                        rt.anchoredPosition = new Vector2(x, startY);
                        x += colW[i] + spacing.x;
                    }
                    break;
                }

            case LayoutMode.Vertical:
                {
                    float y = startY;
                    for (int j = 0; j < rowH.Length && j < kids.Count; j++)
                    {
                        var rt = kids[j];
                        if (setCellSize)
                            rt.sizeDelta = new Vector2((colW.Length > 0 ? colW[0] : rt.sizeDelta.x), rowH[j]);

                        rt.anchoredPosition = new Vector2(startX, y);
                        y -= rowH[j] + spacing.y;
                    }
                    break;
                }

            case LayoutMode.Grid:
                {
                    for (int r = 0; r < rowH.Length; r++)
                    {
                        float y = startY - rowAccum(rowH, r) - r * spacing.y;
                        for (int c = 0; c < colW.Length; c++)
                        {
                            if (idx >= kids.Count) break;
                            float x = startX + colAccum(colW, c) + c * spacing.x;

                            var rt = kids[idx++];
                            if (setCellSize)
                                rt.sizeDelta = new Vector2(colW[c], rowH[r]);

                            rt.anchoredPosition = new Vector2(x, y);
                        }
                    }
                    break;
                }
        }

        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);

        // local helpers
        float colAccum(float[] arr, int upToExclusive)
        {
            float sum = 0f;
            for (int i = 0; i < upToExclusive; i++) sum += arr[i];
            return sum;
        }
        float rowAccum(float[] arr, int upToExclusive)
        {
            float sum = 0f;
            for (int i = 0; i < upToExclusive; i++) sum += arr[i];
            return sum;
        }
    }
}
