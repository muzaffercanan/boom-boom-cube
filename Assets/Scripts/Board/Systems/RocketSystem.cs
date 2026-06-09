using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Systems
{
public class RocketSystem
{
    private readonly GridSystem _gridSystem;
    private readonly ItemFactory _itemFactory;
    private readonly Transform _boardParent;
    private readonly float _cellSize;
    private readonly MonoBehaviour _coroutineRunner;
    private readonly AudioClip _rocketSfx;
    private readonly Dictionary<string, ObjectPool<GameObject>> _projectilePools = new Dictionary<string, ObjectPool<GameObject>>();
    private readonly HashSet<RocketProjectile> _activeProjectiles = new HashSet<RocketProjectile>();

    private System.Action<Vector2Int> _onDamageRequest;
    private int _activeProjectileCount;

    public int ActiveProjectileCount => _activeProjectileCount;

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
            SpawnProjectile(ItemIds.RocketHorizontalPartLeft, x, y, Vector2.left, maxRange);
            SpawnProjectile(ItemIds.RocketHorizontalPartRight, x, y, Vector2.right, maxRange);
        }
        else
        {
            SpawnProjectile(ItemIds.RocketVerticalPartBottom, x, y, Vector2.down, maxRange);
            SpawnProjectile(ItemIds.RocketVerticalPartTop, x, y, Vector2.up, maxRange);
        }
    }

    private void SpawnProjectile(string prefabId, int startX, int startY, Vector2 direction, int maxRange = -1)
    {
        string poolKey = prefabId;
        GameObject projectileObj = GetProjectileFromPool(poolKey);
        
        if (projectileObj == null)
        {
            poolKey = ItemIds.RocketProjectile;
            projectileObj = GetProjectileFromPool(poolKey);

            if (projectileObj == null)
            {
                poolKey = null;
                projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileObj.transform.localScale = Vector3.one * 0.5f;
                projectileObj.transform.SetParent(_boardParent);
            }
        }


        float worldY = startY * _cellSize;
        projectileObj.transform.localPosition = new Vector3(startX * _cellSize, worldY, -1f);

        var projectileComp = projectileObj.GetComponent<RocketProjectile>();
        if (projectileComp == null) projectileComp = projectileObj.AddComponent<RocketProjectile>();

        projectileComp.SetPoolKey(poolKey);
        _activeProjectileCount++;
        _activeProjectiles.Add(projectileComp);
        projectileComp.Init(direction, startX, startY, _cellSize, _gridSystem, OnProjectileHitCell, _rocketSfx, maxRange, ReleaseTrackedProjectile);
    }

    public IEnumerator WaitForProjectilesToComplete(float timeoutSeconds = 2.5f)
    {
        float elapsed = 0f;

        while (_activeProjectileCount > 0 && elapsed < timeoutSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_activeProjectileCount > 0)
        {
            Debug.LogWarning($"[RocketSystem] Projectile completion timed out. Active projectiles: {_activeProjectileCount}");
            CancelActiveProjectiles();
        }
    }

    public void CancelActiveProjectiles()
    {
        if (_activeProjectiles.Count == 0)
        {
            _activeProjectileCount = 0;
            return;
        }

        RocketProjectile[] projectiles = new RocketProjectile[_activeProjectiles.Count];
        _activeProjectiles.CopyTo(projectiles);

        foreach (RocketProjectile projectile in projectiles)
        {
            if (projectile != null)
            {
                projectile.Cancel();
            }
        }

        _activeProjectiles.Clear();
        _activeProjectileCount = 0;
    }

    private void OnProjectileHitCell(int x, int y)
    {
        _onDamageRequest?.Invoke(new Vector2Int(x, y));
    }

    private GameObject GetProjectileFromPool(string prefabId)
    {
        ObjectPool<GameObject> pool = GetOrCreateProjectilePool(prefabId);
        return pool?.Get();
    }

    private ObjectPool<GameObject> GetOrCreateProjectilePool(string prefabId)
    {
        if (_projectilePools.TryGetValue(prefabId, out ObjectPool<GameObject> pool))
        {
            return pool;
        }

        GameObject prefab = _itemFactory.GetPrefab(prefabId);
        if (prefab == null)
        {
            return null;
        }

        pool = new ObjectPool<GameObject>(
            createFunc: () => Object.Instantiate(prefab, _boardParent),
            actionOnGet: projectile =>
            {
                projectile.transform.SetParent(_boardParent);
                projectile.SetActive(true);
            },
            actionOnRelease: projectile => projectile.SetActive(false),
            actionOnDestroy: projectile => Object.Destroy(projectile),
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: 32);

        _projectilePools.Add(prefabId, pool);
        return pool;
    }

    private void ReleaseProjectile(RocketProjectile projectile)
    {
        if (projectile == null) return;

        string prefabId = projectile.PoolKey;
        if (!string.IsNullOrEmpty(prefabId) && _projectilePools.TryGetValue(prefabId, out ObjectPool<GameObject> pool))
        {
            pool.Release(projectile.gameObject);
            return;
        }

        Object.Destroy(projectile.gameObject);
    }

    private void ReleaseTrackedProjectile(RocketProjectile projectile)
    {
        _activeProjectiles.Remove(projectile);
        ReleaseProjectile(projectile);
        _activeProjectileCount = Mathf.Max(0, _activeProjectileCount - 1);
    }
}
}
