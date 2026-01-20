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

        FloodFill(startX, startY, targetColor, visited, matches);

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

    private void FloodFill(int x, int y, CubeColor color, bool[,] visited, List<IBoardItem> result)
    {
        if (!_gridSystem.IsValid(x, y)) return;
        if (visited[x, y]) return;

        var item = _gridSystem.GetItem(x, y);
        if (item == null) return;

        if (item is IMatchable m && m.GetColor() == color)
        {
            visited[x, y] = true;
            result.Add(item);

            FloodFill(x + 1, y, color, visited, result);
            FloodFill(x - 1, y, color, visited, result);
            FloodFill(x, y + 1, color, visited, result);
            FloodFill(x, y - 1, color, visited, result);
        }
    }
}
