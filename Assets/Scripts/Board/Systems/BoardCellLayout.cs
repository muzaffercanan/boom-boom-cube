using DreamGames.Data;

namespace DreamGames.Board.Systems
{
public static class BoardCellLayout
{
    public static BoardCellState[,] FromLevelData(LevelData data)
    {
        BoardCellState[,] cells = new BoardCellState[data.grid_width, data.grid_height];

        for (int y = 0; y < data.grid_height; y++)
        {
            for (int x = 0; x < data.grid_width; x++)
            {
                int index = y * data.grid_width + x;
                cells[x, y] = data.HasCellLayout && index < data.cells.Count
                    ? BoardCellState.FromLevelCell(data.cells[index])
                    : BoardCellState.Normal;
            }
        }

        return cells;
    }
}
}
