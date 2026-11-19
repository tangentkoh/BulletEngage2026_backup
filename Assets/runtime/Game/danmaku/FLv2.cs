using System.Collections;
using UnityEngine;

public class FLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName = "リバースエンジア";

    [Header("矢弾プレハブ")]
    [SerializeField] private GameObject arrowBulletPrefab;

    [Header("持続時間設定 (パワー依存)")]
    [Tooltip("Power 1.0 時の持続時間 (秒)")]
    [SerializeField] private float baseDuration = 1.8f;
    [Tooltip("Power 1.0 上昇ごとの延長時間 (秒)")]
    [SerializeField] private float durationPerPower = 0.3f;

    [Header("発射数 & 連射レート (パワー依存)")]
    [Tooltip("初期の同時発射数 (Power 5.0 未満)")]
    [SerializeField] private int baseBulletCount = 3;
    [Tooltip("Power 5.0 到達時の追加発射数")]
    [SerializeField] private int bonusBulletCountAtPower5 = 2;
    [Tooltip("発射間隔 (秒・固定)")]
    [SerializeField] private float spawnInterval = 0.2f;

    [Header("弾速 & ダメージ (パワー依存)")]
    [Tooltip("初期弾速 (等速)")]
    [SerializeField] private float baseBulletSpeed = 4.0f;
    [Tooltip("Power 9.0 到達時の弾速倍率")]
    [SerializeField] private float speedMultiplierAtPower9 = 1.5f;
    [Tooltip("弾のダメージ")]
    [SerializeField] private float bulletDamage = 10f;

    [Header("発射角度 (真上=90度 基準)")]
    [Tooltip("右側への最大ブレ角度 (例: 75度)")]
    [SerializeField] private float minAngle = 80f;
    [Tooltip("左側への最大ブレ角度 (例: 105度)")]
    [SerializeField] private float maxAngle = 100f;
    [Tooltip("進行方向に合わせて矢の向きを回転させるか")]
    [SerializeField] private bool rotateToDirection = true;

    [Header("出現位置設定 (相手フィールド下端)")]
    [Tooltip("フィールド左右端からの安全マージン")]
    [SerializeField] private float edgeMarginX = 0.2f;
    [Tooltip("フィールド下端からのオフセット")]
    [SerializeField] private float spawnOffsetY = 0f;

    private PlayField targetField;
    private float powerLevel = 1.0f;
    public string AttackName => attackName;

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(ReverseBarrageRoutine());
    }

    private IEnumerator ReverseBarrageRoutine()
    {
        if (targetField == null || arrowBulletPrefab == null) yield break;

        // 1. Power 1.0ごとに発動時間を0.3s延長 (初期1.8s)
        float totalDuration = baseDuration + ((powerLevel - 1.0f) * durationPerPower);

        // 2. Power 5.0以上で弾数が2つ増加 (初期3つ -> 5つ)
        int currentBulletCount = (powerLevel >= 5.0f)
            ? (baseBulletCount + bonusBulletCountAtPower5)
            : baseBulletCount;

        // 3. Power 9.0以上で弾速が1.5倍にアップ (初期4.0f -> 6.0f)
        float currentSpeed = (powerLevel >= 9.0f)
            ? (baseBulletSpeed * speedMultiplierAtPower9)
            : baseBulletSpeed;

        float timer = 0f;

        while (timer < totalDuration)
        {
            timer += spawnInterval;

            for (int i = 0; i < currentBulletCount; i++)
            {
                // 相手フィールド最下部のランダムX座標
                float spawnX = Random.Range(targetField.bounds.xMin + edgeMarginX, targetField.bounds.xMax - edgeMarginX);
                float spawnY = targetField.bounds.yMin + spawnOffsetY;
                Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

                // 上方向への角度算出
                float angleDeg = Random.Range(minAngle, maxAngle);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

                Quaternion rot = rotateToDirection
                    ? Quaternion.Euler(0f, 0f, angleDeg - 90f)
                    : Quaternion.identity;

                GameObject b = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(arrowBulletPrefab, spawnPos, rot)
                    : Instantiate(arrowBulletPrefab, spawnPos, rot);

                EnemyBullet eb = b.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(targetField, dir, currentSpeed, bulletDamage);
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        Destroy(gameObject);
    }
}