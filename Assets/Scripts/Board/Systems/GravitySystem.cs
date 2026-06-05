using UnityEngine;

public class GravitySystem
{
    private readonly GridSystem _grid;
    private readonly float _cellSize;

    public GravitySystem(GridSystem grid, float cellSize)
    {
        _grid = grid;
        _cellSize = cellSize;
    }

    public bool ApplyGravity()
    {
        bool anyItemMoved = false;

        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 1; y < _grid.Height; y++)
            {
                var item = _grid.GetItem(x, y);
                if (item == null) continue;

                if (item is IFallable && _grid.GetItem(x, y - 1) == null)
                {
                    MoveItemDown(item, x, y);
                    anyItemMoved = true;
                }
            }
        }

        return anyItemMoved;
    }

    private void MoveItemDown(IBoardItem item, int x, int y)
    {
        _grid.SetItem(x, y, null);
        _grid.SetItem(x, y - 1, item);


        Vector3 targetPos = new Vector3(
            x * _cellSize,
            (y - 1) * _cellSize,
            0f
        );


        if (item is AbstractBoardItem abi)
        {

            abi.MoveToPosition(targetPos, 0.15f); 
            

            abi.SetPosition(x, y - 1);
        }
        else
        {
            GameObject go = item.GetGameObject();
            if (go != null)
            {
                go.transform.localPosition = targetPos;
            }
        }
    }
}
