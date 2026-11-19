using System.Collections;
using UnityEngine;

public class AnnaLv3 : EngageBossBase
{
    [Header("弾プレハブ")]
    [Tooltip("EnemyBulletコンポーネントがアタッチされた弾プレハブ")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("発射ポイント設定 (未設定時はボス位置からのオフセットを使用)")]
    [SerializeField] private Transform leftMuzzle;
    [SerializeField] private Transform rightMuzzle;
    [Tooltip("Muzzle未割り当て時に使用するボス中心からの左右オフセット")]
    [SerializeField] private float muzzleOffset = 0.5f;

    [Header("バースト & クールタイム設定")]
    [Tooltip("バースト中の連射間隔(秒)")]
    [SerializeField] private float fireInterval = 0.02f;
    [Tooltip("バースト終了後のクールタイム(秒)")]
    [SerializeField] private float cooldownTime = 1.0f;

    [Header("パワー依存パラメータ設定")]
    [Tooltip("Power 1.0 時のバースト持続時間(秒)")]
    [SerializeField] private float baseBurstDuration = 0.6f;
    [Tooltip("Power 1.0 あたりの持続時間延長(秒)")]
    [SerializeField] private float durationPerPower = 0.1f;
    [Tooltip("最大バースト持続時間(秒)")]
    [SerializeField] private float maxBurstDuration = 1.4f;

    [Header("弾幕スペック")]
    [SerializeField] private float bulletSpeed = 7.5f;
    [SerializeField] private float bulletDamage = 12.0f;
    [Tooltip("下方向(-90度)を中心とした拡散角度 (例: 90で -135度〜-45度)")]
    [SerializeField] private float spreadAngle = 90.0f;

    private Coroutine barrageCoroutine;

    protected override void StartBarrage()
    {
        barrageCoroutine = StartCoroutine(MegaFlameLoop());
    }

    protected override void StopBarrage()
    {
        if (barrageCoroutine != null)
        {
            StopCoroutine(barrageCoroutine);
            barrageCoroutine = null;
        }
    }

    private IEnumerator MegaFlameLoop()
    {
        if (bulletPrefab == null) yield break;

        while (true)
        {
            // --- パワー値に応じた動的パラメータ計算 ---
            // 1. バースト持続時間: 初期値0.6s、1.0ごとに+0.1s (最大1.4s)
            float currentBurstDuration = Mathf.Min(maxBurstDuration, baseBurstDuration + (currentPower - 1.0f) * durationPerPower);

            // 2. 1回の発射でばら撒く弾数: 初期値2個、Power 9.0で3個に増加
            int bulletsPerFire = (currentPower >= 9.0f) ? 3 : 2;

            // 3. 弾源の増加判定: Power 5.0 以上で右側も追加（左右両方へ）
            bool enableRightMuzzle = currentPower >= 5.0f;

            // --- バースト攻撃開始 ---
            float burstTimer = 0f;
            while (burstTimer < currentBurstDuration)
            {
                // ① 左側の弾源から発射（常時）
                Vector3 leftPos = (leftMuzzle != null) ? leftMuzzle.position : (transform.position + Vector3.left * muzzleOffset);
                FireSpreadBurst(leftPos, bulletsPerFire);

                // ② Power 5.0 以上なら右側の弾源からも同時に発射（左右両方）
                if (enableRightMuzzle)
                {
                    Vector3 rightPos = (rightMuzzle != null) ? rightMuzzle.position : (transform.position + Vector3.right * muzzleOffset);
                    FireSpreadBurst(rightPos, bulletsPerFire);
                }

                burstTimer += fireInterval;
                yield return new WaitForSeconds(fireInterval);
            }

            // --- バースト後のクールタイム ---
            yield return new WaitForSeconds(cooldownTime);
        }
    }

    private void FireSpreadBurst(Vector3 muzzlePos, int count)
    {
        float halfAngle = spreadAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            // 下方向(-90度)を中心に ±45度(計90度)の範囲でランダムにばら撒く
            float randomAngle = Random.Range(-90f - halfAngle, -90f + halfAngle) * Mathf.Deg2Rad;
            Vector3 shootDir = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(bulletPrefab, muzzlePos, Quaternion.identity)
                : Instantiate(bulletPrefab, muzzlePos, Quaternion.identity);

            EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(targetField, shootDir, bulletSpeed, bulletDamage);
            }
        }
    }
}