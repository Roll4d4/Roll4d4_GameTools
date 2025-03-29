using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Provides utility functions for navigating and generating a hex grid using axial and cube coordinates.
/// Supports both flat-topped and pointy-topped hex orientations.
/// </summary>

public static class HexUtilities
{

    private const float HEX_HORIZONTAL_SPACING = 1.5f;  // Horizontal spacing between hexes
    private const float HEX_VERTICAL_SPACING = 1.7320508075689f;  // Vertical spacing for flat-topped hexes

    #region World Conversions to conversions

    public static Vector2Int WorldToAxial(Vector3 worldPos, HexOrientation orientation, float size)
    {
        float q, r;

        if (orientation == HexOrientation.PointyTop)
        {
            q = (Mathf.Sqrt(3) / 3f * worldPos.x - 1f / 3f * worldPos.z) / size;
            r = (2f / 3f * worldPos.z) / size;
        }
        else // FlatTop
        {
            q = (2f / 3f * worldPos.x) / size;
            r = (-1f / 3f * worldPos.x + Mathf.Sqrt(3) / 3f * worldPos.z) / size;
        }

        return RoundAxialCoordinates(q, r);
    }

    #endregion

    #region Axial Coordinate Methods

    /// <summary>
    /// Rounds floating-point axial coordinates to the nearest hex (Q, R).
    /// </summary>
    public static Vector2Int RoundAxialCoordinates(float q, float r)
    {
        int roundedQ = Mathf.RoundToInt(q);
        int roundedR = Mathf.RoundToInt(r);

        return new Vector2Int(roundedQ, roundedR);
    }

    /// <summary>
    /// Converts axial coordinates to chunk coordinates.
    /// </summary>
    public static Vector2Int AxialToChunk(Vector2Int axialCoords, int chunkRadius)
    {
        int chunkQ = Mathf.RoundToInt(axialCoords.x / (float)chunkRadius);
        int chunkR = Mathf.RoundToInt(axialCoords.y / (float)chunkRadius);
        return new Vector2Int(chunkQ, chunkR);
    }
   
    /// <summary>
    /// Converts axial hex coordinates (q, r) to world-space coordinates.
    /// The conversion depends on the hex orientation (FlatTop or PointyTop).
    /// </summary>
    /// <param name="q">The axial q-coordinate of the hex.</param>
    /// <param name="r">The axial r-coordinate of the hex.</param>
    /// <param name="orientation">The hex grid orientation (FlatTop or PointyTop).</param>
    /// <param name="size">The radius (size) of the hexagon.</param>
    /// <returns>A Vector3 representing the world-space position of the hex.</returns>
    public static Vector3 AxialToWorld(int q, int r, HexOrientation orientation, float size)
    {
        if (orientation == HexOrientation.PointyTop)
        {
            // Pointy-top hex layout
            float x = size * Mathf.Sqrt(3) * (q + r / 2f);  // Horizontal offset
            float z = size * 1.5f * r;                      // Vertical distance between rows
            return new Vector3(x, 0, z); // Assuming y is up
        }
        else // FlatTop
        {
            // Flat-top hex layout
            float x = size * 1.5f * q;                      // Horizontal distance between columns
            float z = size * Mathf.Sqrt(3) * (r + q / 2f);  // Vertical offset
            return new Vector3(x, 0, z); // Assuming y is up
        }
    }

