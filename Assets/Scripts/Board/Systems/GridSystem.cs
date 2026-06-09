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
    private BoardCellState[,] _cells;

    public int Width => _width;
    public int Height => _height;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _grid = new IBoardItem[width, height];
        _cells = new BoardCellState[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _cells[x, y] = BoardCellState.Normal;
            }
        }
    }

    public void Initialize(int width, int height, BoardCellState[,] cells)
    {
        Initialize(width, height);
        if (cells == null) return;

        int copyWidth = Mathf.Min(width, cells.GetLength(0));
        int copyHeight = Mathf.Min(height, cells.GetLength(1));
        for (int x = 0; x < copyWidth; x++)
        {
            for (int y = 0; y < copyHeight; y++)
            {
                _cells[x, y] = cells[x, y];
            }
        }
    }

    public IBoardItem GetItem(int x, int y)
    {
        if (!CanHoldItem(x, y)) return null;
        return _grid[x, y];
    }

    public void SetItem(int x, int y, IBoardItem item)
    {
        if (!IsInBounds(x, y)) return;
        if (item != null && !CanHoldItem(x, y)) return;
        _grid[x, y] = item;
        if (item != null)
        {
            item.SetPosition(x, y);
        }
    }

    public bool IsValid(int x, int y)
    {
        return IsInBounds(x, y) && IsPlayableCell(x, y);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    public BoardCellState GetCellState(int x, int y)
    {
        if (!IsInBounds(x, y) || _cells == null)
        {
            return BoardCellState.Hole;
        }

        return _cells[x, y];
    }

    public bool CellExists(int x, int y)
    {
        return IsInBounds(x, y) && GetCellState(x, y).Exists;
    }

    public bool IsPlayableCell(int x, int y)
    {
        BoardCellState state = GetCellState(x, y);
        return state.Exists && state.Playable;
    }

    public bool CanHoldItem(int x, int y)
    {
        BoardCellState state = GetCellState(x, y);
        return state.Exists && state.CanHoldItem;
    }

    public bool CanSpawnItem(int x, int y)
    {
        BoardCellState state = GetCellState(x, y);
        return state.Exists && state.CanHoldItem && state.CanSpawnItem;
    }

    public bool BlocksFall(int x, int y)
    {
        return !IsInBounds(x, y) || GetCellState(x, y).BlocksFall;
    }

    public bool BlocksRocket(int x, int y)
    {
        return !IsInBounds(x, y) || GetCellState(x, y).BlocksRocket;
    }

    public void ClearCell(int x, int y)
    {
        if (IsInBounds(x, y)) _grid[x, y] = null;
    }

    public void DestroyItem(int x, int y)
    {
        if (!IsInBounds(x, y)) return;
        var item = _grid[x, y];
        if (item != null)
        {
            Object.Destroy(item.GetGameObject());
            _grid[x, y] = null;
        }
    }
}
}
