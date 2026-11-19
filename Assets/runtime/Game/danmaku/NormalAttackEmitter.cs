using System.Collections;
using UnityEngine;

public class NormalAttackEmitter : MonoBehaviour, IAttackEmitter
{
    [Header("弾プレハブ")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("発射時間設定 (パワー依存)")]
    [Tooltip("Power 1.0 時の基礎発射持続時間 (秒)")]
    [SerializeField] private float baseDuration = 1.5f;
    [Tooltip("Power 1.0 上昇ごとの発射延長時間 (秒)")]
    [SerializeField] private float durationPerPower = 0.5f;

    [Header("連射レート & ダメージ")]
    [Tooltip("弾の生成間隔 (秒)")]
    [SerializeField] private float spawnInterval = 0.1f;
    [Tooltip("弾のダメージ")]
    [SerializeField] private float bulletDamage = 25f;

    [Header("弾速設定")]
    [SerializeField] private float minBulletSpeed = 4.5f;
    [SerializeField] private float maxBulletSpeed = 6.5f;

    [Header("発射角度 (真下=-90度 基準)")]
    [Tooltip("左側への最大ブレ角度 (例: -110度)")]
    [SerializeField] private float minAngle = -110f;
    [Tooltip("右側への最大ブレ角度 (例: -70度)")]
    [SerializeField] private float maxAngle = -70f;

    [Header("出現位置設定")]
    [Tooltip("フィールド左右端からの安全マージン")]
    [SerializeField] private float edgeMarginX = 0.5f;
    [Tooltip("フィールド上端からのオフセット")]
    [SerializeField] private float spawnOffsetY = 0.1f;

    private PlayField targetField;
    private float powerLevel = 1.0f;
    public string AttackName => "ノーマルアタック";

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(RainBarrageRoutine());
    }

    private IEnumerator RainBarrageRoutine()
    {
        if (targetField == null || bulletPrefab == null) yield break;

        // パワーに応じた総発射時間を算出
        float totalDuration = baseDuration + ((powerLevel - 1.0f) * durationPerPower);
        float timer = 0f;

        while (timer < totalDuration)
        {
            timer += spawnInterval;

            // 相手フィールド最上部のランダムX座標
            float spawnX = Random.Range(targetField.bounds.xMin + edgeMarginX, targetField.bounds.xMax - edgeMarginX);
            float spawnY = targetField.bounds.yMax - spawnOffsetY;
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            // 角度と弾速のランダム抽出
            float angle = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            float speed = Random.Range(minBulletSpeed, maxBulletSpeed);

            GameObject b = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(bulletPrefab, spawnPos, Quaternion.identity)
                : Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            EnemyBullet eb = b.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(targetField, dir, speed, bulletDamage);
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        Destroy(gameObject);
    }
}