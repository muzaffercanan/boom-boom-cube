using System.Collections.Generic;

public class MatchSystem
{
    private GridSystem _gridSystem;

    public MatchSystem(GridSystem gridSystem)
    {
        _gridSystem = gridSystem;
    }

    public List<IBoardItem> FindMatches(int startX, int startY)
    {
        var startItem = _gridSystem.GetItem(startX, startY);
        if (startItem == null || !(startItem is IMatchable matchable)) 
            return new List<IBoardItem>();

        CubeColor targetColor = matchable.GetColor();
        List<IBoardItem> matches = new List<IBoardItem>();
        bool[,] visited = new bool[_gridSystem.Width, _gridSystem.Height];

        FindMatchesWithBfs(startX, startY, targetColor, visited, matches);

        return matches;
    }

    public List<IBoardItem> GetAdjacentObstacles(List<IBoardItem> matchedItems)
    {
        List<IBoardItem> adjacentObstacles = new List<IBoardItem>();
        HashSet<IBoardItem> visited = new HashSet<IBoardItem>();

        foreach (var item in matchedItems)
        {
            CheckAdjacentForObstacle(item.X + 1, item.Y, visited, adjacentObstacles);
            CheckAdjacentForObstacle(item.X - 1, item.Y, visited, adjacentObstacles);
            CheckAdjacentForObstacle(item.X, item.Y + 1, visited, adjacentObstacles);
            CheckAdjacentForObstacle(item.X, item.Y - 1, visited, adjacentObstacles);
        }

        return adjacentObstacles;
    }

    private void CheckAdjacentForObstacle(int x, int y, HashSet<IBoardItem> visited, List<IBoardItem> result)
    {
        var item = _gridSystem.GetItem(x, y);
        if (item != null && item is IDamageable && !visited.Contains(item))
        {
            visited.Add(item);
            result.Add(item);
        }
    }

    private void FindMatchesWithBfs(int startX, int startY, CubeColor color, bool[,] visited, List<IBoardItem> result)
    {
        Queue<GridPosition> pending = new Queue<GridPosition>();
        pending.Enqueue(new GridPosition(startX, startY));

        while (pending.Count > 0)
        {
            GridPosition position = pending.Dequeue();
            if (!_gridSystem.IsValid(position.X, position.Y)) continue;
            if (visited[position.X, position.Y]) continue;

            visited[position.X, position.Y] = true;

            var item = _gridSystem.GetItem(position.X, position.Y);
            if (item == null) continue;
            if (!(item is IMatchable matchable) || matchable.GetColor() != color) continue;

            result.Add(item);

            pending.Enqueue(new GridPosition(position.X + 1, position.Y));
            pending.Enqueue(new GridPosition(position.X - 1, position.Y));
            pending.Enqueue(new GridPosition(position.X, position.Y + 1));
            pending.Enqueue(new GridPosition(position.X, position.Y - 1));
        }
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
