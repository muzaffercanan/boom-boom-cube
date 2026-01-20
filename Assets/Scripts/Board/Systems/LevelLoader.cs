using UnityEngine;
using System;

public class LevelLoader
{
    private ItemFactory _factory;
    private Transform _boardParent;
    private float _cellSize;

    public LevelLoader(ItemFactory factory, Transform boardParent, float cellSize = 1.0f)
    {
        _factory = factory;
        _boardParent = boardParent;
        _cellSize = cellSize;
    }

    public void LoadLevel(GridSystem gridSystem, LevelData data, Action<int, int> onItemClicked)
    {
        gridSystem.Initialize(data.grid_width, data.grid_height);

        int index = 0;
        for (int y = 0; y < data.grid_height; y++)
        {
            for (int x = 0; x < data.grid_width; x++)
            {
                if (index >= data.grid.Count) break;

                string id = data.grid[index];
                
                var item = _factory.CreateItem(id, _boardParent);
                if (item != null)
                {
                    item.Init(onItemClicked);
                    gridSystem.SetItem(x, y, item);
                    item.GetGameObject().transform.localPosition = new Vector3(x, y, 0) * _cellSize;
                }

                index++;
            }
        }
    }
}
