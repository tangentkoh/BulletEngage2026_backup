using System.Collections;
using UnityEngine;

public class FridaLv3 : EngageBossBase
{
    [Header("弾プレハブ")]
    [SerializeField] private GameObject curvingArrowPrefab; // カーブ矢弾
    [SerializeField] private GameObject straightArrowPrefab; // 直進矢弾

    [Header("発射ポイント")]
    [SerializeField] private Transform leftMuzzle;
    [SerializeField] private Transform rightMuzzle;

    [Header("ばらまきカーブ弾設定 (パワー依存)")]
    [SerializeField] private float baseFireInterval = 0.36f;
    [SerializeField] private float intervalReductionPerPower = 0.02f;
    [SerializeField] private int baseBulletsPerMuzzle = 2;
    [SerializeField] private float curveAngleSpeed = 45.0f;
    [SerializeField] private float rainBulletSpeed = 5.0f;

    [Header("Power 5.0 追加: 自機狙い3way設定")]
    [SerializeField] private float aimedInterval = 1.2f;
    [SerializeField] private float aimedBulletSpeed = 10f;
    [SerializeField] private float aimedSpreadAngle = 12.0f;

    private Coroutine rainCoroutine;
    private Coroutine aimedCoroutine;

    protected override void StartBarrage()
    {
        rainCoroutine = StartCoroutine(RainBarrageLoop());

        // Power 5.0 以上なら 3way を起動
        if (currentPower >= 5.0f)
        {
            aimedCoroutine = StartCoroutine(Aimed3WayLoop());
        }
    }

    protected override void StopBarrage()
    {
        if (rainCoroutine != null) StopCoroutine(rainCoroutine);
        if (aimedCoroutine != null) StopCoroutine(aimedCoroutine);
    }

    // --- 左右旋回ばらまきループ ---
    private IEnumerator RainBarrageLoop()
    {
        // 連射間隔をパワーに応じて短縮
        float currentInterval = Mathf.Max(0.06f, baseFireInterval - ((currentPower - 1.0f) * intervalReductionPerPower));

        // Power 9.0 で弾数+1
        int bulletsPerShot = (currentPower >= 9.0f) ? (baseBulletsPerMuzzle + 1) : baseBulletsPerMuzzle;

        while (true)
        {
            yield return new WaitForSeconds(currentInterval);

            // 左銃口: 反時計回り (+curveAngleSpeed)
            if (leftMuzzle != null)
            {
                FireCurvingBurst(leftMuzzle.position, curveAngleSpeed, bulletsPerShot);
            }

            // 右銃口: 時計回り (-curveAngleSpeed)
            if (rightMuzzle != null)
            {
                FireCurvingBurst(rightMuzzle.position, -curveAngleSpeed, bulletsPerShot);
            }
        }
    }

    private void FireCurvingBurst(Vector3 muzzlePos, float curveSpeed, int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 下(-90度)を基準に ±45度ばら撒き
            float angle = Random.Range(-135f, -45f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            GameObject b = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(curvingArrowPrefab, muzzlePos, Quaternion.identity)
                : Instantiate(curvingArrowPrefab, muzzlePos, Quaternion.identity);
            CurvingEnemyBullet ceb = b.GetComponent<CurvingEnemyBullet>();
            if (ceb != null)
            {
                ceb.InitializeCurving(targetField, dir, rainBulletSpeed, 15f, curveSpeed);
            }
        }
    }

    // --- Power 5.0 追加: 自機狙い3way直進弾 ---
    private IEnumerator Aimed3WayLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(aimedInterval);

            Transform playerT = GetTargetPlayer();
            Vector3 targetPos = playerT != null ? playerT.position : transform.position + Vector3.down;
            Vector3 baseDir = (targetPos - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;


            // -12度, 0度, +12度 の3way発射
            float[] angleOffsets = { -aimedSpreadAngle, 0f, aimedSpreadAngle };
            foreach (float offset in angleOffsets)
            {
                float rad = (baseAngle + offset) * Mathf.Deg2Rad;
                Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                GameObject b = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(straightArrowPrefab, transform.position, Quaternion.identity)
                    : Instantiate(straightArrowPrefab, transform.position, Quaternion.identity);
                EnemyBullet eb = b.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(targetField, shootDir, aimedBulletSpeed, 25f);
                }
            }
        }
    }

    private Transform GetTargetPlayer()
    {
        if (targetField == null) return null;
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (p.transform.position.x >= targetField.bounds.xMin && p.transform.position.x <= targetField.bounds.xMax)
            {
                return p.transform;
            }
        }
        return null;
    }
}