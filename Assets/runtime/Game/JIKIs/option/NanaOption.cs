using UnityEngine;

public class NanaOption : PlayerOptionBase
{
    [Header("位置・復帰設定")]
    [Tooltip("高速移動時の自機からの相対位置（自機頭上）")]
    [SerializeField] private Vector3 normalLocalPos = new Vector3(0f, 1.2f, 0f);
    [Tooltip("高速移動復帰時の移動スムーズ度")]
    [SerializeField] private float returnSmooth = 12f;

    private bool isFixed = false;
    private Vector3 fixedWorldPosition;

    public override void UpdatePosition(bool isSlow)
    {
        if (isSlow)
        {
            // 低速移動開始時の1フレーム目にその場のワールド座標を記憶
            if (!isFixed)
            {
                fixedWorldPosition = transform.position;
                isFixed = true;
            }

            // 低速移動中は記憶したワールド座標に固定
            transform.position = fixedWorldPosition;
        }
        else
        {
            if (isFixed)
            {
                isFixed = false;
            }

            // 高速移動中は自機の頭上（normalLocalPos）へ滑らかに復帰・追従
            transform.localPosition = Vector3.Lerp(transform.localPosition, normalLocalPos, Time.deltaTime * returnSmooth);
        }
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier)
    {
        if (bulletPrefab == null) return;

        // オプションの位置から正面（上方向）へ弾を発射
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