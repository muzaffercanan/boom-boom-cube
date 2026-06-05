using System.Collections.Generic;

public sealed class NoMoveScanner
{
    private readonly GridSystem _gridSystem;
    private readonly int _minGroupSize;

    public NoMoveScanner(GridSystem gridSystem, int minGroupSize)
    {
        _gridSystem = gridSystem;
        _minGroupSize = minGroupSize;
    }

    public bool HasPlayableMove()
    {
        return TryFindPlayableMove(out _);
    }

    public bool TryFindPlayableMove(out PlayableMove move)
    {
        move = default;

        if (_gridSystem == null || _gridSystem.Width <= 0 || _gridSystem.Height <= 0)
        {
            return false;
        }

        bool[,] visited = new bool[_gridSystem.Width, _gridSystem.Height];

        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                IBoardItem item = _gridSystem.GetItem(x, y);
                if (item == null)
                {
                    visited[x, y] = true;
                    continue;
                }

                if (IsPlayableBooster(item))
                {
                    move = new PlayableMove(x, y, 1, true);
                    return true;
                }

                if (visited[x, y] || !CanScanAsMatchable(item))
                {
                    continue;
                }

                int groupSize = CountConnectedGroup(x, y, ((IMatchable)item).GetColor(), visited);
                if (groupSize >= _minGroupSize)
                {
                    move = new PlayableMove(x, y, groupSize, false);
                    return true;
                }
            }
        }

        return false;
    }

    private int CountConnectedGroup(int startX, int startY, CubeColor color, bool[,] visited)
    {
        int count = 0;
        Queue<GridPosition> pending = new Queue<GridPosition>();
        pending.Enqueue(new GridPosition(startX, startY));

        while (pending.Count > 0)
        {
            GridPosition position = pending.Dequeue();
            if (!_gridSystem.IsValid(position.X, position.Y)) continue;
            if (visited[position.X, position.Y]) continue;

            IBoardItem item = _gridSystem.GetItem(position.X, position.Y);
            if (!CanScanAsMatchable(item) || ((IMatchable)item).GetColor() != color)
            {
                continue;
            }

            visited[position.X, position.Y] = true;
            count++;

            pending.Enqueue(new GridPosition(position.X + 1, position.Y));
            pending.Enqueue(new GridPosition(position.X - 1, position.Y));
            pending.Enqueue(new GridPosition(position.X, position.Y + 1));
            pending.Enqueue(new GridPosition(position.X, position.Y - 1));
        }

        return count;
    }

    private static bool IsPlayableBooster(IBoardItem item)
    {
        return item != null && item.GetItemType() == ItemType.Rocket;
    }

    private static bool CanScanAsMatchable(IBoardItem item)
    {
        return item is IMatchable matchable && matchable.CanMatch();
    }

    private readonly struct GridPosition
    {
        public int X { get; }
        public int Y { get; }

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}

public readonly struct PlayableMove
{
    public int X { get; }
    public int Y { get; }
    public int GroupSize { get; }
    public bool IsBooster { get; }

    public PlayableMove(int x, int y, int groupSize, bool isBooster)
    {
        X = x;
        Y = y;
        GroupSize = groupSize;
        IsBooster = isBooster;
    }
}
