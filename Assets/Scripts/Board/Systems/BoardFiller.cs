using UnityEngine;
using System.Collections;
using System;

public class BoardFiller
{
    private readonly GridSystem _gridSystem;
    private readonly ItemFactory _itemFactory;
    private readonly Transform _boardParent;
    private readonly float _cellSize;
    private readonly MonoBehaviour _runner;

    private Action<int, int> _onItemClicked;

    public BoardFiller(GridSystem gridSystem, ItemFactory itemFactory, Transform boardParent, float cellSize, MonoBehaviour runner)
    {
        _gridSystem = gridSystem;
        _itemFactory = itemFactory;
        _boardParent = boardParent;
        _cellSize = cellSize;
        _runner = runner;
    }

    public void SetClickCallback(Action<int, int> onItemClicked)
    {
        _onItemClicked = onItemClicked;
    }

    public void FillEmptySpaces()
    {
        for (int x = 0; x < _gridSystem.Width; x++)
        {
            for (int y = 0; y < _gridSystem.Height; y++)
            {
                if (_gridSystem.GetItem(x, y) == null)
                    SpawnItemAt(x, y);
            }
        }
    }

    public void SpawnItemAt(int x, int y)
    {
        var item = _itemFactory.CreateItem(ItemIds.Random, _boardParent);
        if (item == null) return;

        item.Init(_onItemClicked);
        _gridSystem.SetItem(x, y, item);

        var go = item.GetGameObject();
        if (go == null) return;

        go.transform.localPosition = new Vector3(x * _cellSize, y * _cellSize, 0);

        Vector3 targetScale = go.transform.localScale;
        go.transform.localScale = Vector3.zero;
        _runner.StartCoroutine(ScaleUp(go.transform, 0.2f, targetScale));
    }

    public void CreateRocket(int x, int y)
    {
        bool isHorizontal = UnityEngine.Random.value < 0.5f;
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

    private IEnumerator ScaleUp(Transform target, float duration, Vector3 targetScale)
    {
        float t = 0;
        while (t < 1f)
        {
            if (target == null) yield break;
            t += Time.deltaTime / duration;
            target.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }
        if (target != null) target.localScale = targetScale;
    }
}
