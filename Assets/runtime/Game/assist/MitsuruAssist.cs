using System.Collections;
using UnityEngine;

public class MitsuruAssist : AssistantBossBase
{
    [Header("弾設定")]
    [SerializeField] private GameObject mediumBulletPrefab;
    [SerializeField] private float bulletSpeed = 4.5f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private float barrageInterval = 0.5f;
    [SerializeField] private int bulletsPerWave = 5;

    private Coroutine barrageCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);

        // 主役ボスの最大HPおよび現在HPを20%増加
        if (mainBoss != null)
        {
            mainBoss.BuffMaxHP(1.2f);
        }

        barrageCoroutine = StartCoroutine(RandomBarrageLoop());
    }

    private IEnumerator RandomBarrageLoop()
    {
        yield return new WaitForSeconds(1.0f); // フェードイン完了待ち

        while (true)
        {
            yield return new WaitForSeconds(barrageInterval);
            if (targetField == null) yield break;

            // 中玉のランダム角バラマキ
            for (int i = 0; i < bulletsPerWave; i++)
            {
                float angle = Random.Range(190f, 350f); // 主に下半球方向
                float rad = angle * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                if (mediumBulletPrefab != null)
                {
                    GameObject b = EnemyBulletPool.Instance != null
                        ? EnemyBulletPool.Instance.Get(mediumBulletPrefab, transform.position, Quaternion.identity)
                        : Instantiate(mediumBulletPrefab, transform.position, Quaternion.identity);
                    EnemyBullet eb = b.GetComponent<EnemyBullet>();
                    if (eb != null)
                    {
                        eb.Initialize(targetField, dir, bulletSpeed, bulletDamage);
                    }
                }
            }
        }
    }

    protected override void Dismiss()
    {
        if (barrageCoroutine != null) StopCoroutine(barrageCoroutine);
        base.Dismiss();
    }
}