using System;
using System.Collections.Generic;
using UnityEngine;
using DreamGames.Core;

namespace DreamGames.Board.Visuals
{
[CreateAssetMenu(fileName = "BoardVisualConfig", menuName = "DreamGames/Board Visual Config")]
public sealed class BoardVisualConfig : ScriptableObject
{
    [SerializeField, Min(0.01f)] private float _cellSize = 1f;
    [SerializeField, Min(0.01f)] private float _defaultItemScale = 0.65f;
    [SerializeField] private Vector2 _defaultItemOffset = Vector2.zero;
    [SerializeField] private Vector2 _backgroundPadding = new Vector2(0.1f, 0.1f);
    [SerializeField] private Vector2 _cameraPadding = new Vector2(0.15f, 0.15f);
    [SerializeField] private List<ItemVisualOverride> _itemOverrides = new List<ItemVisualOverride>();

    public float CellSize
    {
        get => Mathf.Max(0.01f, _cellSize);
        set => _cellSize = Mathf.Max(0.01f, value);
    }

    public float DefaultItemScale
    {
        get => Mathf.Max(0.01f, _defaultItemScale);
        set => _defaultItemScale = Mathf.Max(0.01f, value);
    }

    public Vector2 DefaultItemOffset
    {
        get => _defaultItemOffset;
        set => _defaultItemOffset = value;
    }

    public Vector2 BackgroundPadding
    {
        get => ClampNonNegative(_backgroundPadding);
        set => _backgroundPadding = ClampNonNegative(value);
    }

    public Vector2 CameraPadding
    {
        get => ClampNonNegative(_cameraPadding);
        set => _cameraPadding = ClampNonNegative(value);
    }

    public IReadOnlyList<ItemVisualOverride> ItemOverrides => _itemOverrides;

    public ItemVisualSettings Resolve(ItemId itemId)
    {
        ItemVisualSettings settings = ItemVisualSettings.Default(DefaultItemScale, DefaultItemOffset);

        if (_itemOverrides == null)
        {
            return settings;
        }

        for (int i = 0; i < _itemOverrides.Count; i++)
        {
            ItemVisualOverride itemOverride = _itemOverrides[i];
            if (itemOverride.ItemId != itemId)
            {
                continue;
            }

            return itemOverride.Resolve(settings);
        }

        return settings;
    }

    public Vector2 EstimateMaxVisualHalfExtents()
    {
        float cellSize = CellSize;
        Vector2 max = Vector2.one * (cellSize * 0.5f);

        IncludeSettings(ref max, ItemVisualSettings.Default(DefaultItemScale, DefaultItemOffset), cellSize);

        if (_itemOverrides != null)
        {
            for (int i = 0; i < _itemOverrides.Count; i++)
            {
                IncludeSettings(ref max, _itemOverrides[i].Resolve(ItemVisualSettings.Default(DefaultItemScale, DefaultItemOffset)), cellSize);
            }
        }

        return max;
    }

    private static void IncludeSettings(ref Vector2 max, ItemVisualSettings settings, float cellSize)
    {
        Vector2 half = Vector2.one * (settings.VisualScale * cellSize * 0.5f);
        half += new Vector2(Mathf.Abs(settings.VisualOffset.x), Mathf.Abs(settings.VisualOffset.y));
        max = Vector2.Max(max, half);
    }

    private static Vector2 ClampNonNegative(Vector2 value)
    {
        return new Vector2(Mathf.Max(0f, value.x), Mathf.Max(0f, value.y));
    }

    private void OnValidate()
    {
        _cellSize = Mathf.Max(0.01f, _cellSize);
        _defaultItemScale = Mathf.Max(0.01f, _defaultItemScale);
        _backgroundPadding = ClampNonNegative(_backgroundPadding);
        _cameraPadding = ClampNonNegative(_cameraPadding);
    }
}

[Serializable]
public sealed class ItemVisualOverride
{
    [SerializeField] private ItemId _itemId = ItemId.Unknown;
    [SerializeField, Min(0f)] private float _visualScale;
    [SerializeField] private Vector2 _visualOffset;
    [SerializeField] private bool _overrideVisualScale;
    [SerializeField] private bool _overrideVisualOffset;
    [SerializeField] private bool _fitInsideCell = true;
    [SerializeField] private bool _allowVerticalOverlap;
    [SerializeField] private bool _allowHorizontalOverlap;
    [SerializeField] private int _visualSortBias;
    [SerializeField] private bool _configureCollider = true;
    [SerializeField] private Vector2 _colliderSizeInCells = Vector2.one;
    [SerializeField] private Vector2 _colliderOffsetInCells = Vector2.zero;

    public ItemId ItemId => _itemId;

    public ItemVisualSettings Resolve(ItemVisualSettings fallback)
    {
        float visualScale = _overrideVisualScale && _visualScale > 0f
            ? _visualScale
            : fallback.VisualScale;
        Vector2 visualOffset = _overrideVisualOffset
            ? _visualOffset
            : fallback.VisualOffset;

        Vector2 colliderSize = new Vector2(
            _colliderSizeInCells.x > 0f ? _colliderSizeInCells.x : fallback.ColliderSizeInCells.x,
            _colliderSizeInCells.y > 0f ? _colliderSizeInCells.y : fallback.ColliderSizeInCells.y);

        return new ItemVisualSettings(
            visualScale,
            visualOffset,
            _fitInsideCell,
            _allowVerticalOverlap,
            _allowHorizontalOverlap,
            _visualSortBias,
            _configureCollider,
            colliderSize,
            _colliderOffsetInCells);
    }
}

public readonly struct ItemVisualSettings
{
    public readonly float VisualScale;
    public readonly Vector2 VisualOffset;
    public readonly bool FitInsideCell;
    public readonly bool AllowVerticalOverlap;
    public readonly bool AllowHorizontalOverlap;
    public readonly int VisualSortBias;
    public readonly bool ConfigureCollider;
    public readonly Vector2 ColliderSizeInCells;
    public readonly Vector2 ColliderOffsetInCells;

    public ItemVisualSettings(
        float visualScale,
        Vector2 visualOffset,
        bool fitInsideCell,
        bool allowVerticalOverlap,
        bool allowHorizontalOverlap,
        int visualSortBias,
        bool configureCollider,
        Vector2 colliderSizeInCells,
        Vector2 colliderOffsetInCells)
    {
        VisualScale = Mathf.Max(0.01f, visualScale);
        VisualOffset = visualOffset;
        FitInsideCell = fitInsideCell;
        AllowVerticalOverlap = allowVerticalOverlap;
        AllowHorizontalOverlap = allowHorizontalOverlap;
        VisualSortBias = visualSortBias;
        ConfigureCollider = configureCollider;
        ColliderSizeInCells = new Vector2(
            Mathf.Max(0.01f, colliderSizeInCells.x),
            Mathf.Max(0.01f, colliderSizeInCells.y));
        ColliderOffsetInCells = colliderOffsetInCells;
    }

    public static ItemVisualSettings Default(float visualScale, Vector2 visualOffset)
    {
        return new ItemVisualSettings(
            visualScale,
            visualOffset,
            fitInsideCell: true,
            allowVerticalOverlap: false,
            allowHorizontalOverlap: false,
            visualSortBias: 0,
            configureCollider: true,
            colliderSizeInCells: Vector2.one,
            colliderOffsetInCells: Vector2.zero);
    }
}
}
