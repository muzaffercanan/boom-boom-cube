using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketSystem
{
    private readonly GridSystem _gridSystem;
    private readonly ItemFactory _itemFactory;
    private readonly Transform _boardParent;
    private readonly float _cellSize;
    private readonly MonoBehaviour _coroutineRunner;

    private System.Action<Vector2Int> _onDamageRequest;

    public RocketSystem(GridSystem grid, ItemFactory factory, Transform boardParent, float cellSize, MonoBehaviour runner, System.Action<Vector2Int> onDamageRequest)
    {
        _gridSystem = grid;
        _itemFactory = factory;
        _boardParent = boardParent;
        _cellSize = cellSize;
        _coroutineRunner = runner;
        _onDamageRequest = onDamageRequest;
    }

    public bool TryProcessRocketClick(int x, int y, RocketItem clickedRocket, out bool isCombo)
    {
        isCombo = false;
        
        var neighborRocket = FindNeighborRocket(x, y);

        if (neighborRocket != null)
        {
            isCombo = true;
            ProcessRocketCombo(clickedRocket, neighborRocket);
            return true;
        }

        ProcessSingleRocket(x, y, clickedRocket);
        return true;
    }

    private RocketItem FindNeighborRocket(int x, int y)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            var item = _gridSystem.GetItem(x + dir.x, y + dir.y);
            if (item != null && item is RocketItem rItem)
            {
                return rItem;
            }
        }
        return null;
    }

    private void ProcessSingleRocket(int x, int y, RocketItem rocket)
    {
        _gridSystem.SetItem(x, y, null);
        Object.Destroy(rocket.GetGameObject());

        SpawnRocketBeams(x, y, rocket.IsHorizontal, false);
    }

    private void ProcessRocketCombo(RocketItem r1, RocketItem r2)
    {
        _gridSystem.SetItem(r1.X, r1.Y, null);
        _gridSystem.SetItem(r2.X, r2.Y, null);
        
        Object.Destroy(r1.GetGameObject());
        Object.Destroy(r2.GetGameObject());

        // Standard Cross Blast: Clear 1 Row and 1 Column
        SpawnRocketBeams(r1.X, r1.Y, true, true);
        SpawnRocketBeams(r1.X, r1.Y, false, true);
    }

    private void SpawnRocketBeams(int x, int y, bool isHorizontal, bool isCombo)
    {
        if (isHorizontal)
        {
            SpawnProjectile("rocket_h_part_left", x, y, Vector2.left);
            SpawnProjectile("rocket_h_part_right", x, y, Vector2.right);
        }
        else
        {
            SpawnProjectile("rocket_v_part_bottom", x, y, Vector2.down);
            SpawnProjectile("rocket_v_part_top", x, y, Vector2.up);
        }
    }

    private void SpawnProjectile(string prefabId, int startX, int startY, Vector2 direction)
    {
        GameObject projectileObj = _itemFactory.CreateVisual(prefabId, _boardParent); 
        
        if (projectileObj == null)
        {
            projectileObj = _itemFactory.CreateVisual("rocket_projectile", _boardParent);

            if (projectileObj == null)
            {
                projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileObj.transform.localScale = Vector3.one * 0.5f;
                projectileObj.transform.SetParent(_boardParent);
            }
        }

        // Apply bottom-left origin conversion
        float worldY = (_gridSystem.Height - 1 - startY) * _cellSize;
        projectileObj.transform.localPosition = new Vector3(startX * _cellSize, worldY, -1f);

        var projectileComp = projectileObj.GetComponent<RocketProjectile>();
        if (projectileComp == null) projectileComp = projectileObj.AddComponent<RocketProjectile>();

        projectileComp.Init(direction, startX, startY, _cellSize, _gridSystem, OnProjectileHitCell);
    }

    private void OnProjectileHitCell(int x, int y)
    {
        _onDamageRequest?.Invoke(new Vector2Int(x, y));
    }
}
