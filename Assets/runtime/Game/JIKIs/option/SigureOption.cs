using UnityEngine;

public class SigureOption : PlayerOptionBase
{
    [Header("配置オフセット")]
    [Tooltip("高速時のローカル目標座標 (横広め)")]
    [SerializeField] private Vector3 normalLocalPos;
    [Tooltip("低速時のローカル目標座標 (縦収束)")]
    [SerializeField] private Vector3 slowLocalPos;
    [SerializeField] private float moveSmooth = 10f;

    [Header("バルカン散布角設定 (単発・前方ランダムブレ)")]
    [SerializeField] private float normalRandomSpread = 15f;
    [SerializeField] private float slowRandomSpread = 5f;

    private Vector3 targetLocalPos;
    private bool currentIsSlow = false;

    public override void UpdatePosition(bool isSlow)
    {
        currentIsSlow = isSlow;
        targetLocalPos = isSlow ? slowLocalPos : normalLocalPos;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * moveSmooth);
    }

    public override void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier)
    {
        if (bulletPrefab == null) return;

        // 高速/低速に応じたブレ幅を決定
        float currentSpread = currentIsSlow ? slowRandomSpread : normalRandomSpread;

        // 真上(90度)を基準に ±currentSpread のランダム角度
        float randomAngle = 90f + Random.Range(-currentSpread, currentSpread);
        float rad = randomAngle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
        Quaternion rot = Quaternion.Euler(0f, 0f, randomAngle - 90f);

        GameObject b = Instantiate(bulletPrefab, transform.position, rot);
        PlayerBullet bullet = b.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(bulletSpeed);
            bullet.SetField(field);
            bullet.SetDamage(bullet.Damage * damageMultiplier);
        }
    }
}