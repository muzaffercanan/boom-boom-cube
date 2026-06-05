using DG.Tweening;
using UnityEngine;
using System;

public class BoardFiller
{
    private const float SpawnRowsAboveBoard = 1.0f;
    private const float SpawnBaseDuration = 0.18f;
    private const float SpawnDurationPerCell = 0.025f;
    private const float SpawnStagger = 0.03f;
    private const float SpawnStartScale = 0.9f;

    private readonly GridSystem _gridSystem;
    private readonly ItemFactory _itemFactory;
    private readonly Transform _boardParent;
    private readonly float _cellSize;

    private Action<int, int> _onItemClicked;

    public BoardFiller(GridSystem gridSystem, ItemFactory itemFactory, Transform boardParent, float cellSize)
    {
        _gridSystem = gridSystem;
        _itemFactory = itemFactory;
        _boardParent = boardParent;
        _cellSize = cellSize;
    }

    public float LastFillAnimationDuration { get; private set; }

    public void SetClickCallback(Action<int, int> onItemClicked)
    {
        _onItemClicked = onItemClicked;
    }

    public float FillEmptySpaces()
    {
        LastFillAnimationDuration = 0f;
        int[] spawnCountsByColumn = new int[_gridSystem.Width];

        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                if (_gridSystem.GetItem(x, y) == null)
                {
                    float duration = SpawnItemAt(x, y, spawnCountsByColumn[x]);
                    LastFillAnimationDuration = Mathf.Max(LastFillAnimationDuration, duration);
                    spawnCountsByColumn[x]++;
                }
            }
        }

        return LastFillAnimationDuration;
    }

    public float SpawnItemAt(int x, int y)
    {
        return SpawnItemAt(x, y, 0);
    }

    private float SpawnItemAt(int x, int y, int columnSpawnIndex)
    {
        var item = _itemFactory.CreateItem(ItemIds.Random, _boardParent);
        if (item == null) return 0f;

        item.Init(_onItemClicked);
        _gridSystem.SetItem(x, y, item);

        var go = item.GetGameObject();
        if (go == null) return 0f;

        Vector3 targetPosition = new Vector3(x * _cellSize, y * _cellSize, 0f);
        float spawnY = (_gridSystem.Height + SpawnRowsAboveBoard + columnSpawnIndex) * _cellSize;
        Vector3 spawnPosition = new Vector3(x * _cellSize, spawnY, 0f);

        Vector3 targetScale = go.transform.localScale;
        float distanceInCells = Mathf.Abs(spawnY - targetPosition.y) / Mathf.Max(_cellSize, 0.001f);
        float moveDuration = SpawnBaseDuration + distanceInCells * SpawnDurationPerCell;
        float delay = columnSpawnIndex * SpawnStagger;

        go.transform.DOKill();
        go.transform.localPosition = spawnPosition;
        go.transform.localScale = targetScale * SpawnStartScale;

        Sequence sequence = DOTween.Sequence().SetTarget(go.transform);
        if (delay > 0f)
        {
            sequence.AppendInterval(delay);
        }

        sequence.Join(
            go.transform
                .DOLocalMove(targetPosition, moveDuration)
                .SetEase(Ease.OutCubic)
        );
        sequence.Join(
            go.transform
                .DOScale(targetScale, Mathf.Min(0.12f, moveDuration))
                .SetEase(Ease.OutQuad)
        );

        return delay + moveDuration;
    }

    public void CreateRocket(int x, int y)
    {
        bool isHorizontal = GameRng.Shared.Value() < 0.5f;
        string rocketId = isHorizontal ? ItemIds.HorizontalRocket : ItemIds.VerticalRocket;

        var rocket = _itemFactory.CreateItem(rocketId, _boardParent);
        if (rocket == null) return;

        rocket.Init(_onItemClicked);
        _gridSystem.SetItem(x, y, rocket);

        var go = rocket.GetGameObject();
        if (go == null)
        {
            Debug.LogError("[BoardFiller] Rocket GetGameObject() is null");
            return;
        }

        go.transform.localPosition = new Vector3(x * _cellSize, y * _cellSize, 0);
    }
}
