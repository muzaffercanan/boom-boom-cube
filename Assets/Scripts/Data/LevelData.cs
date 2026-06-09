using System;
using System.Collections.Generic;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Data
{
[Serializable]
public class LevelData
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public List<string> grid;
    public List<LevelCellData> cells;

    public bool HasCellLayout => cells != null && cells.Count > 0;

    public string GetItemIdAt(int index)
    {
        if (HasCellLayout && index >= 0 && index < cells.Count)
        {
            LevelCellData cell = cells[index];
            if (cell != null && cell.item != null)
            {
                return cell.item;
            }
        }

        if (grid != null && index >= 0 && index < grid.Count)
        {
            return grid[index];
        }

        return null;
    }

    public IEnumerable<string> EnumerateItemIds()
    {
        int count = grid_width * grid_height;
        for (int i = 0; i < count; i++)
        {
            string itemId = GetItemIdAt(i);
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                yield return itemId;
            }
        }
    }
}

[Serializable]
public class LevelCellData
{
    public string cell_type;
    public string item;
    public bool locked;
}
}
