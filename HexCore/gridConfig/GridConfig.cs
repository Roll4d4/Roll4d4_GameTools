using UnityEngine;

/// <summary>
/// ScriptableObject that stores configuration settings for a hex grid.
/// This includes hex size, chunk radius, and grid orientations.
/// The chunk size is computed dynamically based on hex size and chunk radius.
/// </summary>


[CreateAssetMenu(fileName = "GridConfig", menuName = "Hex/GridConfig")]
public class GridConfig : ScriptableObject
{
    [Header("Hex Grid Dimensions")]
    [Tooltip("Size of each individual hex cell.")]
    public float hexSize = 5f;

    [Tooltip("Radius of each chunk in hex cells.")]
    public int chunkRadius = 5;

    [Header("Hex Orientations")]
    public HexOrientation overlayGridOrientation = HexOrientation.FlatTop;
    public HexOrientation baseGridOrientation = HexOrientation.PointyTop;

    /// <summary>
    /// Computed chunk size = hexSize * chunkRadius * ~1.732f
    /// (assuming chunk is a large hex shape of radius 'chunkRadius')
    /// </summary>
    public float ChunkSize => hexSize * (chunkRadius * 1.732f);
}
