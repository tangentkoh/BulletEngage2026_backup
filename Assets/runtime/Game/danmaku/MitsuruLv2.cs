using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MitsuruLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName;
    public string AttackName => attackName;

    [Header("弾プレハブ設定")]
    [SerializeField] private GameObject bigBulletPrefab;
    [SerializeField] private float bulletDamage = 25f;

    [Header("弾速・拡散範囲")]
    [SerializeField] private float minSpeed = 3.5f;
    [SerializeField] private float maxSpeed = 6.0f;
    [SerializeField] private float spreadAngle = 25f;

    [Header("弾源オフセット設定")]
    [SerializeField] private float spawnOffsetX = 1.0f;

    private PlayField targetField;
    private float currentPower = 1.0f;
    private float fireDuration;
    private const float FireInterval = 0.1f;

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        currentPower = Mathf.Clamp(power, 1.0f, 9.0f);

        // 持続時間: 初期1.4s + Power 1.0につき0.2s延長 (Power 1.0で1.4s、Power 9.0で最大3.0s)
        fireDuration = Mathf.Min(3.2f, 1.6f + (currentPower - 1.0f) * 0.2f);

        StartCoroutine(BarrageRoutine());
    }

    private IEnumerator BarrageRoutine()
    {
        if (targetField == null) yield break;

        // 基準となる上部中央座標
        float centerX = targetField.bounds.center.x;
        float spawnY = targetField.bounds.yMax - 0.2f;

        // 発射ポイントリストの構築
        List<Vector3> spawnPoints = new List<Vector3>
        {
            new Vector3(centerX, spawnY, 0f) // 中央弾源 (常に存在)
        };

        // Power 5.0 以上で中央から少し右に追加
        if (currentPower >= 5.0f)
        {
            spawnPoints.Add(new Vector3(centerX + spawnOffsetX, spawnY, 0f));
        }

        // Power 9.0 でさらに中央から少し左に追加
        if (currentPower >= 9.0f)
        {
            spawnPoints.Add(new Vector3(centerX - spawnOffsetX, spawnY, 0f));
        }

        float timer = 0f;
        while (timer < fireDuration)
        {
            // 有効なすべての弾源から大玉を発射
            foreach (var point in spawnPoints)
            {
                SpawnRandomBigBullet(point);
            }

            yield return new WaitForSeconds(FireInterval);
            timer += FireInterval;
        }

        // 撃ち終わりに自身を破棄
        Destroy(gameObject);
    }

    private void SpawnRandomBigBullet(Vector3 spawnPos)
    {
        if (bigBulletPrefab == null || targetField == null) return;

        // 下方向 (-90度) を中心にランダムな角度ブレ
        float angleOffset = Random.Range(-spreadAngle, spreadAngle);
        float baseAngle = -90f + angleOffset;
        float rad = baseAngle * Mathf.Deg2Rad;
        Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        float speed = Random.Range(minSpeed, maxSpeed);

        GameObject b = EnemyBulletPool.Instance != null
            ? EnemyBulletPool.Instance.Get(bigBulletPrefab, spawnPos, Quaternion.identity)
            : Instantiate(bigBulletPrefab, spawnPos, Quaternion.identity);
        EnemyBullet eb = b.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.Initialize(targetField, shootDir, speed, bulletDamage);
        }
    }
}