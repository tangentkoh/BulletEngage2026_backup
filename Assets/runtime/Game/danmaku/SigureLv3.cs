using System.Collections;
using UnityEngine;

public class SigureLv3 : EngageBossBase
{
    [Header("弾プレハブ")]
    [SerializeField] private GameObject squareBulletPrefab; // 正方形を構成する弾
    [SerializeField] private GameObject sniperBulletPrefab; // 自機狙い弾

    [Header("正方形弾幕設定")]
    [Tooltip("正方形の1辺あたりの弾数")]
    [SerializeField] private int baseSideBulletCount = 8; // 初期密度 (計28発)
    [Tooltip("Power 9.0 到達時の1辺あたりの弾数")]
    [SerializeField] private int maxPowerSideBulletCount = 10; // 高密度 (計36発)

    [Tooltip("Power 1.0 時の連射間隔 (秒)")]
    [SerializeField] private float baseFireInterval = 0.74f;
    [Tooltip("Power 1.0 上昇ごとの間隔短縮量 (秒)")]
    [SerializeField] private float intervalReductionPerPower = 0.08f;

    [Tooltip("正方形の基準速度 (辺の中央の速度)")]
    [SerializeField] private float baseExpansionSpeed = 4.0f;
    [Tooltip("1波ごとの正方形の回転角 (度)")]
    [SerializeField] private float rotationAngleStep = 15f;

    [Header("Power 5.0 追加: 自機狙い1way設定")]
    [SerializeField] private float sniperInterval = 1.4f;
    [SerializeField] private float sniperBulletSpeed = 10f;
    [SerializeField] private float sniperDamage = 60f;

    private float currentRotationAngle = 0f;
    private Coroutine squareBarrageCoroutine;
    private Coroutine sniperCoroutine;

    protected override void StartBarrage()
    {
        squareBarrageCoroutine = StartCoroutine(SquareBarrageLoop());

        // Power 5.0 到達で自機狙い1wayを開始
        if (currentPower >= 5.0f)
        {
            sniperCoroutine = StartCoroutine(SniperLoop());
        }
    }

    protected override void StopBarrage()
    {
        if (squareBarrageCoroutine != null) StopCoroutine(squareBarrageCoroutine);
        if (sniperCoroutine != null) StopCoroutine(sniperCoroutine);
    }

    // --- 正方形弾幕ループ ---
    private IEnumerator SquareBarrageLoop()
    {
        // パワーに応じた連射間隔を算出
        float currentInterval = Mathf.Max(0.2f, baseFireInterval - ((currentPower - 1.0f) * intervalReductionPerPower));

        // Power 9.0 で密度増加
        int sideBullets = (currentPower >= 9.0f) ? maxPowerSideBulletCount : baseSideBulletCount;

        while (true)
        {
            FireSquareWave(sideBullets);

            // 次の波に向けて角度を少し回す
            currentRotationAngle = (currentRotationAngle + rotationAngleStep) % 360f;

            yield return new WaitForSeconds(currentInterval);
        }
    }

    // 正方形の4辺からベクトルを算出して発射
    private void FireSquareWave(int sideCount)
    {
        if (squareBulletPrefab == null || targetField == null) return;

        // 正方形の一辺の半分の長さを 1.0 とする
        // (-1, 1) -> (1, 1) -> (1, -1) -> (-1, -1) の枠線上をサンプリング
        Vector2[] corners = new Vector2[]
        {
            new Vector2(-1f,  1f), // 左上
            new Vector2( 1f,  1f), // 右上
            new Vector2( 1f, -1f), // 右下
            new Vector2(-1f, -1f)  // 左下
        };

        Quaternion waveRot = Quaternion.Euler(0, 0, currentRotationAngle);

        // 4つの辺を順に走査
        for (int side = 0; side < 4; side++)
        {
            Vector2 startCorner = corners[side];
            Vector2 endCorner = corners[(side + 1) % 4];

            // 頂点の重複を防ぐため sideCount - 1 回ステップ
            for (int i = 0; i < sideCount - 1; i++)
            {
                float t = (float)i / (sideCount - 1);
                Vector2 localEdgePoint = Vector2.Lerp(startCorner, endCorner, t);

                // 中心(0,0)から辺上の点へのベクトル
                // 角(1, 1)の長さは約 1.414、辺の中央(0, 1)の長さは 1.0 になるため
                // このベクトルをそのまま方向×速度として利用すると完全な正方形を保って広がる
                Vector3 worldDir = waveRot * (Vector3)localEdgePoint;
                float bulletSpeed = worldDir.magnitude * baseExpansionSpeed;
                Vector3 moveDir = worldDir.normalized;

                GameObject bulletObj = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(squareBulletPrefab, transform.position, Quaternion.identity)
                    : Instantiate(squareBulletPrefab, transform.position, Quaternion.identity);
                EnemyBullet eb = bulletObj.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(targetField, moveDir, bulletSpeed, 15f);
                }
            }
        }
    }

    // --- Power 5.0 追加: 自機狙い1wayスナイプループ ---
    private IEnumerator SniperLoop()
    {
        GameObject bulletToUse = sniperBulletPrefab != null ? sniperBulletPrefab : squareBulletPrefab;

        while (true)
        {
            yield return new WaitForSeconds(sniperInterval);

            Transform playerTransform = GetTargetPlayer();
            Vector3 targetPos = (playerTransform != null) ? playerTransform.position : transform.position + Vector3.down;
            Vector3 shootDir = (targetPos - transform.position).normalized;

            GameObject b = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(bulletToUse, transform.position, Quaternion.identity)
                : Instantiate(bulletToUse, transform.position, Quaternion.identity);
            EnemyBullet eb = b.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(targetField, shootDir, sniperBulletSpeed, sniperDamage);
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