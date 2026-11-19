using UnityEngine;

public class MimoriOption : PlayerOptionBase
{
    [Header("配置設定 (ローカル座標)")]
    [Tooltip("高速移動時の配置 (左側)")]
    [SerializeField] private Vector3 normalLocalPos = new Vector3(-0.5f, 0f, 0f);

    [Tooltip("低速移動時の配置 (右側)")]
    [SerializeField] private Vector3 slowLocalPos = new Vector3(0.5f, 0f, 0f);

    [SerializeField] private float moveSmooth = 10f;

    [Header("弾ブレ設定")]
    [Tooltip("発射角度のブレ幅 (±度)")]
    [SerializeField] private float spreadAngle = 10f;

    private Vector3 targetLocalPos;

    public override void UpdatePosition(bool isSlow)
    {
        // 高速時は左側、低速時は右側へターゲット位置を変更
        targetLocalPos = isSlow ? slowLocalPos : normalLocalPos;

        // 滑らかに位置追従
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * moveSmooth);

        // 本体角度は正面固定
        transform.localRotation = Quaternion.identity;
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier)
    {
        if (bulletPrefab == null) return;

        // 上方向 (90度) をベースに ±spreadAngle のランダムなブレを加算
        float randomOffset = Random.Range(-spreadAngle, spreadAngle);
        float shootAngle = 90f + randomOffset;
        float rad = shootAngle * Mathf.Deg2Rad;
        Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        // 弾の向きを進行方向に合わせる
        Quaternion rot = Quaternion.Euler(0f, 0f, shootAngle - 90f);

        GameObject bObj = Instantiate(bulletPrefab, transform.position, rot);
        PlayerBullet bullet = bObj.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(bulletSpeed);
            bullet.SetField(field);
            bullet.SetDamage(bullet.Damage * damageMultiplier);
        }
    }
}