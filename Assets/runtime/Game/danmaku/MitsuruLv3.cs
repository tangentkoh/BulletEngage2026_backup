using System.Collections;
using UnityEngine;

public class MitsuruLv3 : EngageBossBase
{
    [Header("弾プレハブ設定")]
    [SerializeField] private GameObject bigBulletPrefab;    // 大玉 (威力20)
    [SerializeField] private GameObject mediumBulletPrefab; // 中玉 (威力15)

    [Header("大玉バースト弾幕設定")]
    [SerializeField] private float bigBulletDamage = 20f;
    [SerializeField] private float bigBulletSpeed = 5f;
    [SerializeField] private float spawnRadius = 1.0f;       // ボス周囲の生成半径 (1f程度)
    [SerializeField] private float aimRoughnessAngle = 15f;  // 粗めの狙いブレ角度 (±15度)
    [SerializeField] private float burstFireInterval = 0.1f; // バースト内の連射間隔

    [Header("Power 5.0: 回転8方向中玉弾幕設定")]
    [SerializeField] private float ringBulletDamage = 15f;
    [SerializeField] private float ringBulletSpeed = 6f;
    [SerializeField] private float ringFireInterval = 0.5f; // リング弾の発射間隔
    [SerializeField] private float rotationStepAngle = 10f;  // 1ウェーブごとの左回転角

    private Coroutine mainBurstCoroutine;
    private Coroutine ringBarrageCoroutine;

    // パワー値からバースト持続時間を計算 (初期1.0s + 0.1s * (power - 1.0), Max 1.8s)
    private float BurstDuration => Mathf.Min(2.0f, 1.2f + (currentPower - 1.0f) * 0.1f);

    // パワー値9.0でインターバル半減 (通常1.0s -> 0.5s)
    private float BurstCooldown => (currentPower >= 9.0f) ? 0.4f : 0.8f;

    protected override void StartBarrage()
    {
        mainBurstCoroutine = StartCoroutine(BigBulletBurstLoop());

        // Power 5.0 以上で回転8方向リング弾幕を並行展開
        if (currentPower >= 5.0f)
        {
            ringBarrageCoroutine = StartCoroutine(RotatingRingLoop());
        }
    }

    protected override void StopBarrage()
    {
        if (mainBurstCoroutine != null)
        {
            StopCoroutine(mainBurstCoroutine);
            mainBurstCoroutine = null;
        }

        if (ringBarrageCoroutine != null)
        {
            StopCoroutine(ringBarrageCoroutine);
            ringBarrageCoroutine = null;
        }
    }

    // --- メイン大玉バースト連射ループ ---
    private IEnumerator BigBulletBurstLoop()
    {
        yield return new WaitForSeconds(0.4f);

        while (!isDead && !isRetreating)
        {
            float burstTimer = 0f;
            float currentBurstDuration = BurstDuration;

            // バースト連射フェーズ
            while (burstTimer < currentBurstDuration)
            {
                SpawnAimedBigBullet();
                yield return new WaitForSeconds(burstFireInterval);
                burstTimer += burstFireInterval;
            }

            // インターバルフェーズ (Power 9.0 で半減)
            yield return new WaitForSeconds(BurstCooldown);
        }
    }

    private void SpawnAimedBigBullet()
    {
        if (bigBulletPrefab == null || targetField == null) return;

        // ボス周囲 1f 以内のランダムな位置に出現
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + (Vector3)randomOffset;

        // 相手自機の位置を取得
        Transform targetPlayer = GetTargetPlayer();
        Vector3 baseDir = Vector3.down;
        if (targetPlayer != null)
        {
            baseDir = (targetPlayer.position - spawnPos).normalized;
        }

        // 狙いに粗めのブレ角度を付与
        float angleOffset = Random.Range(-aimRoughnessAngle, aimRoughnessAngle);
        Vector3 shootDir = Quaternion.Euler(0f, 0f, angleOffset) * baseDir;

        GameObject b = EnemyBulletPool.Instance != null
            ? EnemyBulletPool.Instance.Get(bigBulletPrefab, spawnPos, Quaternion.identity)
            : Instantiate(bigBulletPrefab, spawnPos, Quaternion.identity);
        EnemyBullet eb = b.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.Initialize(targetField, shootDir, bigBulletSpeed, bigBulletDamage);
        }
    }

    // --- Power 5.0: 8方向左回転中玉ループ ---
    private IEnumerator RotatingRingLoop()
    {
        yield return new WaitForSeconds(1.0f);
        float currentAngle = 0f;

        while (!isDead && !isRetreating)
        {
            if (mediumBulletPrefab != null && targetField != null)
            {
                float step = 360f / 8;
                for (int i = 0; i < 8; i++)
                {
                    float angle = currentAngle + (i * step);
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                    GameObject b = EnemyBulletPool.Instance != null
                        ? EnemyBulletPool.Instance.Get(mediumBulletPrefab, transform.position, Quaternion.identity)
                        : Instantiate(mediumBulletPrefab, transform.position, Quaternion.identity);
                    EnemyBullet eb = b.GetComponent<EnemyBullet>();
                    if (eb != null)
                    {
                        eb.Initialize(targetField, dir, ringBulletSpeed, ringBulletDamage);
                    }
                }

                // 左回転（正の角度加算）
                currentAngle = (currentAngle + rotationStepAngle) % 360f;
            }

            yield return new WaitForSeconds(ringFireInterval);
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