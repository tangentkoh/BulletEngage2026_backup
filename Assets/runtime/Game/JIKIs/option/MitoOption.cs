using UnityEngine;

public class MitoOption : PlayerOptionBase
{
    [Header("配置設定 (ローカル座標)")]
    [Tooltip("高速移動時の配置 (例: 左 (-1.5, 0, 0) / 右 (1.5, 0, 0))")]
    [SerializeField] private Vector3 normalLocalPos = new Vector3(1.5f, 0f, 0f);

    [Tooltip("低速移動時の配置 (例: 左 (-0.5, 0, 0) / 右 (0.5, 0, 0))")]
    [SerializeField] private Vector3 slowLocalPos = new Vector3(0.5f, 0f, 0f);

    [SerializeField] private float moveSmooth = 14f;

    [Header("低速移動時パラメータ補正")]
    [Tooltip("低速移動時の弾速倍率")]
    [SerializeField] private float slowSpeedMultiplier = 1.2f;

    private Vector3 targetLocalPos;
    private bool isSlowMode = false;

    public override void UpdatePosition(bool isSlow)
    {
        isSlowMode = isSlow;
        targetLocalPos = isSlow ? slowLocalPos : normalLocalPos;

        // 滑らかに位置移動（展開・収束）
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * moveSmooth);

        // 角度は常に正面固定
        transform.localRotation = Quaternion.identity;
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier)
    {
        if (bulletPrefab == null) return;

        // 低速移動時は弾速が 1.2 倍に上昇
        float finalSpeed = isSlowMode ? (bulletSpeed * slowSpeedMultiplier) : bulletSpeed;

        // 常に正面（Quaternion.identity）に向けて発射
        GameObject bObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        PlayerBullet bullet = bObj.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(finalSpeed);
            bullet.SetField(field);
            bullet.SetDamage(bullet.Damage * damageMultiplier);
        }
    }
}