using System.Collections;
using UnityEngine;

public class PainsoltLv3 : EngageBossBase
{
    [Header("弾プレハブ (AcceleratingBullet付き)")]
    [SerializeField] private GameObject acceleratingBulletPrefab;

    [Header("連射設定 (パワー依存 & 時間経過短縮)")]
    [Tooltip("Power 1.0 時の初期連射間隔")]
    [SerializeField] private float baseFireInterval = 0.85f;

    [Tooltip("Power 1.0 ごとの初期連射短縮量")]
    [SerializeField] private float intervalReductionPerPower = 0.05f;

    [Tooltip("1秒経過ごとの連射速度上昇量")]
    [SerializeField] private float intervalReductionPerSecond = 0.01f;

    [Tooltip("連射間隔の下限")]
    [SerializeField] private float minFireInterval = 0.1f;

    [Header("同時発射数")]
    [Tooltip("初期の同時発射数")]
    [SerializeField] private int baseShotCount = 14;

    private Coroutine barrageCoroutine;

    protected override void StartBarrage()
    {
        barrageCoroutine = StartCoroutine(BarrageLoop());
    }

    protected override void StopBarrage()
    {
        if (barrageCoroutine != null)
        {
            StopCoroutine(barrageCoroutine);
        }
    }

    private IEnumerator BarrageLoop()
    {
        float timeElapsed = 0f;

        while (true)
        {
            // 1. パワー値に応じた初期連射間隔の算出 (0.85s - (Power-1.0)*0.05s)
            float powerBasedInterval = baseFireInterval - ((currentPower - 1.0f) * intervalReductionPerPower);

            // 2. 時間経過によるリアルタイム連射加速 (1秒につき0.01s短縮)
            float currentInterval = Mathf.Max(minFireInterval, powerBasedInterval - (timeElapsed * intervalReductionPerSecond));

            // 3. パワー値に応じた同時発射数
            int shotCount = baseShotCount;
            if (currentPower >= 9.0f)
            {
                shotCount += 10;
            }
            else if (currentPower >= 5.0f)
            {
                shotCount += 6;
            }

            // 4. 完全ランダム方向への加速弾ばら撒き発射
            FireRandomAcceleratingBurst(shotCount);

            yield return new WaitForSeconds(currentInterval);
            timeElapsed += currentInterval;
        }
    }

    /// <summary>
    /// 各弾を完全にランダムな方向(0〜360度)へ向けて発射
    /// </summary>
    private void FireRandomAcceleratingBurst(int count)
    {
        if (acceleratingBulletPrefab == null || targetField == null) return;

        for (int i = 0; i < count; i++)
        {
            // 0度〜360度の完全ランダムな角度
            float randomAngle = Random.Range(0f, 360f);

            // AcceleratingBullet は -transform.up 方向に進むため +90 度の補正を適用
            Quaternion rot = Quaternion.Euler(0f, 0f, randomAngle + 90f);

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(acceleratingBulletPrefab, transform.position, rot)
                : Instantiate(acceleratingBulletPrefab, transform.position, rot);

            AcceleratingBullet bullet = bObj.GetComponent<AcceleratingBullet>();
            if (bullet != null)
            {
                bullet.Initialize(targetField);
            }
        }
    }
}