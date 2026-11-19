using UnityEngine;

public class MitsuruOption : PlayerOptionBase
{
    [Header("配置オフセット")]
    [Tooltip("高速時の自機からの相対位置 (すぐ上)")]
    [SerializeField] private Vector3 normalLocalOffset = new Vector3(0f, 0.8f, 0f);
    [Tooltip("低速時の自機からの相対位置 (すぐ下)")]
    [SerializeField] private Vector3 slowLocalOffset = new Vector3(0f, -0.8f, 0f);
    [SerializeField] private float moveSmooth = 25f;

    [Header("索敵設定")]
    [Tooltip("敵を感知できる最大半径")]
    [SerializeField] private float searchRadius = 5.0f;
    [Tooltip("追尾角度の最大許容")]
    [SerializeField] private float maxTargetAngle = 105f;

    [Header("連射補正設定")]
    [Tooltip("低速時の連射インターバル短縮係数")]
    [SerializeField] private float slowFireRateMultiplier = 0.8f;

    private bool isSlowMode = false;
    private float nextFireTime = 0f;
    private PlayerShip cachedShip;

    private void Awake()
    {
        // 親オブジェクトから PlayerShip を取得してキャッシュ
        cachedShip = GetComponentInParent<PlayerShip>();
    }

    public override void UpdatePosition(bool isSlow)
    {
        isSlowMode = isSlow;
        Vector3 targetOffset = isSlow ? slowLocalOffset : normalLocalOffset;
        Transform parentTransform = transform.parent;

        if (parentTransform != null)
        {
            Vector3 targetWorldPos = parentTransform.position + targetOffset;
            transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * moveSmooth);
        }
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier)
    {
        if (bulletPrefab == null) return;

        // 低速時の内部連射レート管理
        if (Time.time < nextFireTime) return;

        // キャッシュした自機データから基本インターバルを取得
        if (cachedShip == null) cachedShip = GetComponentInParent<PlayerShip>();
        float baseInterval = (cachedShip != null && cachedShip.CharacterData != null) ? cachedShip.CharacterData.optionShotInterval : 0.15f;
        float actualInterval = isSlowMode ? (baseInterval * slowFireRateMultiplier) : baseInterval;
        nextFireTime = Time.time + actualInterval;

        // 弾道方向の算出
        Vector3 shootDir = Vector3.up;

        // 高速時のみ、射程内の敵を索敵して発射角度を決定
        if (!isSlowMode)
        {
            Transform targetEnemy = FindNearestEnemy(field);
            if (targetEnemy != null)
            {
                Vector3 toEnemy = (targetEnemy.position - transform.position).normalized;
                float angle = Vector3.Angle(Vector3.up, toEnemy);
                if (angle <= maxTargetAngle)
                {
                    shootDir = toEnemy;
                }
            }
        }

        // 弾の生成と射出
        Quaternion rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg - 90f);
        GameObject bObj = Instantiate(bulletPrefab, transform.position, rot);

        PlayerBullet bullet = bObj.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(bulletSpeed);
            bullet.SetField(field);
            bullet.SetDamage(bullet.Damage * damageMultiplier);
        }
    }

    private Transform FindNearestEnemy(PlayField field)
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        Transform closest = null;
        float closestDistSqr = searchRadius * searchRadius;
        Vector3 selfPos = transform.position;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            if (field != null && (enemy.transform.position.x < field.bounds.xMin || enemy.transform.position.x > field.bounds.xMax))
            {
                continue;
            }

            // 自機より後方（通り過ぎた敵）は狙わない
            if (enemy.transform.position.y < selfPos.y - 0.5f) continue;

            float distSqr = (enemy.transform.position - selfPos).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = enemy.transform;
            }
        }

        return closest;
    }
}