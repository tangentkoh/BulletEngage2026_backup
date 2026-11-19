using System.Collections;
using UnityEngine;

public class SigureAssist : AssistantBossBase
{
    [Header("生成プレハブ")]
    [SerializeField] private GameObject paintPrefab;
    [SerializeField] private GameObject sniperBulletPrefab;

    [Header("アクション間隔 & パラメータ")]
    [SerializeField] private float actionInterval = 2.0f;
    [SerializeField] private float sniperSpeed = 5.0f;
    [SerializeField] private float sniperDamage = 25f;
    [SerializeField] private float paintMarginX = 1.5f;
    [SerializeField] private float paintMarginY = 1.5f;

    private Coroutine sabotageCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);
        sabotageCoroutine = StartCoroutine(SabotageLoop());
    }

    private IEnumerator SabotageLoop()
    {
        yield return new WaitForSeconds(1.0f); // フェードイン完了待ち

        while (true)
        {
            yield return new WaitForSeconds(actionInterval);
            if (targetField == null) yield break;

            // 1. ペイント弾の生成 (フィールド端を除外)
            if (paintPrefab != null)
            {
                float px = Random.Range(targetField.bounds.xMin + paintMarginX, targetField.bounds.xMax - paintMarginX);
                float py = Random.Range(targetField.bounds.yMin + paintMarginY, targetField.bounds.yMax - paintMarginY);
                Instantiate(paintPrefab, new Vector3(px, py, 0f), Quaternion.identity);
            }

            // 2. 自機狙い弾の射出
            if (sniperBulletPrefab != null)
            {
                Transform player = GetTargetPlayer();
                Vector3 targetPos = (player != null) ? player.position : transform.position + Vector3.down;
                Vector3 shootDir = (targetPos - transform.position).normalized;

                GameObject b = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(sniperBulletPrefab, transform.position, Quaternion.identity)
                    : Instantiate(sniperBulletPrefab, transform.position, Quaternion.identity);
                EnemyBullet eb = b.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(targetField, shootDir, sniperSpeed, sniperDamage);
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

    protected override void Dismiss()
    {
        if (sabotageCoroutine != null) StopCoroutine(sabotageCoroutine);
        base.Dismiss();
    }
}