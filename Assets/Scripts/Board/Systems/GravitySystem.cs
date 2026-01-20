using UnityEngine;

public class GravitySystem
{
    private GridSystem _grid;
    private float _fallDuration;

    public GravitySystem(GridSystem grid, float fallDuration = 0.2f)
    {
        _grid = grid;
        _fallDuration = fallDuration;
    }

    public void ApplyGravity()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < _grid.Height; y++)
            {
                var item = _grid.GetItem(x, y);

                if (item != null && !(item is IFallable))
                {
                    writeY = y + 1;
                    continue;
                }

                if (item != null && item is IFallable fallable)
                {
                    if (y != writeY)
                    {
                        _grid.ClearCell(x, y);
                        _grid.SetItem(x, writeY, item);
                        fallable.FallTo(writeY, _fallDuration);
                    }
                    writeY++;
                }
            }
        }
    }
}
