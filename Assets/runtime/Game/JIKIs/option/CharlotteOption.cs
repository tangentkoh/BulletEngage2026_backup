using UnityEngine;

public class CharlotteOption : PlayerOptionBase
{
    [Header("位置設定 (ローカル座標)")]
    [Tooltip("通常移動時の配置 (例: 左オプション (-1.2, 0, 0) / 右オプション (1.2, 0, 0))")]
    [SerializeField] private Vector3 normalLocalPos;

    [Tooltip("低速移動時の配置 (例: 左オプション (-0.5, 0, 0) / 右オプション (0.5, 0, 0))")]
    [SerializeField] private Vector3 slowLocalPos;

    [SerializeField] private float moveSmooth = 14f;

    private Vector3 targetLocalPos;

    public override void UpdatePosition(bool isSlow)
    {
        targetLocalPos = isSlow ? slowLocalPos : normalLocalPos;

        // 滑らかに位置追従
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * moveSmooth);

        // 向きは常に正面(0, 0, 0)で固定
        transform.localRotation = Quaternion.identity;
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier)
    {
        if (bulletPrefab == null) return;

        // 常に正面（Quaternion.identity）に向けて発射
        GameObject b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        PlayerBullet bullet = b.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(bulletSpeed);
            bullet.SetField(field);
            bullet.SetDamage(bullet.Damage * damageMultiplier);
        }
    }
}