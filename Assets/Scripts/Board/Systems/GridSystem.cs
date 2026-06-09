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
public class GridSystem
{
    private int _width;
    private int _height;
    private IBoardItem[,] _grid;

    public int Width => _width;
    public int Height => _height;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _grid = new IBoardItem[width, height];
    }

    public IBoardItem GetItem(int x, int y)
    {
        if (!IsValid(x, y)) return null;
        return _grid[x, y];
    }

    public void SetItem(int x, int y, IBoardItem item)
    {
        if (!IsValid(x, y)) return;
        _grid[x, y] = item;
        if (item != null)
        {
            item.SetPosition(x, y);
        }
    }

    public bool IsValid(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    public void ClearCell(int x, int y)
    {
        if (IsValid(x, y)) _grid[x, y] = null;
    }

    public void DestroyItem(int x, int y)
    {
        if (!IsValid(x, y)) return;
        var item = _grid[x, y];
        if (item != null)
        {
            Object.Destroy(item.GetGameObject());
            _grid[x, y] = null;
        }
    }
}
}
