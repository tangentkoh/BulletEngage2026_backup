using System.Collections;
using UnityEngine;

public class PainsoltAssist : AssistantBossBase
{
    [Header("バウンドフルーツ弾プレハブ")]
    [SerializeField] private GameObject bouncingFruitBulletPrefab;

    [Header("連射パラメータ (徐々に加速)")]
    [Tooltip("初期連射間隔 (初期値: 2.0s)")]
    [SerializeField] private float initialInterval = 2.0f;
    [Tooltip("1秒経過ごとの連射短縮量 (0.04s/s)")]
    [SerializeField] private float intervalReductionPerSecond = 0.04f;
    [Tooltip("連射間隔の下限")]
    [SerializeField] private float minInterval = 0.2f;

    [Header("弾パラメータ (コラボスキル専用調整)")]
    [SerializeField] private float bulletSpeed = 7.0f;
    [SerializeField] private float bulletDamage = 10.0f;
    [Tooltip("壁での反射回数")]
    [SerializeField] private int bounceCount = 2;

    private Coroutine throwCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);
        throwCoroutine = StartCoroutine(FruitThrowLoop());
    }

    protected override void Dismiss()
    {
        if (throwCoroutine != null) StopCoroutine(throwCoroutine);
        base.Dismiss();
    }

    private IEnumerator FruitThrowLoop()
    {
        yield return new WaitForSeconds(0.5f); // 登場フェードイン待機

        float timeElapsed = 0f;

        while (true)
        {
            // 1. 時間経過によるリアルタイム連射加速 (1秒あたり0.04s短縮)
            float currentInterval = Mathf.Max(minInterval, initialInterval - (timeElapsed * intervalReductionPerSecond));

            if (targetField != null && bouncingFruitBulletPrefab != null)
            {
                // 2. 相手プレイヤーの方向を取得して狙いを定める (いなければ真下)
                Transform playerT = GetTargetPlayer();
                Vector3 targetPos = (playerT != null) ? playerT.position : (transform.position + Vector3.down);
                Vector3 shootDir = (targetPos - transform.position).normalized;

                // 3. ボス本体位置から狙撃反射弾を生成
                GameObject bObj = Instantiate(bouncingFruitBulletPrefab, transform.position, Quaternion.identity);
                BouncingFruitBullet bullet = bObj.GetComponent<BouncingFruitBullet>();
                if (bullet != null)
                {
                    bullet.Initialize(targetField, shootDir, bulletSpeed, bulletDamage, bounceCount);
                }
            }

            yield return new WaitForSeconds(currentInterval);
            timeElapsed += currentInterval;
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