using System.Collections.Generic;
using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Systems
{
public sealed class ShuffleSystem
{
    private readonly GridSystem _gridSystem;
    private readonly int _minGroupSize;
    private readonly float _cellSize;
    private readonly GameRng _rng;
    private readonly int _maxAttempts;

    public ShuffleSystem(
        GridSystem gridSystem,
        int minGroupSize,
        float cellSize = 1f,
        GameRng rng = null,
        int maxAttempts = 20)
    {
        _gridSystem = gridSystem;
        _minGroupSize = minGroupSize;
        _cellSize = cellSize;
        _rng = rng ?? GameRng.Shared;
        _maxAttempts = Mathf.Max(1, maxAttempts);
    }

    public bool TryShuffleNormalCubesUntilPlayable(out int attemptsUsed)
    {
        attemptsUsed = 0;

        if (_gridSystem == null || _gridSystem.Width <= 0 || _gridSystem.Height <= 0)
        {
            return false;
        }

        NoMoveScanner scanner = new NoMoveScanner(_gridSystem, _minGroupSize);
        if (scanner.HasPlayableMove())
        {
            return true;
        }

        List<Vector2Int> cubePositions = new List<Vector2Int>();
        List<IBoardItem> originalItems = new List<IBoardItem>();
        CollectNormalCubes(cubePositions, originalItems);

        if (cubePositions.Count < _minGroupSize)
        {
            return false;
        }

        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            attemptsUsed = attempt;
            List<IBoardItem> shuffled = new List<IBoardItem>(originalItems);
            ShuffleList(shuffled);
            ApplyArrangement(cubePositions, shuffled);

            if (scanner.HasPlayableMove())
            {
                return true;
            }
        }

        if (TryApplyDeterministicPlayableArrangement(cubePositions, originalItems) && scanner.HasPlayableMove())
        {
            return true;
        }

        ApplyArrangement(cubePositions, originalItems);
        return false;
    }

    private void CollectNormalCubes(List<Vector2Int> positions, List<IBoardItem> items)
    {
        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                IBoardItem item = _gridSystem.GetItem(x, y);
                if (!IsNormalCube(item)) continue;

                positions.Add(new Vector2Int(x, y));
                items.Add(item);
            }
        }
    }

    private bool TryApplyDeterministicPlayableArrangement(List<Vector2Int> positions, List<IBoardItem> originalItems)
    {
        if (!TryFindConnectedSlots(positions, _minGroupSize, out List<Vector2Int> connectedSlots))
        {
            return false;
        }

        if (!TryFindColorGroup(originalItems, _minGroupSize, out List<IBoardItem> colorGroup))
        {
            return false;
        }

        IBoardItem[] arranged = new IBoardItem[originalItems.Count];
        HashSet<IBoardItem> usedItems = new HashSet<IBoardItem>();

        for (int i = 0; i < connectedSlots.Count; i++)
        {
            int positionIndex = positions.IndexOf(connectedSlots[i]);
            if (positionIndex < 0) return false;

            arranged[positionIndex] = colorGroup[i];
            usedItems.Add(colorGroup[i]);
        }

        int fillIndex = 0;
        for (int i = 0; i < originalItems.Count; i++)
        {
            if (usedItems.Contains(originalItems[i])) continue;

            while (fillIndex < arranged.Length && arranged[fillIndex] != null)
            {
                fillIndex++;
            }

            if (fillIndex >= arranged.Length) break;
            arranged[fillIndex] = originalItems[i];
        }

        ApplyArrangement(positions, new List<IBoardItem>(arranged));
        return true;
    }

    private bool TryFindConnectedSlots(List<Vector2Int> positions, int targetCount, out List<Vector2Int> connectedSlots)
    {
        connectedSlots = null;
        HashSet<Vector2Int> positionSet = new HashSet<Vector2Int>(positions);

        foreach (Vector2Int start in positions)
        {
            List<Vector2Int> cluster = new List<Vector2Int>();
            Queue<Vector2Int> pending = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            pending.Enqueue(start);
            visited.Add(start);

            while (pending.Count > 0)
            {
                Vector2Int current = pending.Dequeue();
                cluster.Add(current);

                if (cluster.Count >= targetCount)
                {
                    connectedSlots = cluster;
                    return true;
                }

                TryEnqueueSlot(current + Vector2Int.up, positionSet, visited, pending);
                TryEnqueueSlot(current + Vector2Int.down, positionSet, visited, pending);
                TryEnqueueSlot(current + Vector2Int.left, positionSet, visited, pending);
                TryEnqueueSlot(current + Vector2Int.right, positionSet, visited, pending);
            }
        }

        return false;
    }

    private static void TryEnqueueSlot(
        Vector2Int position,
        HashSet<Vector2Int> positionSet,
        HashSet<Vector2Int> visited,
        Queue<Vector2Int> pending)
    {
        if (!positionSet.Contains(position) || visited.Contains(position))
        {
            return;
        }

        visited.Add(position);
        pending.Enqueue(position);
    }

    private static bool TryFindColorGroup(List<IBoardItem> items, int targetCount, out List<IBoardItem> colorGroup)
    {
        Dictionary<CubeColor, List<IBoardItem>> byColor = new Dictionary<CubeColor, List<IBoardItem>>();

        foreach (IBoardItem item in items)
        {
            if (!(item is IMatchable matchable)) continue;

            CubeColor color = matchable.GetColor();
            if (!byColor.TryGetValue(color, out List<IBoardItem> group))
            {
                group = new List<IBoardItem>();
                byColor.Add(color, group);
            }

            group.Add(item);
        }

        foreach (List<IBoardItem> group in byColor.Values)
        {
            if (group.Count < targetCount) continue;

            colorGroup = group.GetRange(0, targetCount);
            return true;
        }

        colorGroup = null;
        return false;
    }

    private void ShuffleList(List<IBoardItem> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = _rng.Range(0, i + 1);
            IBoardItem temp = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = temp;
        }
    }

    private void ApplyArrangement(List<Vector2Int> positions, List<IBoardItem> items)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int position = positions[i];
            IBoardItem item = items[i];

            _gridSystem.SetItem(position.x, position.y, item);
            MoveVisual(item, position);
        }
    }

    private void MoveVisual(IBoardItem item, Vector2Int position)
    {
        if (item == null) return;

        GameObject go = item.GetGameObject();
        if (go == null) return;

        go.transform.localPosition = new Vector3(position.x * _cellSize, position.y * _cellSize, 0f);
    }

    private static bool IsNormalCube(IBoardItem item)
    {
        return item != null && item.GetItemType() == ItemType.Cube && item is IMatchable;
    }
}
}
