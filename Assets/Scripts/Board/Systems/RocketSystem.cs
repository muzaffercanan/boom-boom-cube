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
    private readonly AudioClip _rocketSfx;

    private System.Action<Vector2Int> _onDamageRequest;

    public RocketSystem(GridSystem grid, ItemFactory factory, Transform boardParent, float cellSize, MonoBehaviour runner, System.Action<Vector2Int> onDamageRequest, AudioClip rocketSfx = null)
    {
        _gridSystem = grid;
        _itemFactory = factory;
        _boardParent = boardParent;
        _cellSize = cellSize;
        _coroutineRunner = runner;
        _onDamageRequest = onDamageRequest;
        _rocketSfx = rocketSfx;
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

        TriggerRocket(x, y, clickedRocket);
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

    public void TriggerRocket(int x, int y, RocketItem rocket)
    {

        if (rocket == null) return;

        _gridSystem.SetItem(x, y, null);
        
        GameObject go = rocket.GetGameObject();
        if (go != null)
        {
             Object.Destroy(go);
        }

        SpawnRocketBeams(x, y, rocket.IsHorizontal, false);
    }

    private void ProcessRocketCombo(RocketItem r1, RocketItem r2)
    {

        _gridSystem.SetItem(r1.X, r1.Y, null);
        _gridSystem.SetItem(r2.X, r2.Y, null);
        
        Object.Destroy(r1.GetGameObject());
        Object.Destroy(r2.GetGameObject());


        int centerX = r1.X;
        int centerY = r1.Y;

        // 2. Directly damage/clear the central 3x3 area
        // (Since projectiles move outwards and might skip their spawn cell)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int x = centerX + dx;
                int y = centerY + dy;
                
                if (_gridSystem.IsValid(x, y))
                {
                   _onDamageRequest?.Invoke(new Vector2Int(x, y));
                }
            }
        }


        for (int offset = -1; offset <= 1; offset++)
        {
            int rowY = centerY + offset;

            if (rowY >= 0 && rowY < _gridSystem.Height)
            {

                SpawnRocketBeams(centerX, rowY, isHorizontal: true, isCombo: true);
            }
        }


        for (int offset = -1; offset <= 1; offset++)
        {
            int colX = centerX + offset;

            if (colX >= 0 && colX < _gridSystem.Width)
            {

                SpawnRocketBeams(colX, centerY, isHorizontal: false, isCombo: true);
            }
        }
    }

    private void SpawnRocketBeams(int x, int y, bool isHorizontal, bool isCombo, int maxRange = -1)
    {
        if (isHorizontal)
        {
            SpawnProjectile("rocket_h_part_left", x, y, Vector2.left, maxRange);
            SpawnProjectile("rocket_h_part_right", x, y, Vector2.right, maxRange);
        }
        else
        {
            SpawnProjectile("rocket_v_part_bottom", x, y, Vector2.down, maxRange);
            SpawnProjectile("rocket_v_part_top", x, y, Vector2.up, maxRange);
        }
    }

    private void SpawnProjectile(string prefabId, int startX, int startY, Vector2 direction, int maxRange = -1)
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


        float worldY = startY * _cellSize;
        projectileObj.transform.localPosition = new Vector3(startX * _cellSize, worldY, -1f);

        var projectileComp = projectileObj.GetComponent<RocketProjectile>();
        if (projectileComp == null) projectileComp = projectileObj.AddComponent<RocketProjectile>();

        projectileComp.Init(direction, startX, startY, _cellSize, _gridSystem, OnProjectileHitCell, _rocketSfx, maxRange);
    }

    private void OnProjectileHitCell(int x, int y)
    {
        _onDamageRequest?.Invoke(new Vector2Int(x, y));
    }
}
