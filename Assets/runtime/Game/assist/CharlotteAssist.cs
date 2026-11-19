using System.Collections;
using UnityEngine;

public class CharlotteAssist : AssistantBossBase
{
    [Header("斬撃弾設定")]
    [SerializeField] private GameObject slashBulletPrefab;
    [SerializeField] private float bulletSpeed = 9.0f;     // 素早い弾速
    [SerializeField] private float bulletDamage = 20.0f;
    [SerializeField] private float fireInterval = 1.1f;     // 斬撃の発射間隔

    private Coroutine slashCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);

        // 主役ボスの移動後の待機時間を半減
        if (mainBoss != null)
        {
            mainBoss.MultiplyStayTime(0.5f);
        }

        slashCoroutine = StartCoroutine(SlashBarrageLoop());
    }

    private IEnumerator SlashBarrageLoop()
    {
        yield return new WaitForSeconds(1.0f); // フェードイン完了待ち

        while (true)
        {
            yield return new WaitForSeconds(fireInterval);
            if (targetField == null) yield break;

            // 相手プレイヤーの方向を取得して狙いを定める (いなければ真下)
            Transform playerT = GetTargetPlayer();
            Vector3 targetPos = (playerT != null) ? playerT.position : (transform.position + Vector3.down);
            Vector3 shootDir = (targetPos - transform.position).normalized;

            if (slashBulletPrefab != null)
            {
                GameObject b = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(slashBulletPrefab, transform.position, Quaternion.identity)
                    : Instantiate(slashBulletPrefab, transform.position, Quaternion.identity);

                EnemyBullet eb = b.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(targetField, shootDir, bulletSpeed, bulletDamage);
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
        if (slashCoroutine != null) StopCoroutine(slashCoroutine);
        base.Dismiss();
    }
}