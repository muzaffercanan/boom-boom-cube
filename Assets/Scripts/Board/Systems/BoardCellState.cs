using System;
using DreamGames.Data;

namespace DreamGames.Board.Systems
{
public readonly struct BoardCellState
{
    public static readonly BoardCellState Normal = new BoardCellState(
        exists: true,
        playable: true,
        canHoldItem: true,
        canSpawnItem: true,
        blocksFall: false,
        blocksRocket: false,
        locked: false,
        cellType: BoardCellType.Normal);

    public static readonly BoardCellState Hole = new BoardCellState(
        exists: false,
        playable: false,
        canHoldItem: false,
        canSpawnItem: false,
        blocksFall: true,
        blocksRocket: true,
        locked: false,
        cellType: BoardCellType.Hole);

    public static readonly BoardCellState Blocked = new BoardCellState(
        exists: true,
        playable: false,
        canHoldItem: false,
        canSpawnItem: false,
        blocksFall: true,
        blocksRocket: true,
        locked: false,
        cellType: BoardCellType.Blocked);

    public bool Exists { get; }
    public bool Playable { get; }
    public bool CanHoldItem { get; }
    public bool CanSpawnItem { get; }
    public bool BlocksFall { get; }
    public bool BlocksRocket { get; }
    public bool Locked { get; }
    public BoardCellType CellType { get; }

    public BoardCellState(
        bool exists,
        bool playable,
        bool canHoldItem,
        bool canSpawnItem,
        bool blocksFall,
        bool blocksRocket,
        bool locked,
        BoardCellType cellType)
    {
        Exists = exists;
        Playable = playable;
        CanHoldItem = canHoldItem;
        CanSpawnItem = canSpawnItem;
        BlocksFall = blocksFall;
        BlocksRocket = blocksRocket;
        Locked = locked;
        CellType = cellType;
    }

    public static BoardCellState FromLevelCell(LevelCellData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.cell_type))
        {
            return Normal;
        }

        switch (data.cell_type.Trim().ToLowerInvariant())
        {
            case "normal":
            case "cell":
            case "playable":
                return data.locked ? LockedNormal() : Normal;
            case "hole":
            case "none":
            case "no_cell":
            case "no-cell":
                return Hole;
            case "blocked":
            case "blocker":
                return Blocked;
            case "locked":
                return LockedNormal();
            default:
                return Normal;
        }
    }

    public static bool IsKnownCellType(string cellType)
    {
        if (string.IsNullOrWhiteSpace(cellType))
        {
            return true;
        }

        switch (cellType.Trim().ToLowerInvariant())
        {
            case "normal":
            case "cell":
            case "playable":
            case "hole":
            case "none":
            case "no_cell":
            case "no-cell":
            case "blocked":
            case "blocker":
            case "locked":
                return true;
            default:
                return false;
        }
    }

    private static BoardCellState LockedNormal()
    {
        return new BoardCellState(
            exists: true,
            playable: true,
            canHoldItem: true,
            canSpawnItem: false,
            blocksFall: false,
            blocksRocket: false,
            locked: true,
            cellType: BoardCellType.Locked);
    }
}

public enum BoardCellType
{
    Normal,
    Hole,
    Blocked,
    Locked
}
}
