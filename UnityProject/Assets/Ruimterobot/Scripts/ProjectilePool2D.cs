using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

/// <summary>
/// Reuses player and boss projectiles so rapid-fire does not constantly create
/// and destroy physics objects. Pools are cleared when a level changes.
/// </summary>
public static class ProjectilePool2D
{
    private static readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private static string poolSceneKey;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPools()
    {
        foreach (ObjectPool<GameObject> pool in pools.Values)
            pool.Clear();
        pools.Clear();
        poolSceneKey = null;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        ClearWhenSceneChanges();
        if (!pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            pool = CreatePool(prefab);
            pools.Add(prefab, pool);
        }

        GameObject projectile = pool.Get();
        if (projectile == null)
            return null;

        projectile.transform.SetPositionAndRotation(position, rotation);
        return projectile;
    }

    private static ObjectPool<GameObject> CreatePool(GameObject prefab)
    {
        ObjectPool<GameObject> pool = null;
        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject instance = Object.Instantiate(prefab);
                instance.name = prefab.name + " (Pooled)";
                instance.SetActive(false);

                Projectile2D playerProjectile = instance.GetComponent<Projectile2D>();
                if (playerProjectile != null)
                    playerProjectile.SetPool(pool);

                BossProjectile2D bossProjectile = instance.GetComponent<BossProjectile2D>();
                if (bossProjectile != null)
                    bossProjectile.SetPool(pool);

                return instance;
            },
            actionOnGet: instance =>
            {
                if (instance != null)
                    instance.SetActive(true);
            },
            actionOnRelease: instance =>
            {
                if (instance == null)
                    return;

                instance.transform.SetParent(null);
                instance.SetActive(false);
            },
            actionOnDestroy: instance =>
            {
                if (instance != null)
                    Object.Destroy(instance);
            },
            collectionCheck: true,
            defaultCapacity: 8,
            maxSize: 64);

        return pool;
    }

    private static void ClearWhenSceneChanges()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string activeSceneKey = string.IsNullOrEmpty(activeScene.path) ? activeScene.name : activeScene.path;
        if (poolSceneKey == activeSceneKey)
            return;

        foreach (ObjectPool<GameObject> pool in pools.Values)
            pool.Clear();
        pools.Clear();
        poolSceneKey = activeSceneKey;
    }
}
