using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public static class ParticlePool
{
    private static readonly Dictionary<int, ObjectPool<ParticleSystem>> Pools = new Dictionary<int, ObjectPool<ParticleSystem>>();

    public static ParticleSystem Get(ParticleSystem prefab, Transform parent)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        if (!Pools.TryGetValue(key, out ObjectPool<ParticleSystem> pool))
        {
            pool = CreatePool(prefab);
            Pools.Add(key, pool);
        }

        ParticleSystem instance = pool.Get();
        instance.transform.SetParent(parent);
        instance.gameObject.SetActive(true);

        PooledParticleReturn returner = instance.GetComponent<PooledParticleReturn>();
        if (returner == null)
        {
            returner = instance.gameObject.AddComponent<PooledParticleReturn>();
        }
        returner.Bind(pool);

        return instance;
    }

    private static ObjectPool<ParticleSystem> CreatePool(ParticleSystem prefab)
    {
        return new ObjectPool<ParticleSystem>(
            createFunc: () => Object.Instantiate(prefab),
            actionOnGet: instance => instance.gameObject.SetActive(true),
            actionOnRelease: instance => instance.gameObject.SetActive(false),
            actionOnDestroy: instance => Object.Destroy(instance.gameObject),
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: 64);
    }
}

public sealed class PooledParticleReturn : MonoBehaviour
{
    private ObjectPool<ParticleSystem> _pool;
    private ParticleSystem _particleSystem;

    public void Bind(ObjectPool<ParticleSystem> pool)
    {
        _pool = pool;
        if (_particleSystem == null)
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }
    }

    private void OnParticleSystemStopped()
    {
        if (_pool == null || _particleSystem == null)
        {
            Destroy(gameObject);
            return;
        }

        _pool.Release(_particleSystem);
    }
}
