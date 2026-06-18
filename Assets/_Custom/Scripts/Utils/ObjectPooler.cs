using UnityEngine;
using UnityEngine.Pool;

public class ObjectPooler<T> where T : Component
{
    private ObjectPool<T> pool;
    private T prefab;

    public ObjectPooler(T prefab, int defaultCapacity = 20, int maxSize = 100)
    {
        this.prefab = prefab;
        pool = new ObjectPool<T>(
            createFunc: CreateObject,
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroyObject,
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private T CreateObject()
    {
        return Object.Instantiate(prefab);
    }

    private void OnGetObject(T obj)
    {
        obj.gameObject.SetActive(true);
    }

    private void OnReleaseObject(T obj)
    {
        obj.gameObject.SetActive(false);
    }

    private void OnDestroyObject(T obj)
    {
        Object.Destroy(obj.gameObject);
    }

    public T Get()
    {
        return pool.Get();
    }

    public void Release(T obj)
    {
        pool.Release(obj);
    }
}
