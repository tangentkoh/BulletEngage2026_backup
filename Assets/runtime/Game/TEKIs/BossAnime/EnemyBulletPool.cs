using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyBulletPool : MonoBehaviour
{
    public static EnemyBulletPool Instance { get; private set; }

    // プレハブごとにプールを自動生成して辞書管理
    private readonly Dictionary<GameObject, IObjectPool<GameObject>> poolDict = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!poolDict.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: obj => {
                    if (obj) obj.SetActive(true);
                },
                actionOnRelease: obj => {
                    if (obj) obj.SetActive(false);
                },
                actionOnDestroy: obj => {
                    if (obj) Destroy(obj);
                },
                defaultCapacity: 60,
                maxSize: 600
            );
            poolDict[prefab] = pool;
        }

        GameObject bullet = pool.Get();
        bullet.transform.SetPositionAndRotation(position, rotation);

        // 1. IPooledBullet 実装弾へのプール登録 (カーブ弾・加速弾など)
        if (bullet.TryGetComponent<IPooledBullet>(out var pooledBullet))
        {
            pooledBullet.SetPool(pool);
        }
        // 2. 既存の EnemyBullet への後方互換対応
        else if (bullet.TryGetComponent<EnemyBullet>(out var eb))
        {
            eb.SetPool(pool);
        }

        return bullet;
    }

    
    // シーン遷移時やゲーム終了時に全プールを破棄・クリーンアップ
    public void ClearAllPools()
    {
        foreach (var pool in poolDict.Values)
        {
            pool.Clear();
        }
        poolDict.Clear();
    }
}

// プールに登録可能な弾のインターフェース
public interface IPooledBullet
{
    void SetPool(IObjectPool<GameObject> pool);
    void ReleaseToPool();
}