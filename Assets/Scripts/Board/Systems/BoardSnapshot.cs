using System;
using System.Text;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Systems
{
public sealed class BoardSnapshot
{
    private readonly CellSnapshot[,] _cells;

    public int Width { get; }
    public int Height { get; }

    private BoardSnapshot(int width, int height, CellSnapshot[,] cells)
    {
        Width = width;
        Height = height;
        _cells = cells;
    }

    public static BoardSnapshot FromGrid(GridSystem gridSystem)
    {
        if (gridSystem == null)
        {
            throw new ArgumentNullException(nameof(gridSystem));
        }

        CellSnapshot[,] cells = new CellSnapshot[gridSystem.Width, gridSystem.Height];

        for (int x = 0; x < gridSystem.Width; x++)
        {
            for (int y = 0; y < gridSystem.Height; y++)
            {
                cells[x, y] = CellSnapshot.FromItem(x, y, gridSystem.GetItem(x, y));
            }
        }

        return new BoardSnapshot(gridSystem.Width, gridSystem.Height, cells);
    }

    public CellSnapshot GetCell(int column, int row)
    {
        if (column < 0 || column >= Width || row < 0 || row >= Height)
        {
            throw new ArgumentOutOfRangeException();
        }

        return _cells[column, row];
    }

    public string ToDebugString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"width\":").Append(Width)
            .Append(",\"height\":").Append(Height)
            .Append(",\"cells\":[");

        bool firstCell = true;
        for (int row = 0; row < Height; row++)
        {
            for (int column = 0; column < Width; column++)
            {
                if (!firstCell)
                {
                    builder.Append(',');
                }

                builder.Append(_cells[column, row].ToDebugString());
                firstCell = false;
            }
        }

        builder.Append("]}");
        return builder.ToString();
    }

    public override string ToString()
    {
        return ToDebugString();
    }
}

public readonly struct CellSnapshot
{
    public int Row { get; }
    public int Column { get; }
    public bool IsEmpty { get; }
    public ItemType ItemType { get; }
    public bool HasColor { get; }
    public CubeColor Color { get; }
    public bool IsObstacle { get; }
    public bool IsBooster { get; }
    public int Health { get; }

    private CellSnapshot(
        int column,
        int row,
        bool isEmpty,
        ItemType itemType,
        bool hasColor,
        CubeColor color,
        bool isObstacle,
        bool isBooster,
        int health)
    {
        Column = column;
        Row = row;
        IsEmpty = isEmpty;
        ItemType = itemType;
        HasColor = hasColor;
        Color = color;
        IsObstacle = isObstacle;
        IsBooster = isBooster;
        Health = health;
    }

    public static CellSnapshot FromItem(int column, int row, IBoardItem item)
    {
        if (item == null)
        {
            return new CellSnapshot(column, row, true, ItemType.None, false, default, false, false, 0);
        }

        bool hasColor = item is IMatchable;
        CubeColor color = hasColor ? ((IMatchable)item).GetColor() : default;
        ItemType itemType = item.GetItemType();
        int health = item is IDamageable damageable ? damageable.Health : 0;

        return new CellSnapshot(
            column,
            row,
            false,
            itemType,
            hasColor,
            color,
            itemType == ItemType.Obstacle || item is IDamageable,
            itemType == ItemType.Rocket,
            health);
    }

    public string ToDebugString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"row\":").Append(Row)
            .Append(",\"column\":").Append(Column)
            .Append(",\"empty\":").Append(IsEmpty ? "true" : "false")
            .Append(",\"itemType\":\"").Append(ItemType).Append('"')
            .Append(",\"hasColor\":").Append(HasColor ? "true" : "false");

        if (HasColor)
        {
            builder.Append(",\"color\":\"").Append(Color).Append('"');
        }

        builder.Append(",\"obstacle\":").Append(IsObstacle ? "true" : "false")
            .Append(",\"booster\":").Append(IsBooster ? "true" : "false")
            .Append(",\"health\":").Append(Health)
            .Append('}');

        return builder.ToString();
    }
}
}
