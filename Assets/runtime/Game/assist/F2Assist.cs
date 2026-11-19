using System.Collections;
using UnityEngine;

public class FAssist : AssistantBossBase
{
    [Header("弾設定")]
    [Tooltip("EnemyBulletコンポーネントがアタッチされた通常弾プレハブ")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private float bulletDamage = 10f;

    [Header("攻撃パターン設定 (1way / 2way 交互)")]
    [Tooltip("発射間隔 (秒)")]
    [SerializeField] private float fireInterval = 0.5f;
    [Tooltip("2way時の弾同士の角度差 (度)")]
    [SerializeField] private float twoWaySpreadAngle = 20.0f;

    [Header("定期パワー減少効果設定")]
    [Tooltip("パワー減少が発動する経過時間間隔 (秒)")]
    [SerializeField] private float drainInterval = 14.0f;
    [Tooltip("1回あたりのパワー減少量")]
    [SerializeField] private float powerDrainAmount = 0.5f;

    private Coroutine barrageCoroutine;
    private Coroutine powerDrainCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);

        barrageCoroutine = StartCoroutine(AlternateAimedShotLoop());
        powerDrainCoroutine = StartCoroutine(PeriodicPowerDrainLoop());
    }

    // --- 0.5sごとに自機狙い1wayと2way(スキマ20度)を交互発射 ---
    private IEnumerator AlternateAimedShotLoop()
    {
        yield return new WaitForSeconds(0.5f); // 登場待ち

        bool isTwoWay = false;

        while (true)
        {
            yield return new WaitForSeconds(fireInterval);
            if (targetField == null || bulletPrefab == null) yield break;

            PlayerShip targetPlayer = GetTargetPlayer();
            Vector3 targetPos = (targetPlayer != null)
                ? targetPlayer.transform.position
                : transform.position + Vector3.down;

            Vector3 baseDir = (targetPos - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            if (!isTwoWay)
            {
                // 1way: 自機方向へ真っ直ぐ発射
                SpawnBullet(baseAngle);
            }
            else
            {
                // 2way: スキマ20度（自機方向から -10度 と +10度）
                float halfSpread = twoWaySpreadAngle * 0.5f;
                SpawnBullet(baseAngle - halfSpread);
                SpawnBullet(baseAngle + halfSpread);
            }

            isTwoWay = !isTwoWay;
        }
    }

    // --- ボス出現から14s経過するごとにパワー値を0.5減少 ---
    private IEnumerator PeriodicPowerDrainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(drainInterval);
            if (targetField == null) yield break;

            PlayerShip targetPlayer = GetTargetPlayer();
            if (targetPlayer != null)
            {
                targetPlayer.ReducePower(powerDrainAmount);
            }
        }
    }

    private void SpawnBullet(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        GameObject bObj = EnemyBulletPool.Instance != null
            ? EnemyBulletPool.Instance.Get(bulletPrefab, transform.position, Quaternion.identity)
            : Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.Initialize(targetField, dir, bulletSpeed, bulletDamage);
        }
    }

    private PlayerShip GetTargetPlayer()
    {
        if (targetField == null) return null;
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (p.transform.position.x >= targetField.bounds.xMin &&
                p.transform.position.x <= targetField.bounds.xMax)
            {
                return p;
            }
        }
        return null;
    }

    protected override void Dismiss()
    {
        if (barrageCoroutine != null) StopCoroutine(barrageCoroutine);
        if (powerDrainCoroutine != null) StopCoroutine(powerDrainCoroutine);
        base.Dismiss();
    }
}