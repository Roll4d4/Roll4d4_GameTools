using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides utility methods for working with chunks in a hexagonal grid system.
/// </summary>
public static class ChunkUtilities
{
    #region Chunk Coordinate Conversions

    /// <summary>
    /// Converts chunk axial coordinates (chunkQ, chunkR) to a world position.
    /// </summary>
    public static Vector3 ChunkToWorld(int chunkQ, int chunkR, GridConfig config)
    {
        return HexUtilities.AxialToWorld(
            chunkQ,
            chunkR,
            config.overlayGridOrientation,
            config.ChunkSize
        );
    }

    /// <summary>
    /// Converts a world position to chunk axial coordinates.
    /// </summary>
    public static Vector2Int WorldToChunk(Vector3 worldPos, GridConfig config)
    {
        return HexUtilities.WorldToAxial(
            worldPos,
            config.overlayGridOrientation,
            config.ChunkSize
        );
    }

    /// <summary>
    /// Rounds an axial coordinate to the nearest chunk coordinate based on the chunk radius.
    /// </summary>
    public static Vector2Int RoundToChunk(Vector2Int axial, int chunkRadius)
    {
        int chunkQ = Mathf.RoundToInt((float)axial.x / chunkRadius);
        int chunkR = Mathf.RoundToInt((float)axial.y / chunkRadius);
        return new Vector2Int(chunkQ, chunkR);
    }

    #endregion

    #region Chunk Hex Retrieval

    /// <summary>
    /// Returns a list of all hexes (in world space) within a given chunk.
    /// </summary>
    public static List<Vector3> GetChunkHexesInWorldSpace(int chunkQ, int chunkR, GridConfig config)
    {
        List<Vector3> hexCenters = new List<Vector3>();

        // Get the world position of the chunk center
        Vector3 chunkPos = ChunkToWorld(chunkQ, chunkR, config);
        int hexRadius = config.chunkRadius;

        // Iterate over the hexes inside this chunk
        for (int q = -hexRadius; q <= hexRadius; q++)
        {
            int rMin = Mathf.Max(-hexRadius, -q - hexRadius);
            int rMax = Mathf.Min(hexRadius, -q + hexRadius);

            for (int r = rMin; r <= rMax; r++)
            {
                Vector3 localHexPos = HexUtilities.AxialToWorld(
                    q,
                    r,
                    config.baseGridOrientation,
                    config.hexSize
                );

                hexCenters.Add(chunkPos + localHexPos);
            }
        }

        return hexCenters;
    }

    /// <summary>
    /// Returns all hex offsets for a chunk of a given radius.
    /// </summary>
    public static List<Vector2Int> GetChunkHexes(int chunkRadius)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();

        for (int q = -chunkRadius; q <= chunkRadius; q++)
        {
            for (int r = Mathf.Max(-chunkRadius, -q - chunkRadius); r <= Mathf.Min(chunkRadius, -q + chunkRadius); r++)
            {
                offsets.Add(new Vector2Int(q, r));
            }
        }

        return offsets;
    }

    #endregion

    #region Chunk Spatial Queries

    /// <summary>
    /// Returns a list of chunk coordinates (axial) within a specified world-space radius.
    /// </summary>
    public static List<Vector2Int> GetChunksWithinRadius(Vector3 centerPosition, float worldRadius, GridConfig config)
    {
        List<Vector2Int> chunks = new List<Vector2Int>();

        // Convert center world position to chunk coordinates
        Vector2Int centerChunk = WorldToChunk(centerPosition, config);

        // Convert world radius to chunk space
        int chunkRadius = Mathf.CeilToInt(worldRadius / config.ChunkSize);

        // Iterate over potential chunks in the radius
        for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
        {
            for (int dy = Mathf.Max(-chunkRadius, -dx - chunkRadius); dy <= Mathf.Min(chunkRadius, -dx + chunkRadius); dy++)
            {
                int dz = -dx - dy;

                // Check if within chunk radius
                if (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz) <= chunkRadius * 2)
                {
                    chunks.Add(new Vector2Int(centerChunk.x + dx, centerChunk.y + dy));
                }
            }
        }

        return chunks;
    }

    /// <summary>
    /// Determines if a given chunk (chunkQ, chunkR) is within the valid range (-10 to 10).
    /// </summary>
    public static bool IsChunkInRange(int chunkQ, int chunkR)
    {
        if (chunkQ < -10 || chunkQ > 10)
            return false;

        int rMin = Mathf.Max(-10, -chunkQ - 10);
        int rMax = Mathf.Min(10, -chunkQ + 10);

        return (chunkR >= rMin && chunkR <= rMax);
    }

    /// <summary>
    /// Calculates the distance between two chunks in chunk space using hex distance.
    /// </summary>
    public static int ChunkDistance(Vector2Int chunkA, Vector2Int chunkB)
    {
        return HexUtilities.HexDistance(chunkA, chunkB);
    }

    #endregion
}

