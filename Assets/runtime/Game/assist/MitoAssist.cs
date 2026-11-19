using System.Collections;
using UnityEngine;

public class MitoAssist : AssistantBossBase
{
    [Header("加速弾プレハブ (AcceleratingBullet付き)")]
    [SerializeField] private GameObject acceleratingBulletPrefab;

    [Header("攻撃設定")]
    [SerializeField] private float fireInterval = 0.5f;       // 0.5秒ごとに発射
    [SerializeField] private float spawnOffsetRangeX = 1.5f; // ボス周辺の生成幅

    private Coroutine poisonCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);
        poisonCoroutine = StartCoroutine(PoisonDropLoop());
    }

    protected override void Dismiss()
    {
        if (poisonCoroutine != null) StopCoroutine(poisonCoroutine);
        base.Dismiss();
    }

    private IEnumerator PoisonDropLoop()
    {
        yield return new WaitForSeconds(0.5f); // 登場フェードイン待機

        // 真下 (-transform.up) を向ける回転
        Quaternion downwardRot = Quaternion.Euler(0f, 0f, 0f);

        while (true)
        {
            yield return new WaitForSeconds(fireInterval);

            if (targetField != null && acceleratingBulletPrefab != null)
            {
                // ボス周辺の左右位置から3つの加速毒弾を生成して落とす
                for (int i = 0; i < 3; i++)
                {
                    // 1つ目と3つ目はボスの右側周辺、2つ目はボスの左側周辺に生成する
                    float offsetX = (i == 1)
                        ? Random.Range(-spawnOffsetRangeX, -0.2f)
                        : Random.Range(0.2f, spawnOffsetRangeX);

                    Vector3 spawnPos = transform.position + new Vector3(offsetX, -0.3f, 0f);

                    GameObject bObj = EnemyBulletPool.Instance != null
                        ? EnemyBulletPool.Instance.Get(acceleratingBulletPrefab, spawnPos, downwardRot)
                        : Instantiate(acceleratingBulletPrefab, spawnPos, downwardRot);

                    AcceleratingBullet bullet = bObj.GetComponent<AcceleratingBullet>();
                    if (bullet != null)
                    {
                        bullet.Initialize(targetField);
                    }
                }
            }
        }
    }
}