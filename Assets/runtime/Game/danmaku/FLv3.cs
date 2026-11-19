using System.Collections;
using UnityEngine;

public class FLv3 : EngageBossBase
{
    [Header("技名 & 弾プレハブ")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("連射速度設定 (パワー依存)")]
    [Tooltip("Power 1.0 時の発射間隔 (秒)")]
    [SerializeField] private float baseFireInterval = 0.5f;
    [Tooltip("Power 1.0 上昇ごとの間隔短縮量 (秒)")]
    [SerializeField] private float intervalReductionPerPower = 0.04f;
    [Tooltip("発射間隔の下限安全値 (秒)")]
    [SerializeField] private float minFireInterval = 0.08f;

    [Header("弾数設定 (パワー依存)")]
    [Tooltip("初期の各辺ごとの同時発射数")]
    [SerializeField] private int baseBulletsPerSide = 3;

    [Header("弾スペック (等速直進)")]
    [SerializeField] private float bulletSpeed = 4.5f;
    [SerializeField] private float bulletDamage = 20f;

    [Header("出現位置マージン設定")]
    [Tooltip("画面端からの内側オフセット")]
    [SerializeField] private float spawnEdgeOffset = 0.1f;
    [Tooltip("角付近のランダム範囲マージン")]
    [SerializeField] private float cornerMargin = 0.3f;

    private Coroutine attackCoroutine;

    protected override void StartBarrage()
    {
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(TruthBarrageRoutine());
    }

    protected override void StopBarrage()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    private IEnumerator TruthBarrageRoutine()
    {
        if (targetField == null || bulletPrefab == null) yield break;

        // 1. Power 1.0ごとに連射間隔を 0.04s 短縮 (初期 0.5s)
        float fireInterval = Mathf.Max(
            minFireInterval,
            baseFireInterval - ((currentPower - 1.0f) * intervalReductionPerPower)
        );

        // 2. Power 3.0 と 7.0 で端ごとの弾数が1ずつ増加 (初期3個 -> 4個 -> 5個)
        int bulletsPerSide = baseBulletsPerSide;
        if (currentPower >= 3.0f) bulletsPerSide++;
        if (currentPower >= 7.0f) bulletsPerSide++;

        // 3. Power 5.0 で右側追加、9.0 で下側追加 (初期は左側と上側のみ)
        bool enableRightSide = currentPower >= 5.0f;
        bool enableBottomSide = currentPower >= 9.0f;

        while (!IsDead && targetField != null)
        {
            // 上端から真下へ
            SpawnEdgeBullets(EdgeSide.Top, bulletsPerSide);

            // 左端から真右へ
            SpawnEdgeBullets(EdgeSide.Left, bulletsPerSide);

            // 右端から真左へ (Power 5.0以上)
            if (enableRightSide)
            {
                SpawnEdgeBullets(EdgeSide.Right, bulletsPerSide);
            }

            // 下端から真上へ (Power 9.0以上)
            if (enableBottomSide)
            {
                SpawnEdgeBullets(EdgeSide.Bottom, bulletsPerSide);
            }

            yield return new WaitForSeconds(fireInterval);
        }
    }

    private enum EdgeSide { Top, Left, Right, Bottom }

    private void SpawnEdgeBullets(EdgeSide side, int count)
    {
        Rect bounds = targetField.bounds;

        float xMin = bounds.xMin + cornerMargin;
        float xMax = bounds.xMax - cornerMargin;
        float yMin = bounds.yMin + cornerMargin;
        float yMax = bounds.yMax - cornerMargin;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            Vector3 direction = Vector3.down;
            Quaternion rotation = Quaternion.identity;

            switch (side)
            {
                case EdgeSide.Top:
                    spawnPos = new Vector3(Random.Range(xMin, xMax), bounds.yMax - spawnEdgeOffset, 0f);
                    direction = Vector3.down;
                    rotation = Quaternion.Euler(0f, 0f, 180f);
                    break;

                case EdgeSide.Left:
                    spawnPos = new Vector3(bounds.xMin + spawnEdgeOffset, Random.Range(yMin, yMax), 0f);
                    direction = Vector3.right;
                    rotation = Quaternion.Euler(0f, 0f, -90f);
                    break;

                case EdgeSide.Right:
                    spawnPos = new Vector3(bounds.xMax - spawnEdgeOffset, Random.Range(yMin, yMax), 0f);
                    direction = Vector3.left;
                    rotation = Quaternion.Euler(0f, 0f, 90f);
                    break;

                case EdgeSide.Bottom:
                    spawnPos = new Vector3(Random.Range(xMin, xMax), bounds.yMin + spawnEdgeOffset, 0f);
                    direction = Vector3.up;
                    rotation = Quaternion.identity;
                    break;
            }

            GameObject b = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(bulletPrefab, spawnPos, rotation)
                : Instantiate(bulletPrefab, spawnPos, rotation);

            EnemyBullet eb = b.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(targetField, direction, bulletSpeed, bulletDamage);
            }
        }
    }
}