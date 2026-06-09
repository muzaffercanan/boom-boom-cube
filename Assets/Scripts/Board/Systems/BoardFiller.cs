using DG.Tweening;
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
public class BoardFiller
{
    private readonly GridSystem _gridSystem;
    private readonly ItemFactory _itemFactory;
    private readonly Transform _boardParent;
    private readonly BoardGeometry _geometry;
    private readonly BoardAnimationConfig _animationConfig;

    private Action<int, int> _onItemClicked;

    public BoardFiller(
        GridSystem gridSystem,
        ItemFactory itemFactory,
        Transform boardParent,
        float cellSize,
        BoardAnimationConfig animationConfig = null)
        : this(gridSystem, itemFactory, boardParent, new BoardGeometry(boardParent, cellSize), animationConfig)
    {
    }

    public BoardFiller(
        GridSystem gridSystem,
        ItemFactory itemFactory,
        Transform boardParent,
        BoardGeometry geometry,
        BoardAnimationConfig animationConfig = null)
    {
        _gridSystem = gridSystem;
        _itemFactory = itemFactory;
        _boardParent = boardParent;
        _geometry = geometry ?? new BoardGeometry(boardParent, 1f);
        _animationConfig = animationConfig ?? BoardAnimationConfig.Default;
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
        var item = _itemFactory.CreateItem(ItemIds.Random, _boardParent, _geometry.CellSize);
        if (item == null) return 0f;

        item.Init(_onItemClicked);
        _gridSystem.SetItem(x, y, item);

        var go = item.GetGameObject();
        if (go == null) return 0f;

        Vector3 targetPosition = _geometry.CellToLocalPosition(x, y);

        float spawnY = (_gridSystem.Height + _animationConfig.SpawnRowsAboveBoard + columnSpawnIndex) * _geometry.CellSize;
        go.transform.DOKill();
        go.transform.localPosition = new Vector3(x * _geometry.CellSize, spawnY, 0f);

        Vector3 targetScale = go.transform.localScale;
        go.transform.localScale = targetScale * _animationConfig.SpawnStartScale;

        float invSpeed = 1f / GameDebug.SpeedMultiplier;
        float fallCells = Mathf.Max(1f, (spawnY - targetPosition.y) / _geometry.CellSize);
        float moveDuration = _animationConfig.GetFallDuration(fallCells) * invSpeed;
        float delay = _animationConfig.GetCascadeDelay(columnSpawnIndex) * invSpeed;
        float bounceDuration = _animationConfig.LandingBounceDuration * invSpeed;

        if (item is AbstractBoardItem boardItem)
        {
            boardItem.FallToPositionDelayed(
                targetPosition,
                delay,
                moveDuration,
                _animationConfig.LandingBounceHeight,
                bounceDuration);
        }
        else
        {
            Sequence sequence = DOTween.Sequence().SetTarget(go.transform);
            if (delay > 0f)
            {
                sequence.AppendInterval(delay);
            }

            sequence.Append(go.transform.DOLocalMove(targetPosition, moveDuration).SetEase(Ease.InQuad));
        }

        go.transform
            .DOScale(targetScale, Mathf.Min(0.12f * invSpeed, moveDuration))
            .SetEase(Ease.OutQuad)
            .SetDelay(delay)
            .SetTarget(go.transform);

        return delay + moveDuration + bounceDuration;
    }

    public void CreateRocket(int x, int y)
    {
        bool isHorizontal = GameRng.Shared.Value() < 0.5f;
        string rocketId = isHorizontal ? ItemIds.HorizontalRocket : ItemIds.VerticalRocket;

        var rocket = _itemFactory.CreateItem(rocketId, _boardParent, _geometry.CellSize);
        if (rocket == null) return;

        rocket.Init(_onItemClicked);
        _gridSystem.SetItem(x, y, rocket);

        var go = rocket.GetGameObject();
        if (go == null)
        {
            Debug.LogError("[BoardFiller] Rocket GetGameObject() is null");
            return;
        }

        go.transform.localPosition = _geometry.CellToLocalPosition(x, y);
    }
}
}
