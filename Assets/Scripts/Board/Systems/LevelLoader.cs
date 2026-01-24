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
        if (data == null)
        {
            Debug.LogError("[LevelLoader] LevelData is NULL");
            return;
        }

        if (_boardParent == null)
        {
            Debug.LogError("[LevelLoader] BoardParent is NULL");
            return;
        }

        if (_cellSize <= 0f)
        {
            Debug.LogError($"[LevelLoader] cellSize <= 0 ! cellSize={_cellSize}");
        }

        BoardDebug.LogBoardParent(_boardParent);

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

                    var go = item.GetGameObject();
                    if (go == null)
                    {
                        Debug.LogError($"[LevelLoader] GetGameObject() returned NULL for id={id} at ({x},{y})");
                    }
                    else
                    {
                        var root = go.transform.root;
                        bool isRootObj = (go.transform == go.transform.root);


                        float worldY = y * _cellSize;
                        go.transform.localPosition = new Vector3(x * _cellSize, worldY, 0);

                        Debug.Log(
                            $"[LevelLoader] Spawn id={id} at grid({x},{y}) world({x * _cellSize},{worldY}) " +
                            $"go={go.name} isRoot={isRootObj} root={root.name}"
                        );

                        if (_boardParent.TryGetComponent<MonoBehaviour>(out var mb))
                        {
                            mb.StartCoroutine(BoardDebug.LogNextFrame($"LevelSpawn id={id} ({x},{y})", go.transform));
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[LevelLoader] CreateItem returned NULL for id={id} at ({x},{y})");
                }

                index++;
            }
        }
    }
}
