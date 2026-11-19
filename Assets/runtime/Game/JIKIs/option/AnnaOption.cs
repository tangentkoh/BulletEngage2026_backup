using UnityEngine;

public class AnnaOption : PlayerOptionBase
{
    [Header("オプションインデックス設定")]
    [Tooltip("4つのうち何番目のオプションか (0, 1, 2, 3)")]
    [SerializeField] private int optionIndex = 0;
    [Tooltip("全オプション数")]
    [SerializeField] private int totalOptions = 4;

    [Header("回転 & 半径設定")]
    [Tooltip("高速移動時の回転半径")]
    [SerializeField] private float normalRadius = 1.2f;
    [Tooltip("低速移動時の回転半径")]
    [SerializeField] private float slowRadius = 0.6f;
    [Tooltip("回転速度 (度/秒)")]
    [SerializeField] private float rotateSpeed = 180f;
    [Tooltip("半径変化のスムーズ度")]
    [SerializeField] private float radiusSmooth = 20f;

    [Header("射撃・ブレ設定")]
    [Tooltip("弾の発射ブレ角度 (度) ※±この範囲でランダムにぶれる")]
    [SerializeField] private float bulletSpreadAngle = 6.0f;

    private float currentAngle = 0f;
    private float currentRadius;

    private void Awake()
    {
        currentRadius = normalRadius;
        // 初期角度の配置（4つの場合は 0度, 90度, 180度, 270度）
        currentAngle = (360f / Mathf.Max(1, totalOptions)) * optionIndex;
    }

    public override void UpdatePosition(bool isSlow)
    {
        // 1. 時間経過で回転角度を進める
        currentAngle += rotateSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        // 2. 高速 / 低速 に合わせた目標半径へ滑らかに補間
        float targetRadius = isSlow ? slowRadius : normalRadius;
        currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * radiusSmooth);

        // 3. 自機中心の円周上のローカル座標を計算
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 localPos = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * currentRadius;

        transform.localPosition = localPos;
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier = 1.0f)
    {
        if (bulletPrefab == null) return;

        // 正面（上方向 90度）を基準に、僅かにランダムなブレ角度（例: -4度〜+4度）を加算
        float randomOffset = Random.Range(-bulletSpreadAngle, bulletSpreadAngle);
        Quaternion shotRotation = Quaternion.Euler(0f, 0f, randomOffset);

        GameObject b = Instantiate(bulletPrefab, transform.position, shotRotation);
        PlayerBullet bullet = b.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(bulletSpeed);
            bullet.SetField(field);
            bullet.SetDamage(bullet.Damage * damageMultiplier);
        }
    }
}