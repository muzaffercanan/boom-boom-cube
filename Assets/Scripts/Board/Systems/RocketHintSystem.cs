using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// System that displays rocket hints on cube groups that are eligible to create rockets (size >= 4).
/// Shows visual indicators to help players identify rocket-creation opportunities.
/// </summary>
public class RocketHintSystem
{
    private readonly GridSystem _gridSystem;
    private readonly MatchSystem _matchSystem;
    private readonly int _minRocketSize;
    
    // Track which cubes currently have hints displayed
    private HashSet<IBoardItem> _currentHintedItems = new HashSet<IBoardItem>();
    
    public RocketHintSystem(GridSystem gridSystem, MatchSystem matchSystem, int minRocketSize)
    {
        _gridSystem = gridSystem;
        _matchSystem = matchSystem;
        _minRocketSize = minRocketSize;
    }
    
    /// <summary>
    /// Scans the entire grid and updates rocket hints for all eligible groups.
    /// Call this after any grid change (blast, gravity, spawn).
    /// </summary>
    public void UpdateHints()
    {
        // Clear all existing hints
        ClearAllHints();
        
        // Track which items we've already processed to avoid duplicate checks
        HashSet<IBoardItem> processedItems = new HashSet<IBoardItem>();
        
        // Scan entire grid
        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                var item = _gridSystem.GetItem(x, y);
                
                // Skip if not a matchable cube or already processed
                if (item == null || !(item is IMatchable) || processedItems.Contains(item))
                    continue;
                
                // Find all matches for this cube
                var matches = _matchSystem.FindMatches(x, y);
                
                // If group is large enough for rocket, show hints
                if (matches.Count >= _minRocketSize)
                {
                    foreach (var matchedItem in matches)
                    {
                        ShowHintOn(matchedItem);
                        processedItems.Add(matchedItem);
                    }
                }
                else
                {
                    // Mark as processed even if not eligible
                    foreach (var matchedItem in matches)
                    {
                        processedItems.Add(matchedItem);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Shows a rocket hint overlay on the specified item.
    /// </summary>
    private void ShowHintOn(IBoardItem item)
    {
        if (!(item is CubeItem cube)) return;
        
        var gameObject = cube.GetGameObject();
        if (gameObject == null) return;
        
        // Check if hint already exists
        var existingHint = gameObject.transform.Find("RocketHint");
        if (existingHint != null)
        {
            existingHint.gameObject.SetActive(true);
            _currentHintedItems.Add(item);
            return;
        }
        
        // Create hint overlay
        GameObject hintObject = new GameObject("RocketHint");
        hintObject.transform.SetParent(gameObject.transform);
        hintObject.transform.localPosition = Vector3.zero;
        hintObject.transform.localScale = Vector3.one;
        
        // Add sprite renderer
        var spriteRenderer = hintObject.AddComponent<SpriteRenderer>();
        
        // Load rocket hint sprite directly from project folder (No Resources)
        Sprite hintSprite = null;
#if UNITY_EDITOR
        string spriteName = GetRocketHintSpriteName(cube.GetColor());
        string assetPath = $"Assets/Cubes/RocketState/{spriteName}.png";
        hintSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif
        
        if (hintSprite != null)
        {
            spriteRenderer.sprite = hintSprite;
            spriteRenderer.sortingOrder = 1; // Render on top of cube
        }
        else
        {
            // Fallback: use a simple overlay
            Debug.LogWarning($"[RocketHintSystem] Hint sprite not found at path for color: {cube.GetColor()}");
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent white
        }
        
        _currentHintedItems.Add(item);
    }
    
    /// <summary>
    /// Gets the sprite name for rocket hint based on cube color.
    /// </summary>
    private string GetRocketHintSpriteName(CubeColor color)
    {
        switch (color)
        {
            case CubeColor.Red: return "red_rocket";
            case CubeColor.Green: return "green_rocket";
            case CubeColor.Blue: return "blue_rocket";
            case CubeColor.Yellow: return "yellow_rocket";
            default: return "red_rocket";
        }
    }

    /// <summary>
    /// Clears all rocket hints from the grid.
    /// </summary>
    private void ClearAllHints()
    {
        foreach (var item in _currentHintedItems)
        {
            // Check if C# reference is null OR Unity Object is destroyed (magic null check)
            if (item == null || (item is Object unityObj && unityObj == null)) 
                continue;
            
            try
            {
                var gameObject = item.GetGameObject();
                if (gameObject == null) continue;
                
                var hintTransform = gameObject.transform.Find("RocketHint");
                if (hintTransform != null)
                {
                    hintTransform.gameObject.SetActive(false);
                }
            }
            catch (System.Exception) 
            {
                // Item might be in the process of being destroyed
                continue;
            }
        }
        
        _currentHintedItems.Clear();
    }
    
    /// <summary>
    /// Completely removes all hint objects (call on level end).
    /// </summary>
    public void DestroyAllHints()
    {
        foreach (var item in _currentHintedItems)
        {
            if (item == null || (item is Object unityObj && unityObj == null)) 
                continue;
            
            try
            {
                var gameObject = item.GetGameObject();
                if (gameObject == null) continue;
                
                var hintTransform = gameObject.transform.Find("RocketHint");
                if (hintTransform != null)
                {
                    Object.Destroy(hintTransform.gameObject);
                }
            }
            catch (System.Exception) 
            {
                continue; 
            }
        }
        
        _currentHintedItems.Clear();
    }
}
