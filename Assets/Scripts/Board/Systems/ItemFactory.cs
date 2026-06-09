using UnityEngine;
using System.Collections.Generic;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Systems
{
[CreateAssetMenu(menuName = "DreamGames/ItemFactory")]
public class ItemFactory : ScriptableObject
{
    [System.Serializable]
    public struct ItemPrefabMap
    {
        public ItemId itemId;
        public string id;
        public GameObject prefab;
    }

    public List<ItemPrefabMap> mappings;

    [Header("Settings")]
    [SerializeField] private BoardVisualConfig _visualConfig;
    [SerializeField, HideInInspector] private float _itemScale = 1.0f;

    private static readonly ItemId[] RandomColors = { ItemId.Red, ItemId.Green, ItemId.Blue, ItemId.Yellow };
    private readonly Dictionary<ItemId, GameObject> _prefabCache = new Dictionary<ItemId, GameObject>();
    private bool _cacheDirty = true;

    public BoardVisualConfig VisualConfig => _visualConfig;
    public float LegacyItemScale => Mathf.Max(0.01f, _itemScale);

    private void OnEnable()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        _cacheDirty = true;
    }

    public GameObject GetPrefab(string id)
    {
        return GetPrefab(ItemIds.ToItemId(id));
    }

    public GameObject GetPrefab(ItemId itemId)
    {
        itemId = ResolveRandomId(itemId);
        EnsureCache();

        if (!_prefabCache.TryGetValue(itemId, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[ItemFactory] No prefab found for: {itemId}");
            return null;
        }

        return prefab;
    }

    public GameObject CreateVisual(string id, Transform parent)
    {
        return CreateVisual(ItemIds.ToItemId(id), parent);
    }

    public GameObject CreateVisual(ItemId itemId, Transform parent)
    {
        GameObject prefab = GetPrefab(itemId);
        if (prefab == null) return null;

        return Instantiate(prefab, parent);
    }

    public Sprite GetSprite(string id)
    {
        GameObject prefab = GetPrefab(ItemIds.ToItemId(id));
        if (prefab != null)
        {
            var r = prefab.GetComponent<SpriteRenderer>();
            if (r != null) return r.sprite;
        }
        return null;
    }

    public IBoardItem CreateItem(string id, Transform parent, float cellSize = 1f)
    {
        return CreateItem(ItemIds.ToItemId(id), parent, cellSize);
    }

    public IBoardItem CreateItem(ItemId itemId, Transform parent, float cellSize = 1f)
    {
        itemId = ResolveRandomId(itemId);

        GameObject prefab = GetPrefab(itemId);
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, parent);
        ApplyVisualSettings(instance, itemId, cellSize);
        var boardItem = instance.GetComponent<IBoardItem>();

        if (boardItem is CubeItem cubeItem)
        {
            CubeColor color = ParseColor(itemId);
            cubeItem.Init(color);
        }
        else if (boardItem is RocketItem rocketItem)
        {
            bool isHorizontal = (itemId == ItemId.HorizontalRocket);
            rocketItem.Init(isHorizontal);
        }

        return boardItem;
    }

    public void SetVisualConfig(BoardVisualConfig visualConfig)
    {
        _visualConfig = visualConfig;
    }

    public ItemVisualSettings ResolveVisualSettings(ItemId itemId)
    {
        return _visualConfig != null
            ? _visualConfig.Resolve(itemId)
            : ItemVisualSettings.Default(LegacyItemScale, Vector2.zero);
    }

    public void SetDefaultItemScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0.2f, 1.5f);
        if (_visualConfig != null)
        {
            _visualConfig.DefaultItemScale = scale;
        }
        else
        {
            _itemScale = scale;
        }
    }

    public float GetDefaultItemScale()
    {
        return _visualConfig != null ? _visualConfig.DefaultItemScale : LegacyItemScale;
    }

    public float GetCellSize(float fallbackCellSize)
    {
        return _visualConfig != null ? _visualConfig.CellSize : Mathf.Max(0.01f, fallbackCellSize);
    }

    public void ApplyVisualSettings(GameObject instance, ItemId itemId, float cellSize)
    {
        if (instance == null) return;

        ItemVisualSettings settings = ResolveVisualSettings(itemId);
        float safeCellSize = Mathf.Max(0.01f, cellSize);
        float rootScale = settings.VisualScale * safeCellSize;
        instance.transform.localScale = Vector3.one * rootScale;

        if (instance.TryGetComponent(out AbstractBoardItem boardItem))
        {
            boardItem.SetSortingBias(settings.VisualSortBias);
        }

        ApplyVisualOffset(instance, settings.VisualOffset, rootScale);
        ConfigureColliders(instance, settings, rootScale, safeCellSize);
    }

    public Vector2 EstimateMaxVisualHalfExtents(float cellSize)
    {
        float safeCellSize = Mathf.Max(0.01f, cellSize);
        Vector2 max = Vector2.one * (safeCellSize * 0.5f);

        EnsureCache();

        foreach (KeyValuePair<ItemId, GameObject> pair in _prefabCache)
        {
            if (pair.Value == null) continue;

            ItemVisualSettings settings = ResolveVisualSettings(pair.Key);
            Bounds localBounds = CalculatePrefabLocalRendererBounds(pair.Value);
            Vector2 half = localBounds.size.sqrMagnitude > 0f
                ? new Vector2(localBounds.extents.x, localBounds.extents.y) * settings.VisualScale * safeCellSize
                : Vector2.one * (settings.VisualScale * safeCellSize * 0.5f);

            half += new Vector2(Mathf.Abs(settings.VisualOffset.x), Mathf.Abs(settings.VisualOffset.y));
            max = Vector2.Max(max, half);
        }

        if (_visualConfig != null)
        {
            max = Vector2.Max(max, _visualConfig.EstimateMaxVisualHalfExtents());
        }

        return max;
    }

    private static void ApplyVisualOffset(GameObject instance, Vector2 visualOffset, float rootScale)
    {
        if (visualOffset == Vector2.zero) return;

        Transform visual = instance.transform.Find("Visual");
        if (visual == null)
        {
            return;
        }

        float safeRootScale = Mathf.Max(0.01f, rootScale);
        visual.localPosition = new Vector3(
            visualOffset.x / safeRootScale,
            visualOffset.y / safeRootScale,
            visual.localPosition.z);
    }

    private static void ConfigureColliders(
        GameObject instance,
        ItemVisualSettings settings,
        float rootScale,
        float cellSize)
    {
        if (!settings.ConfigureCollider) return;

        BoxCollider2D[] colliders = instance.GetComponentsInChildren<BoxCollider2D>(true);
        if (colliders == null || colliders.Length == 0) return;

        float safeRootScale = Mathf.Max(0.01f, rootScale);
        Vector2 desiredWorldSize = Vector2.Min(settings.ColliderSizeInCells, Vector2.one) * cellSize;
        Vector2 desiredWorldOffset = settings.ColliderOffsetInCells * cellSize;

        for (int i = 0; i < colliders.Length; i++)
        {
            BoxCollider2D collider = colliders[i];
            if (collider == null) continue;

            collider.size = new Vector2(
                desiredWorldSize.x / safeRootScale,
                desiredWorldSize.y / safeRootScale);
            collider.offset = new Vector2(
                desiredWorldOffset.x / safeRootScale,
                desiredWorldOffset.y / safeRootScale);
        }
    }

    private static Bounds CalculatePrefabLocalRendererBounds(GameObject prefab)
    {
        SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;
        Bounds combined = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.sprite == null) continue;

            Bounds spriteBounds = renderer.sprite.bounds;
            Matrix4x4 matrix = prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Bounds rendererBounds = TransformBounds(spriteBounds, matrix);

            if (!hasBounds)
            {
                combined = rendererBounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(rendererBounds);
            }
        }

        return hasBounds ? combined : new Bounds(Vector3.zero, Vector3.zero);
    }

    private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        bool initialized = false;
        Bounds transformed = new Bounds();

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 point = matrix.MultiplyPoint3x4(corner);
                    if (!initialized)
                    {
                        transformed = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        transformed.Encapsulate(point);
                    }
                }
            }
        }

        return transformed;
    }

    private void EnsureCache()
    {
        if (_cacheDirty)
        {
            RebuildCache();
        }
    }

    private void RebuildCache()
    {
        _prefabCache.Clear();
        _cacheDirty = false;

        if (mappings == null)
        {
            Debug.LogWarning("[ItemFactory] Mappings list is null.");
            return;
        }

        foreach (var mapping in mappings)
        {
            ItemId itemId = ResolveMappingId(mapping);
            if (itemId == ItemId.Unknown)
            {
                Debug.LogWarning("[ItemFactory] Mapping contains an unknown item id.");
                continue;
            }

            if (mapping.prefab == null)
            {
                Debug.LogWarning($"[ItemFactory] Mapping '{itemId}' has no prefab assigned.");
                continue;
            }

            if (_prefabCache.ContainsKey(itemId))
            {
                Debug.LogWarning($"[ItemFactory] Duplicate mapping id '{itemId}' ignored.");
                continue;
            }

            _prefabCache.Add(itemId, mapping.prefab);
        }
    }

    private static ItemId ResolveMappingId(ItemPrefabMap mapping)
    {
        if (mapping.itemId != ItemId.Unknown)
        {
            return mapping.itemId;
        }

        return ItemIds.ToItemId(mapping.id);
    }

    private ItemId ResolveRandomId(ItemId itemId)
    {
        if (itemId != ItemId.Random)
        {
            return itemId;
        }

        return RandomColors[GameRng.Shared.Range(0, RandomColors.Length)];
    }

    private CubeColor ParseColor(ItemId itemId)
    {
        switch (itemId)
        {
            case ItemId.Red: return CubeColor.Red;
            case ItemId.Green: return CubeColor.Green;
            case ItemId.Blue: return CubeColor.Blue;
            case ItemId.Yellow: return CubeColor.Yellow;
            default: return CubeColor.Red;
        }
    }
}
}
