using UnityEngine;
using System;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Systems
{
public class LevelLoader
{
    private ItemFactory _factory;
    private Transform _boardParent;
    private float _cellSize;
    private bool _enableDebugLogs;

    public LevelLoader(ItemFactory factory, Transform boardParent, float cellSize = 1.0f, bool enableDebugLogs = false)
    {
        _factory = factory;
        _boardParent = boardParent;
        _cellSize = cellSize;
        _enableDebugLogs = enableDebugLogs;
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

        LogBoardParent();

        ClearBoardParent();
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

                        LogDebug(
                            $"[LevelLoader] Spawn id={id} at grid({x},{y}) world({x * _cellSize},{worldY}) " +
                            $"go={go.name} isRoot={isRootObj} root={root.name}"
                        );

                        if (ShouldLogDebug() && _boardParent.TryGetComponent<MonoBehaviour>(out var mb))
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

    private void LogBoardParent()
    {
        if (!ShouldLogDebug()) return;
        BoardDebug.LogBoardParent(_boardParent);
    }

    private void ClearBoardParent()
    {
        for (int i = _boardParent.childCount - 1; i >= 0; i--)
        {
            Transform child = _boardParent.GetChild(i);
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private void LogDebug(string message)
    {
        if (!ShouldLogDebug()) return;
        Debug.Log(message);
    }

    private bool ShouldLogDebug()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return _enableDebugLogs;
#else
        return false;
#endif
    }
}
}
