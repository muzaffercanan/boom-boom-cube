using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "DreamGames/ItemFactory")]
public class ItemFactory : ScriptableObject
{
    [System.Serializable]
    public struct ItemPrefabMap
    {
        public string id;
        public GameObject prefab;
    }

    public List<ItemPrefabMap> mappings;

    [Header("Settings")]
    [SerializeField] private float _itemScale = 0.95f;

    private static readonly string[] RandomColors = { "r", "g", "b", "y" };

    public GameObject GetPrefab(string id)
    {
        if (id == "rand")
        {
            id = RandomColors[Random.Range(0, RandomColors.Length)];
        }

        var match = mappings.Find(m => m.id == id);
        if (match.prefab == null)
        {
            Debug.LogError($"[ItemFactory] No prefab found for: {id}");
            return null;
        }
        return match.prefab;
    }

    public GameObject CreateVisual(string id, Transform parent)
    {
        GameObject prefab = GetPrefab(id);
        if (prefab == null) return null;

        return Instantiate(prefab, parent);
    }

    public Sprite GetSprite(string id)
    {
        GameObject prefab = GetPrefab(id);
        if (prefab != null)
        {
            var r = prefab.GetComponent<SpriteRenderer>();
            if (r != null) return r.sprite;
        }
        return null;
    }

    public IBoardItem CreateItem(string id, Transform parent)
    {
        if (id == "rand")
        {
            id = RandomColors[Random.Range(0, RandomColors.Length)];
        }

        GameObject prefab = GetPrefab(id);
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, parent);
        instance.transform.localScale = Vector3.one * _itemScale;
        var boardItem = instance.GetComponent<IBoardItem>();

        if (boardItem is CubeItem cubeItem)
        {
            CubeColor color = ParseColor(id);
            cubeItem.Init(color);
        }
        else if (boardItem is RocketItem rocketItem)
        {
            bool isHorizontal = (id == "hro");
            rocketItem.Init(isHorizontal);
        }

        return boardItem;
    }

    private CubeColor ParseColor(string id)
    {
        switch (id)
        {
            case "r": return CubeColor.Red;
            case "g": return CubeColor.Green;
            case "b": return CubeColor.Blue;
            case "y": return CubeColor.Yellow;
            default: return CubeColor.Red;
        }
    }
}
