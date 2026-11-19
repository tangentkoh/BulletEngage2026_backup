using System.Collections;
using UnityEngine;

public class F2Jammer : MonoBehaviour, IAssistantJammer
{
    [Header("弾プレハブ")]
    [Tooltip("EnemyBulletコンポーネントがアタッチされた弾プレハブ")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("発射時間 & 連射設定")]
    [Tooltip("効果時間 (秒)")]
    [SerializeField] private float duration = 1.0f;
    [Tooltip("連射間隔 (秒)")]
    [SerializeField] private float fireInterval = 0.05f;

    [Header("弾パラメータ (固定弾速・直進)")]
    [SerializeField] private float bulletSpeed = 5.0f;
    [SerializeField] private float damage = 10.0f;

    [Header("出現範囲設定 (フィールド上部)")]
    [Tooltip("左右端からのX軸マージン")]
    [SerializeField] private float sideMarginX = 1.0f;
    [Tooltip("上端からのY軸ランダム幅 (上端からどれだけ下まで出現させるか)")]
    [SerializeField] private float topSpawnRangeY = 3.0f;
    [Tooltip("上端からのオフセット")]
    [SerializeField] private float topOffsetY = 1.0f;

    public void TriggerJammer(PlayField targetField)
    {
        if (targetField == null || bulletPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(KillingBlameRoutine(targetField));
    }

    private IEnumerator KillingBlameRoutine(PlayField targetField)
    {
        Rect bounds = targetField.bounds;

        // 1回のスキル発動につき、フィールド上部のランダムな1箇所に弾源を固定
        float spawnX = Random.Range(bounds.xMin + sideMarginX, bounds.xMax - sideMarginX);
        float maxY = bounds.yMax - topOffsetY;
        float minY = maxY - topSpawnRangeY;
        float spawnY = Random.Range(minY, maxY);
        Vector3 fixedSpawnPos = new Vector3(spawnX, spawnY, 0f);

        float timer = 0f;

        while (timer < duration && targetField != null)
        {
            // 360度ランダム方向へ固定弾速で1発発射
            float angleRad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(bulletPrefab, fixedSpawnPos, Quaternion.identity)
                : Instantiate(bulletPrefab, fixedSpawnPos, Quaternion.identity);

            EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(targetField, dir, bulletSpeed, damage);
            }

            timer += fireInterval;
            yield return new WaitForSeconds(fireInterval);
        }

        Destroy(gameObject);
    }
}