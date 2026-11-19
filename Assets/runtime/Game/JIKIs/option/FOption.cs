using UnityEngine;

public class FOption : PlayerOptionBase
{
    [Header("Fオプションのターゲット親")]
    [SerializeField] private Transform targetParent;
    [SerializeField] private Vector3 normalLocalPos;
    [SerializeField] private Vector3 slowLocalPos;
    [SerializeField] private float moveSmooth = 15f;

    [Header("回転角度設定 (低速時の内向き収束)")]
    [SerializeField] private float normalRotationZ = 10f;
    [SerializeField] private float slowRotationZ = -10f;
    [SerializeField] private float rotateSmooth = 20f;

    private Vector3 targetLocalPos;
    private float targetRotationZ;

    public override void UpdatePosition(bool isSlow)
    {
        targetLocalPos = isSlow ? slowLocalPos : normalLocalPos;
        targetRotationZ = isSlow ? slowRotationZ : normalRotationZ;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * moveSmooth);
        
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetRotationZ);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRot, Time.deltaTime * rotateSmooth);
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier)
    {
        if (bulletPrefab == null) return;
        // オプション本体の回転(transform.rotation)を引き継いで発射
        GameObject b = Instantiate(bulletPrefab, transform.position, transform.rotation);
        PlayerBullet bullet = b.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(bulletSpeed);
            bullet.SetField(field);
            bullet.SetDamage(bullet.Damage * damageMultiplier);
        }
    }
}