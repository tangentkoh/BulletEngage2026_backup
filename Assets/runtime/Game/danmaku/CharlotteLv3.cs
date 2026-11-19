using System.Collections;
using UnityEngine;

public class CharlotteLv3 : EngageBossBase
{
    [Header("弾プレハブ")]
    [SerializeField] private GameObject ringBulletPrefab;

    [Header("リング弾幕基本設定")]
    [Tooltip("Power 1.0 時の基本連射間隔 (秒)")]
    [SerializeField] private float baseFireInterval = 0.48f;
    [Tooltip("Power 1.0 ごとの連射短縮量 (秒)")]
    [SerializeField] private float intervalReductionPerPower = 0.04f;

    [Header("リング（ドーナツ）形状設定")]
    [Tooltip("Power 1.0 時の1リングあたりの構成弾数 (初期値: 8)")]
    [SerializeField] private int baseRingBulletCount = 8;
    [Tooltip("ドーナツ（円）の半径")]
    [SerializeField] private float ringRadius = 1.0f;
    [SerializeField] private float bulletSpeed = 5.5f;
    [SerializeField] private float bulletDamage = 10f;

    private Coroutine ringBarrageCoroutine;

    protected override void StartBarrage()
    {
        ringBarrageCoroutine = StartCoroutine(RingBarrageLoop());
    }

    protected override void StopBarrage()
    {
        if (ringBarrageCoroutine != null)
        {
            StopCoroutine(ringBarrageCoroutine);
        }
    }

    private IEnumerator RingBarrageLoop()
    {
        while (true)
        {
            // 1. 連射間隔の算出 (初期0.48s、Power 1.0ごとに -0.04s)
            float currentInterval = Mathf.Max(0.08f, baseFireInterval - ((currentPower - 1.0f) * intervalReductionPerPower));

            // 2. 1リングあたりの弾数の算出 (初期8個、Power 5.0で+2個(10個)、Power 9.0でさらに+2個(12個))
            int ringBulletCount = baseRingBulletCount;
            if (currentPower >= 9.0f)
            {
                ringBulletCount += 4; // 計12個
            }
            else if (currentPower >= 5.0f)
            {
                ringBulletCount += 2; // 計10個
            }

            // 3. 同時に2セットのリング（ドーナツ）を打つ
            // それぞれランダムな方向へ飛ばす
            FireSingleDoughnutRing(ringBulletCount);
            FireSingleDoughnutRing(ringBulletCount);

            yield return new WaitForSeconds(currentInterval);
        }
    }

    /// <summary>
    /// 円形（ドーナツ状）に配置された弾群を、形を維持したまま1つの方向へ打ち出す
    /// </summary>
    private void FireSingleDoughnutRing(int bulletCount)
    {
        if (ringBulletPrefab == null || targetField == null) return;

        // リング全体の進行方向をランダムに決定
        float moveAngle = Random.Range(-225f, 45f) * Mathf.Deg2Rad;
        Vector3 moveDir = new Vector3(Mathf.Cos(moveAngle), Mathf.Sin(moveAngle), 0f).normalized;

        // ボス位置を中心とした円周上に各弾を配置
        float angleStep = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            // 円周上のオフセット位置
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
            Vector3 spawnPos = transform.position + offset;

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(ringBulletPrefab, spawnPos, Quaternion.identity)
                : Instantiate(ringBulletPrefab, spawnPos, Quaternion.identity);

            EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                // 全弾が「共通の進行方向(moveDir)」を持つため、円形の形を崩さずに移動する
                eb.Initialize(targetField, moveDir, bulletSpeed, bulletDamage);
            }
        }
    }
}