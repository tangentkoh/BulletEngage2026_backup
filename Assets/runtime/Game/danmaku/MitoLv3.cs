using System.Collections;
using UnityEngine;

public class MitoLv3 : EngageBossBase
{
    [Header("弾プレハブ (CurvingEnemyBullet付き)")]
    [SerializeField] private GameObject curvingBulletPrefab;

    [Header("基礎スペック")]
    [SerializeField] private int baseShotCount = 10;             // 同時発射数
    [SerializeField] private float baseFireInterval = 0.26f;    // 連射間隔
    [SerializeField] private float baseActiveTime = 4.0f;        // クールタイムまでの射撃時間
    [SerializeField] private float restTime = 1.0f;              // 休息時間

    [Header("弾パラメータ")]
    [SerializeField] private float bulletSpeed = 4.0f;           // 遅めの弾速
    [SerializeField] private float bulletDamage = 12.0f;
    [SerializeField] private float curveAngleSpeed = 30.0f;      // 弾の曲がり速度 (度/秒)
    [SerializeField] private float spiralRotationSpeed = 110.0f; // 渦巻きの回転速度 (度/秒)

    private Coroutine spiralCoroutine;

    protected override void StartBarrage()
    {
        spiralCoroutine = StartCoroutine(SpiralBarrageLoop());
    }

    protected override void StopBarrage()
    {
        if (spiralCoroutine != null)
        {
            StopCoroutine(spiralCoroutine);
        }
    }

    private IEnumerator SpiralBarrageLoop()
    {
        bool isClockwise = true; // 回転方向フラグ
        float currentSpiralAngle = 0f;

        while (true)
        {
            // --- パワー値に応じた各種パラメータ計算 ---
            // 1. 連射速度(間隔): 1.0につき 0.02f 減少 (初期0.26s)
            float fireInterval = Mathf.Max(0.08f, baseFireInterval - ((currentPower - 1.0f) * 0.02f));

            // 2. クールタイム(射撃時間): Power 5.0 以上で 1.0s 増加 (初期4.0s -> 5.0s)
            float activeDuration = (currentPower >= 5.0f) ? (baseActiveTime + 1.0f) : baseActiveTime;

            // 3. 同時発射数: Power 9.0 以上で 6 個増加 (初期10個 -> 16個)
            int shotCount = (currentPower >= 9.0f) ? (baseShotCount + 6) : baseShotCount;

            // 回転方向に応じた符号 (+1: 時計回り / -1: 反時計回り)
            float dirSign = isClockwise ? 1.0f : -1.0f;
            float currentCurveSpeed = curveAngleSpeed * dirSign; // 弾も同じ方向へ曲げる

            float activeTimer = 0f;

            // --- 射撃フェーズ ---
            while (activeTimer < activeDuration)
            {
                // 全方位弾の射出
                FireSpiralBurst(shotCount, currentSpiralAngle, currentCurveSpeed);

                // 渦巻きを形成するために角度を少しずつ回転
                currentSpiralAngle += spiralRotationSpeed * dirSign * fireInterval;
                if (currentSpiralAngle >= 360f) currentSpiralAngle -= 360f;
                else if (currentSpiralAngle < 0f) currentSpiralAngle += 360f;

                yield return new WaitForSeconds(fireInterval);
                activeTimer += fireInterval;
            }

            // --- 1秒間の休息フェーズ ---
            yield return new WaitForSeconds(restTime);

            // 回転方向の反転
            isClockwise = !isClockwise;
        }
    }

    private void FireSpiralBurst(int count, float startAngle, float curveSpeed)
    {
        if (curvingBulletPrefab == null || targetField == null) return;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (i * angleStep);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(curvingBulletPrefab, transform.position, Quaternion.identity)
                : Instantiate(curvingBulletPrefab, transform.position, Quaternion.identity);

            CurvingEnemyBullet ceb = bObj.GetComponent<CurvingEnemyBullet>();
            if (ceb != null)
            {
                ceb.InitializeCurving(targetField, dir, bulletSpeed, bulletDamage, curveSpeed);
            }
        }
    }
}