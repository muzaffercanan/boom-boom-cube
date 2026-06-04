using System.Collections.Generic;
using UnityEngine;


public class RocketHintSystem
{
    private const string HintSpriteResourceRoot = "Cubes/RocketState";

    private readonly GridSystem _gridSystem;
    private readonly MatchSystem _matchSystem;
    private readonly int _minRocketSize;
    

    private HashSet<IBoardItem> _currentHintedItems = new HashSet<IBoardItem>();
    
    public RocketHintSystem(GridSystem gridSystem, MatchSystem matchSystem, int minRocketSize)
    {
        _gridSystem = gridSystem;
        _matchSystem = matchSystem;
        _minRocketSize = minRocketSize;
    }
    

    public void UpdateHints()
    {

        ClearAllHints();
        

        HashSet<IBoardItem> processedItems = new HashSet<IBoardItem>();
        

        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                var item = _gridSystem.GetItem(x, y);
                
        if (item == null || !(item is IMatchable) || processedItems.Contains(item))
                    continue;
                
        var matches = _matchSystem.FindMatches(x, y);
                

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

                    foreach (var matchedItem in matches)
                    {
                        processedItems.Add(matchedItem);
                    }
                }
            }
        }
    }
    

    private void ShowHintOn(IBoardItem item)
    {
        if (!(item is CubeItem cube)) return;
        
        var gameObject = cube.GetGameObject();
        if (gameObject == null) return;
        

        var existingHint = gameObject.transform.Find("RocketHint");
        if (existingHint != null)
        {
            existingHint.gameObject.SetActive(true);
            _currentHintedItems.Add(item);
            return;
        }
        

        GameObject hintObject = new GameObject("RocketHint");
        hintObject.transform.SetParent(gameObject.transform);
        hintObject.transform.localPosition = Vector3.zero;
        hintObject.transform.localScale = Vector3.one;
        

        var spriteRenderer = hintObject.AddComponent<SpriteRenderer>();
        

        string spriteName = GetRocketHintSpriteName(cube.GetColor());
        Sprite hintSprite = Resources.Load<Sprite>($"{HintSpriteResourceRoot}/{spriteName}");
        
        if (hintSprite != null)
        {
            spriteRenderer.sprite = hintSprite;
            spriteRenderer.sortingOrder = 1;
        }
        else
        {

            Debug.LogWarning($"[RocketHintSystem] Hint sprite not found at path for color: {cube.GetColor()}");
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        }
        
        _currentHintedItems.Add(item);
    }
    

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


    private void ClearAllHints()
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
                    hintTransform.gameObject.SetActive(false);
                }
            }
            catch (System.Exception) 
            {

                continue;
            }
        }
        
        _currentHintedItems.Clear();
    }
    

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