    /// <summary>
    /// Calculates the distance between two hexes in axial coordinates.
    /// </summary>
    public static int HexDistance(Vector2Int a, Vector2Int b)
    {
        int dq = Mathf.Abs(a.x - b.x);
        int dr = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dq, dr);
    }

    #endregion


    #region Cube Coordinate Methods

    public static Vector3Int AxialToCube(Vector2Int axial)
    {
        int x = axial.x;
        int z = axial.y;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    /// <summary>
    /// Rounds floating-point cube coordinates to the nearest integer cube coordinates.
    /// </summary>
    public static Vector3Int RoundCubeCoordinates(float x, float y, float z)
    {
        int rx = Mathf.RoundToInt(x);
        int ry = Mathf.RoundToInt(y);
        int rz = Mathf.RoundToInt(z);

        float x_diff = Mathf.Abs(rx - x);
        float y_diff = Mathf.Abs(ry - y);
        float z_diff = Mathf.Abs(rz - z);

        // Adjust to ensure x + y + z = 0
        if (x_diff > y_diff && x_diff > z_diff)
        {
            rx = -ry - rz;
        }
        else if (y_diff > z_diff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new Vector3Int(rx, ry, rz);
    }

    /// <summary>
    /// Adds two cube coordinates together.
    /// </summary>
    public static Vector3Int CubeAdd(Vector3Int a, Vector3Int b)
    {
        return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    /// <summary>
    /// Scales a cube coordinate by a factor.
    /// </summary>
    public static Vector3Int CubeScale(Vector3Int cube, int factor)
    {
        return new Vector3Int(cube.x * factor, cube.y * factor, cube.z * factor);
    }

    /// <summary>
    /// Returns a cube coordinate representing a direction.
    /// </summary>
    public static Vector3Int CubeDirection(int direction)
    {
        // The six directions for cube coordinates
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, -1, 0),  // East
            new Vector3Int(1, 0, -1),  // Northeast
            new Vector3Int(0, 1, -1),  // Northwest
            new Vector3Int(-1, 1, 0),  // West
            new Vector3Int(-1, 0, 1),  // Southwest
            new Vector3Int(0, -1, 1)   // Southeast
        };
        return directions[direction % 6];
    }

    public static Vector3 CubeToWorld(Vector3Int cube, HexOrientation orientation, float size)
    {
        float x, y;
        if (orientation == HexOrientation.PointyTop)
        {
            x = size * HEX_VERTICAL_SPACING * (cube.x + cube.z / 2f);
            y = size * HEX_HORIZONTAL_SPACING * cube.z;
        }
        else // FlatTop
        {
            x = size * HEX_HORIZONTAL_SPACING * cube.x;
            y = size * HEX_VERTICAL_SPACING * (cube.z + cube.x / 2f);
        }
        return new Vector3(x, 0, y);
    }

    /// <summary>
    /// Finds the neighbor of a hex in a given direction.
    /// </summary>
    public static Vector3Int CubeNeighbor(Vector3Int hex, int direction)
    {
        return CubeAdd(hex, CubeDirection(direction));
    }

    /// <summary>
    /// Generates a ring of hexes around a center hex.
    /// </summary>
    public static List<Vector3Int> CubeRing(Vector3Int center, int radius)
    {
        List<Vector3Int> results = new List<Vector3Int>();

        if (radius == 0)
        {
            results.Add(center); // Special case: radius 0 is just the center hex
            return results;
        }

        // Start from a known hex at the edge of the ring
        Vector3Int hex = CubeAdd(center, CubeScale(CubeDirection(4), radius));

        // Walk around the ring
        for (int i = 0; i < 6; i++) // 6 directions around the hex
        {
            for (int j = 0; j < radius; j++) // Move forward by radius steps
            {
                results.Add(hex);
                hex = CubeNeighbor(hex, i); // Move to the next neighbor in the current direction
            }
        }

        return results;
    }

    public static Vector2Int CubeToAxial(Vector3Int cube)
    {
        return new Vector2Int(cube.x, cube.z);
    }

    #endregion

    #region Draw Hexes

    /// <summary>
    /// Calculates the six vertex positions of a hexagon in world space, 
    /// based on its center position, size, and orientation.
    /// </summary>
    /// <param name="center">The world-space position of the hex center.</param>
    /// <param name="size">The radius of the hexagon.</param>
    /// <param name="orientation">The hex grid orientation (FlatTop or PointyTop).</param>
    /// <returns>An array of six Vector3 positions representing the hexagon's vertices.</returns>
    public static Vector3[] GetHexVertices(Vector3 center, float size, HexOrientation orientation)
    {
        float baseOffset = (orientation == HexOrientation.FlatTop) ? 0f : 90f;
        float angleOffset = baseOffset + 180f;

        Vector3[] vertices = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i + angleOffset;
            float angleRad = Mathf.Deg2Rad * angleDeg;
            vertices[i] = new Vector3(
                center.x + size * Mathf.Cos(angleRad),
                center.y,
                center.z + size * Mathf.Sin(angleRad)
            );
        }

        return vertices;
    }

    /// <summary>
    /// Draws a hexagon outline using Gizmos, based on its center position,
    /// size, and orientation.
    /// </summary>
    /// <param name="center">The world-space position of the hex center.</param>
    /// <param name="size">The radius of the hexagon.</param>
    /// <param name="orientation">The hex grid orientation (FlatTop or PointyTop).</param>
    public static void DrawHexagon(Vector3 center, float size, HexOrientation orientation)
    {
        Vector3[] vertices = HexUtilities.GetHexVertices(center, size, orientation);
        for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(vertices[i], vertices[(i + 1) % 6]);
        }
    }

    /// <summary>
    /// Draws a hexagon in the SceneView using Handles.
    /// </summary>
    public static void DrawHexagonHandles(Vector3 center, float size, HexOrientation orientation, Color color)
    {
        Handles.color = color; // Set the color for Handles

        Vector3[] vertices = GetHexVertices(center, size, orientation);
        for (int i = 0; i < 6; i++)
        {
            Handles.DrawLine(vertices[i], vertices[(i + 1) % 6]);
        }
    }
    /// <summary>
    /// Draws a filled hexagon using Unity's Handles system in the Scene View.
    /// This is useful for highlighting hexes with a transparent or solid color.
    /// </summary>
    /// <param name="center">The center position of the hex in world space.</param>
    /// <param name="size">The size (radius) of the hex.</param>
    /// <param name="orientation">The hex grid orientation (FlatTop or PointyTop).</param>
    /// <param name="color">The color to fill the hex with.</param>
    public static void DrawFilledHexagonHandles(Vector3 center, float size, HexOrientation orientation, Color color)
    {
        Vector3[] vertices = GetHexVertices(center, size, orientation);

        Handles.color = color;
        Handles.DrawAAConvexPolygon(vertices);
    }

    /// <summary>
    /// Draws the outer edges of a collection of hexes, outlining only those edges
    /// that do not have an adjacent hex in the collection. This is useful for 
    /// highlighting the boundary of a hex region or a chunk.
    /// </summary>
    /// <param name="hexCoordsCollection">A collection of axial hex coordinates representing the region.</param>
    /// <param name="hexSize">The size of each hex.</param>
    /// <param name="orientation">The hex grid orientation (FlatTop or PointyTop).</param>
    public static void DrawCollectionOuterEdges(IEnumerable<Vector2Int> hexCoordsCollection, float hexSize, HexOrientation orientation)
    {
        HashSet<Vector2Int> hexSet = new HashSet<Vector2Int>(hexCoordsCollection);
        Vector2Int[] neighborOffsets = HexUtilities.GetNeighborOffsets(orientation);
        List<(Vector3, Vector3)> perimeterEdges = new List<(Vector3, Vector3)>();

        foreach (var axial in hexSet)
        {
            Vector3 hexCenter = HexUtilities.AxialToWorld(axial.x, axial.y, orientation, hexSize);
            Vector3[] hexVertices = HexUtilities.GetHexVertices(hexCenter, hexSize, orientation);

            for (int dirIndex = 0; dirIndex < neighborOffsets.Length; dirIndex++)
            {
                Vector2Int neighbor = new Vector2Int(axial.x + neighborOffsets[dirIndex].x, axial.y + neighborOffsets[dirIndex].y);
                if (!hexSet.Contains(neighbor))
                {
                    Vector3 start = hexVertices[dirIndex];
                    Vector3 end = hexVertices[(dirIndex + 1) % 6];
                    perimeterEdges.Add((start, end));
                }
            }
        }

        Gizmos.color = Color.white;
        foreach (var edge in perimeterEdges)
        {
            Gizmos.DrawLine(edge.Item1, edge.Item2);
        }
    }

    /// <summary>
    /// Draws a label at the given hex position with its axial coordinates.
    /// Useful for debugging and visualizing hex positions in the Scene View.
    /// </summary>
    /// <param name="hexPos">The world-space position of the hex.</param>
    /// <param name="q">The axial Q coordinate of the hex.</param>
    /// <param name="r">The axial R coordinate of the hex.</param>
    public static void DrawHexLabel(Vector3 hexPos, int q, int r)
    {
        UnityEditor.Handles.color = Color.white; // or any color you prefer
        UnityEditor.Handles.Label(hexPos + Vector3.up * 0.5f, $"({q},{r})");
    }

    #endregion

    #region Neighboring Hexes

    // Get neighbor coordinates given an axial position and direction for either orientation
    public static Vector2Int GetNeighborAxialCoords(int q, int r, Vector2Int direction)
    {
        return new Vector2Int(q + direction.x, r + direction.y);
    }

    // Returns the correct neighbor offsets based on hex orientation
    public static Vector2Int[] GetNeighborOffsets(HexOrientation orientation)
    {
        if (orientation == HexOrientation.FlatTop)
        {
            // Flat-top neighbors in a hex grid (pointy sides face up/down)
            return new Vector2Int[]
            {
                new Vector2Int(1, 0),    // Right
                new Vector2Int(0, 1),    // Top-right
                new Vector2Int(-1, 1),   // Top-left
                new Vector2Int(-1, 0),   // Left
                new Vector2Int(0, -1),   // Bottom-left
                new Vector2Int(1, -1)    // Bottom-right
            };
        }
        else // PointyTop orientation
        {
            // Pointy-top neighbors in a hex grid (flat sides face up/down)
            return new Vector2Int[]
            {
                new Vector2Int(1, -1),   // Top-right
                new Vector2Int(1, 0),    // Right
                new Vector2Int(0, 1),    // Bottom-right
                new Vector2Int(-1, 1),   // Bottom-left
                new Vector2Int(-1, 0),   // Left
                new Vector2Int(0, -1)    // Top-left
            };
        }
    }

    #endregion

    /// <summary>
    /// Checks if the axial hex (q, r) is exactly on the ring of the given radius.
    /// </summary>
    public static bool IsBorderHex(int q, int r, int radius)
    {
        // Convert to cube coordinate: x = q, z = r, y = -x - z
        int s = -q - r;

        // Distance in cube coords = max(|x|, |y|, |z|)
        int maxDistance = Mathf.Max(Mathf.Abs(q), Mathf.Abs(r), Mathf.Abs(s));

        // If it's exactly radius, it's on the outer ring
        return maxDistance == radius;
    }    

}
