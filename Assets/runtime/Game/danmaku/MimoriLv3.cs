using System.Collections;
using UnityEngine;

public class MimoriLv3 : EngageBossBase
{
    [Header("弾プレハブ (EnemyBullet付き)")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("基礎スペック")]
    [SerializeField] private int baseMaxWayCount = 7;             // 基本最大way数
    [SerializeField] private float baseCooldown = 1.2f;            // 打ち終わりの基本クールタイム
    [SerializeField] private float cooldownReductionPerPower = 0.1f; // Power 1.0ごとの短縮量
    [SerializeField] private float wayFireInterval = 0.1f;       // 連射速度
    [SerializeField] private float bulletSpeed = 4.5f;            // 少し早めの弾速
    [SerializeField] private float bulletDamage = 15.0f;
    [SerializeField] private float waySpreadAngle = 15.0f;         // way間の扇形拡がり角度

    [Header("Power 9.0 追加: 全方位弾幕設定")]
    [SerializeField] private float ringInterval = 1.0f;
    [SerializeField] private int ringBulletCount = 18;

    private Coroutine triangleBarrageCoroutine;
    private Coroutine ringBarrageCoroutine;

    protected override void StartBarrage()
    {
        triangleBarrageCoroutine = StartCoroutine(TriangleWaveLoop());

        // Power 9.0 以上で全方位(円型)弾幕を併発
        if (currentPower >= 9.0f)
        {
            ringBarrageCoroutine = StartCoroutine(RingWaveLoop());
        }
    }

    protected override void StopBarrage()
    {
        if (triangleBarrageCoroutine != null) StopCoroutine(triangleBarrageCoroutine);
        if (ringBarrageCoroutine != null) StopCoroutine(ringBarrageCoroutine);
    }

    private IEnumerator TriangleWaveLoop()
    {
        while (true)
        {
            // --- パワー値に応じたパラメータ計算 ---
            // 1. 最大way数: 初期7 (Power 5.0で+2 -> 9, Power 9.0でさらに+4 -> 13)
            int maxWay = baseMaxWayCount;
            if (currentPower >= 5.0f) maxWay += 2;
            if (currentPower >= 9.0f) maxWay += 4;

            // 2. クールタイム: 1.2s - (Power - 1.0) * 0.1s
            float cooldown = Mathf.Max(0.2f, baseCooldown - ((currentPower - 1.0f) * cooldownReductionPerPower));

            // ★一連のループ（1way〜maxWay）が終わるまで基準位置・角度を固定
            Transform playerT = GetTargetPlayer();
            Vector3 targetPos = (playerT != null) ? playerT.position : (transform.position + Vector3.down);
            Vector3 baseDir = (targetPos - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            // 1way から maxWay まで 0.05s 間隔で連射 (三角形を形成)
            for (int wayCount = 1; wayCount <= maxWay; wayCount++)
            {
                FireWayBurst(baseAngle, wayCount);
                yield return new WaitForSeconds(wayFireInterval);
            }

            // 打ち終わり後のクールタイム
            yield return new WaitForSeconds(cooldown);
        }
    }

    /// <summary>
    /// 指定角度を中心に wayCount 分の扇形弾幕を発射
    /// </summary>
    private void FireWayBurst(float centerAngle, int wayCount)
    {
        if (bulletPrefab == null || targetField == null) return;

        // 扇形の開始角度計算
        float startAngle = centerAngle - (waySpreadAngle * (wayCount - 1) / 2.0f);

        for (int i = 0; i < wayCount; i++)
        {
            float angle = startAngle + (i * waySpreadAngle);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(bulletPrefab, transform.position, Quaternion.identity)
                : Instantiate(bulletPrefab, transform.position, Quaternion.identity);

            EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(targetField, shootDir, bulletSpeed, bulletDamage);
            }
        }
    }

    private IEnumerator RingWaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(ringInterval);

            if (bulletPrefab != null && targetField != null)
            {
                float angleStep = 360f / ringBulletCount;
                for (int i = 0; i < ringBulletCount; i++)
                {
                    float angle = i * angleStep;
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                    GameObject bObj = EnemyBulletPool.Instance != null
                        ? EnemyBulletPool.Instance.Get(bulletPrefab, transform.position, Quaternion.identity)
                        : Instantiate(bulletPrefab, transform.position, Quaternion.identity);

                    EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
                    if (eb != null)
                    {
                        eb.Initialize(targetField, shootDir, bulletSpeed, bulletDamage);
                    }
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