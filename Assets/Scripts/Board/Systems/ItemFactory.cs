using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] private float _itemScale = 0.95f;

    private static readonly ItemId[] RandomColors = { ItemId.Red, ItemId.Green, ItemId.Blue, ItemId.Yellow };
    private readonly Dictionary<ItemId, GameObject> _prefabCache = new Dictionary<ItemId, GameObject>();
    private bool _cacheDirty = true;

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

    public IBoardItem CreateItem(string id, Transform parent)
    {
        return CreateItem(ItemIds.ToItemId(id), parent);
    }

    public IBoardItem CreateItem(ItemId itemId, Transform parent)
    {
        itemId = ResolveRandomId(itemId);

        GameObject prefab = GetPrefab(itemId);
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, parent);
        instance.transform.localScale = Vector3.one * _itemScale;
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
