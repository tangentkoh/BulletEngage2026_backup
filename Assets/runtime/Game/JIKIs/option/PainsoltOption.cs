using UnityEngine;

public class PainsoltOption : PlayerOptionBase
{
    [Header("配置設定 (ローカル座標)")]
    [Tooltip("高速移動時の配置 (例: 左 (-1.0, 0.4, 0) / 右 (1.0, 0.4, 0))")]
    [SerializeField] private Vector3 normalLocalPos = new Vector3(1.0f, 0.4f, 0f);

    [Tooltip("低速移動時の配置 (例: 左 (-0.4, 0.2, 0) / 右 (0.4, 0.2, 0))")]
    [SerializeField] private Vector3 slowLocalPos = new Vector3(0.4f, 0.2f, 0f);

    [SerializeField] private float moveSmooth = 14f;

    [Header("自動自動照準（エイム）設定")]
    [Tooltip("敵を感知できる最大半径")]
    [SerializeField] private float searchRadius = 15.0f;

    private Vector3 targetLocalPos;

    public override void UpdatePosition(bool isSlow)
    {
        targetLocalPos = isSlow ? slowLocalPos : normalLocalPos;

        // 滑らかに位置追従（収縮/展開）
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * moveSmooth);

        // 本体角度は正面固定 (弾の発射角度で調整)
        transform.localRotation = Quaternion.identity;
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier)
    {
        if (bulletPrefab == null) return;

        // 常に一番近い敵（通常敵またはボス）を探す
        Vector3 shootDir = Vector3.up; // 敵がいない場合は正面（上）へ発射
        Transform targetEnemy = FindNearestEnemy(field);

        if (targetEnemy != null)
        {
            shootDir = (targetEnemy.position - transform.position).normalized;
        }

        // 進行方向に向かう角度を算出
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg - 90f;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        // 弾を生成して発射
        GameObject bObj = Instantiate(bulletPrefab, transform.position, rot);
        PlayerBullet bullet = bObj.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(bulletSpeed);
            bullet.SetField(field);
            bullet.SetDamage(bullet.Damage * damageMultiplier);
        }
    }

    /// <summary>
    /// 自フィールド内で一番距離が近い敵 (EnemyBase / EngageBossBase) を取得
    /// </summary>
    private Transform FindNearestEnemy(PlayField field)
    {
        Transform closest = null;
        float closestDistSqr = searchRadius * searchRadius;
        Vector3 selfPos = transform.position;

        // 1. 通常敵の探索
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            // 自フィールド内の敵のみ対象
            if (field != null && (enemy.transform.position.x < field.bounds.xMin || enemy.transform.position.x > field.bounds.xMax))
            {
                continue;
            }

            float distSqr = (enemy.transform.position - selfPos).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = enemy.transform;
            }
        }

        // 2. エンゲージボスの探索
        EngageBossBase[] bosses = FindObjectsByType<EngageBossBase>(FindObjectsInactive.Exclude);
        foreach (var boss in bosses)
        {
            if (boss == null || boss.IsDead) continue;

            if (field != null && (boss.transform.position.x < field.bounds.xMin || boss.transform.position.x > field.bounds.xMax))
            {
                continue;
            }

            float distSqr = (boss.transform.position - selfPos).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = boss.transform;
            }
        }

        return closest;
    }
}