using System.Collections;
using UnityEngine;

public class AnnaLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName = "クリエイトサン";
    public string AttackName => attackName;

    [Header("使用する弾プレハブ")]
    [Tooltip("AcceleratingBulletコンポーネントがアタッチされた弾プレハブ")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("渦巻き弾幕基本設定")]
    [Tooltip("弾を発射する間隔(秒)")]
    [SerializeField] private float fireInterval = 0.08f;
    [Tooltip("渦巻きの回転速度 (度/秒)")]
    [SerializeField] private float rotationSpeed = 120f;

    [Header("発動時間設定")]
    [SerializeField] private float baseDuration = 1.6f;
    [SerializeField] private float durationPerPower = 0.3f;
    [SerializeField] private float maxDuration = 4.0f;

    private PlayField targetField;
    private float powerLevel = 1.0f;

    public void Initialize(PlayField target, float power)
    {
        targetField = target;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(CreateSunRoutine());
    }

    private IEnumerator CreateSunRoutine()
    {
        if (targetField == null || bulletPrefab == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // --- パワー値に応じたパラメータ計算 ---
        // 1. 発動時間: 1.6s 初期値、1.0ごとに+0.3s (最大4.0s)
        float duration = Mathf.Min(maxDuration, baseDuration + (powerLevel - 1.0f) * durationPerPower);

        // 2. 渦巻の数: 初期値4つ、5.0で6つ、9.0で8つ
        int armCount = 4;
        if (powerLevel >= 9.0f) armCount = 8;
        else if (powerLevel >= 5.0f) armCount = 6;

        // 相手フィールドのど真ん中（center）を発生位置にする
        Vector3 centerPos = targetField.bounds.center;

        float timer = 0f;
        float currentAngle = 0f;
        float angleStep = 360f / armCount;

        while (timer < duration)
        {
            // 渦巻きのアーム数ぶんだけ全方位に弾を発射
            for (int i = 0; i < armCount; i++)
            {
                float angle = currentAngle + (i * angleStep);
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);

                GameObject bObj = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(bulletPrefab, centerPos, rot)
                    : Instantiate(bulletPrefab, centerPos, rot);

                AcceleratingBullet bullet = bObj.GetComponent<AcceleratingBullet>();
                if (bullet != null)
                {
                    bullet.Initialize(targetField);
                }
            }

            // 回転角を更新
            currentAngle += rotationSpeed * fireInterval;
            if (currentAngle >= 360f) currentAngle -= 360f;

            timer += fireInterval;
            yield return new WaitForSeconds(fireInterval);
        }

        Destroy(gameObject);
    }
}