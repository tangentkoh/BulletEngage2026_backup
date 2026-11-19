using System.Collections;
using UnityEngine;

public class MimoriAssist : AssistantBossBase
{
    [Header("弾設定")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireInterval = 0.28f;
    [SerializeField] private float bulletSpeed = 8.5f;
    [SerializeField] private float bulletDamage = 10f;

    [Header("2列の高さオフセット")]
    [Tooltip("一番下の列と2列目のY方向の間隔")]
    [SerializeField] private float rowOffset = 0.4f;

    [Tooltip("一番下の列の画面底面からのマージン")]
    [SerializeField] private float bottomMargin = 0.4f;

    private Coroutine barrageCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);
        barrageCoroutine = StartCoroutine(BottomWallBarrageLoop());
    }

    private IEnumerator BottomWallBarrageLoop()
    {
        yield return new WaitForSeconds(1.0f); // フェードイン待機

        while (true)
        {
            yield return new WaitForSeconds(fireInterval);
            if (targetField == null) yield break;

            // 画面右端から射出
            float spawnX = targetField.bounds.xMax + 0.3f;

            // 画面一番下から2列分（高さ2段階）のY座標を算出
            float bottom1 = targetField.bounds.yMin + bottomMargin;
            float bottom2 = bottom1 + rowOffset;

            // 右端から左方向へ2列同時に発射
            SpawnLeftwardBullet(new Vector3(spawnX, bottom1, 0f));
            SpawnLeftwardBullet(new Vector3(spawnX, bottom2, 0f));
        }
    }

    private void SpawnLeftwardBullet(Vector3 pos)
    {
        if (bulletPrefab == null || targetField == null) return;

        GameObject b = EnemyBulletPool.Instance != null
            ? EnemyBulletPool.Instance.Get(bulletPrefab, pos, Quaternion.identity)
            : Instantiate(bulletPrefab, pos, Quaternion.identity);

        EnemyBullet eb = b.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            // 右端から左向き (Vector3.left) へ発射
            eb.Initialize(targetField, Vector3.left, bulletSpeed, bulletDamage);
        }
    }

    protected override void Dismiss()
    {
        if (barrageCoroutine != null) StopCoroutine(barrageCoroutine);
        base.Dismiss();
    }
}