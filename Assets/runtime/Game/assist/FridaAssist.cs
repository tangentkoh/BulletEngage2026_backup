using System.Collections;
using UnityEngine;

public class FridaAssist : AssistantBossBase
{
    [Header("矢弾設定")]
    [SerializeField] private GameObject arrowBulletPrefab;
    [SerializeField] private float fireInterval = 0.28f;
    [SerializeField] private float arrowSpeed = 8.5f;
    [SerializeField] private float arrowDamage = 10f;
    [SerializeField] private float columnOffset = 0.35f;

    private Coroutine barrageCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);
        barrageCoroutine = StartCoroutine(WallArrowBarrageLoop());
    }

    private IEnumerator WallArrowBarrageLoop()
    {
        yield return new WaitForSeconds(1.0f); // フェードイン待ち

        while (true)
        {
            yield return new WaitForSeconds(fireInterval);
            if (targetField == null) yield break;

            float spawnY = targetField.bounds.yMax + 0.3f;

            // 左端2列
            float left1 = targetField.bounds.xMin + 0.3f;
            float left2 = left1 + columnOffset;

            // 右端2列
            float right1 = targetField.bounds.xMax - 0.3f;
            float right2 = right1 - columnOffset;

            SpawnDownwardArrow(new Vector3(left1, spawnY, 0f));
            SpawnDownwardArrow(new Vector3(left2, spawnY, 0f));
            SpawnDownwardArrow(new Vector3(right1, spawnY, 0f));
            SpawnDownwardArrow(new Vector3(right2, spawnY, 0f));
        }
    }

    private void SpawnDownwardArrow(Vector3 pos)
    {
        if (arrowBulletPrefab == null || targetField == null) return;

        Quaternion rot = Quaternion.Euler(0f, 0f, 180f);
        GameObject b = EnemyBulletPool.Instance != null
            ? EnemyBulletPool.Instance.Get(arrowBulletPrefab, pos, rot)
            : Instantiate(arrowBulletPrefab, pos, rot);
        EnemyBullet eb = b.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.Initialize(targetField, Vector3.down, arrowSpeed, arrowDamage);
        }
    }

    protected override void Dismiss()
    {
        if (barrageCoroutine != null) StopCoroutine(barrageCoroutine);
        base.Dismiss();
    }
}