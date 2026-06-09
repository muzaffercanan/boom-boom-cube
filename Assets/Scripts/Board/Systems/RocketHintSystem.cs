using System.Collections.Generic;
using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Systems
{
public class RocketHintSystem
{
    private const string HintSpriteResourceRoot = "Cubes/RocketState";

    private readonly GridSystem _gridSystem;
    private readonly MatchSystem _matchSystem;
    private readonly int _minRocketSize;
    private readonly System.Func<CubeColor, Sprite> _hintSpriteResolver;
    

    private readonly HashSet<IBoardItem> _currentHintedItems = new HashSet<IBoardItem>();
    
    public RocketHintSystem(
        GridSystem gridSystem,
        MatchSystem matchSystem,
        int minRocketSize,
        System.Func<CubeColor, Sprite> hintSpriteResolver = null)
    {
        _gridSystem = gridSystem;
        _matchSystem = matchSystem;
        _minRocketSize = minRocketSize;
        _hintSpriteResolver = hintSpriteResolver ?? LoadHintSprite;
    }
    

    public void UpdateHints()
    {
        ClearAllHints();

        HashSet<IBoardItem> processedItems = new HashSet<IBoardItem>();

        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                IBoardItem item = _gridSystem.GetItem(x, y);
                if (item == null || !(item is IMatchable) || processedItems.Contains(item))
                {
                    continue;
                }

                List<IBoardItem> matches = _matchSystem.FindMatches(x, y);

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

        GameObject gameObject = cube.GetGameObject();
        if (gameObject == null) return;

        Sprite hintSprite = _hintSpriteResolver(cube.GetColor());
        if (hintSprite == null)
        {
            Debug.LogWarning($"[RocketHintSystem] Hint sprite not found at path for color: {cube.GetColor()}");
            return;
        }

        var existingHint = gameObject.transform.Find("RocketHint");
        if (existingHint != null)
        {
            SetBaseSpriteVisible(gameObject, false);
            existingHint.gameObject.SetActive(true);
            _currentHintedItems.Add(item);
            return;
        }

        GameObject hintObject = new GameObject("RocketHint");
        hintObject.transform.SetParent(gameObject.transform);
        hintObject.transform.localPosition = Vector3.zero;
        hintObject.transform.localScale = Vector3.one;

        SpriteRenderer baseRenderer = gameObject.GetComponent<SpriteRenderer>();
        var spriteRenderer = hintObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = hintSprite;
        spriteRenderer.color = Color.white;
        if (baseRenderer != null)
        {
            spriteRenderer.sortingLayerID = baseRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = baseRenderer.sortingOrder;
        }

        SetBaseSpriteVisible(gameObject, false);
        _currentHintedItems.Add(item);
    }
    

    private static string GetRocketHintSpriteName(CubeColor color)
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

    private static Sprite LoadHintSprite(CubeColor color)
    {
        string spriteName = GetRocketHintSpriteName(color);
        string path = $"{HintSpriteResourceRoot}/{spriteName}";
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
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

                SetBaseSpriteVisible(gameObject, true);
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

                SetBaseSpriteVisible(gameObject, true);
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

    private static void SetBaseSpriteVisible(GameObject gameObject, bool isVisible)
    {
        SpriteRenderer baseRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (baseRenderer != null)
        {
            baseRenderer.enabled = isVisible;
        }
    }
}

}
