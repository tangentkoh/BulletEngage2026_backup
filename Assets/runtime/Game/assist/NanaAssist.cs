using System.Collections;
using UnityEngine;

public class NanaAssist : AssistantBossBase
{
    [Header("弾プレハブ設定")]
    [Tooltip("ジャマーと同じ消失弾プレハブ (NanaJammerBullet付き)")]
    [SerializeField] private GameObject disappearingBulletPrefab;
    [Tooltip("自機狙い用直進弾プレハブ (EnemyBullet付き)")]
    [SerializeField] private GameObject aimedBulletPrefab;

    [Header("攻撃パラメータ")]
    [SerializeField] private float attackInterval = 1.5f;

    [Header("消失弾スペック")]
    [SerializeField] private float disappearingSpeed = 3.0f;
    [SerializeField] private float disappearingDamage = 5.0f;
    [Tooltip("下方向(-90度)を基準としたランダムな散開角度の幅")]
    [SerializeField] private float disappearingSpreadAngle = 35.0f;

    [Header("自機狙い直進弾スペック")]
    [SerializeField] private float aimedSpeed = 6.0f;
    [SerializeField] private float aimedDamage = 10.0f;
    [Tooltip("狙いの粗さ（度）※0で完全正確、12で±12度のブレ")]
    [SerializeField] private float aimRoughness = 12.0f;
    [Tooltip("2連射の間隔(秒)")]
    [SerializeField] private float burstDelay = 0.08f;

    private Coroutine barrageCoroutine;

    // アシストボスの初期化時に弾幕を開始
    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);
        barrageCoroutine = StartCoroutine(MultiLoversBarrageLoop());
    }

    // アシストボスの退場時に弾幕を停止
    protected override void Dismiss()
    {
        if (barrageCoroutine != null) StopCoroutine(barrageCoroutine);
        base.Dismiss();
    }

    private IEnumerator MultiLoversBarrageLoop()
    {
        // 登場直後の待機時間
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            yield return new WaitForSeconds(attackInterval);
            if (targetField == null) yield break;

            // 1. フィールド上部中央から消失弾を1発放つ
            FireDisappearingBullet();

            // 2. 自身（ボス）から少し狙撃が粗い自機狙い直進弾を2連射する
            yield return StartCoroutine(FireAimedBurst2());
        }
    }

    // --- 消失弾（1発）の発射 ---
    private void FireDisappearingBullet()
    {
        if (targetField == null || disappearingBulletPrefab == null) return;

        Rect bounds = targetField.bounds;
        Vector3 spawnPos = new Vector3(bounds.center.x, bounds.yMax - 0.5f, 0f);

        float randomAngle = Random.Range(-90f - disappearingSpreadAngle, -90f + disappearingSpreadAngle) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);

        GameObject bObj = Instantiate(disappearingBulletPrefab, spawnPos, Quaternion.identity);
        DeleteBullet bullet = bObj.GetComponent<DeleteBullet>();
        if (bullet != null)
        {
            bullet.Initialize(targetField, dir, disappearingSpeed, disappearingDamage);
        }
    }

    // --- 自機狙撃（粗め2連射）の発射 ---
    private IEnumerator FireAimedBurst2()
    {
        if (aimedBulletPrefab == null || targetField == null) yield break;

        for (int i = 0; i < 2; i++)
        {
            Transform playerT = GetTargetPlayer();
            Vector3 targetPos = (playerT != null) ? playerT.position : (transform.position + Vector3.down);
            Vector3 baseDir = (targetPos - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            // 狙いを少し粗くするため、ランダムな角度オフセットを加算
            float roughAngle = baseAngle + Random.Range(-aimRoughness, aimRoughness);
            float rad = roughAngle * Mathf.Deg2Rad;
            Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(aimedBulletPrefab, transform.position, Quaternion.identity)
                : Instantiate(aimedBulletPrefab, transform.position, Quaternion.identity);

            EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(targetField, shootDir, aimedSpeed, aimedDamage);
            }

            if (i == 0) yield return new WaitForSeconds(burstDelay);
        }
    }

    private Transform GetTargetPlayer()
    {
        if (targetField == null) return null;
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            Rect bounds = targetField.bounds;
            if (p.transform.position.x >= bounds.xMin && p.transform.position.x <= bounds.xMax)
            {
                return p.transform;
            }
        }
        return null;
    }
}