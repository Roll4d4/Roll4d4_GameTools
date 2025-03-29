using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GridGuide serves as a centralized reference for grid settings and configurations. 
/// It pulls values from a GridConfig ScriptableObject and provides static access to 
/// essential properties such as hex size, chunk size, and orientations.
/// </summary>
public class GridGuide : MonoBehaviour
{
    
    [Tooltip("Reference to the GridConfig ScriptableObject that defines hex and chunk settings.")]
    public static GridConfig gridConfig { get; private set; }
    public GridConfig instanceGridConfig;

    [Tooltip("Optional reference to the GridVisualizer for drawing in the editor.")]
    public GridVisualizer gridVisualizer;

    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Ensures the static gridConfig reference is updated.
    /// </summary>
    private void OnValidate()
    {
        if (instanceGridConfig == null)
        {
            Debug.LogWarning($"{nameof(GridGuide)}: No GridConfig assigned!", this);
            return;
        }

        // Assign the static reference
        gridConfig = instanceGridConfig;
    }

    /// <summary>
    /// Ensures static gridConfig is set when the game starts.
    /// </summary>
    private void Awake()
    {
        if (gridConfig == null)
        {
            gridConfig = instanceGridConfig;
        }
    }
}
