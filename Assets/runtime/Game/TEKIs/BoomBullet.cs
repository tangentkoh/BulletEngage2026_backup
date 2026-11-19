using System.Collections;
using UnityEngine;

public class BoomBullet : MonoBehaviour
{
    [Header("円形散弾の設定")]
    [Tooltip("円形に撒かれる散弾プレハブ (EnemyBulletが付いたもの)")]
    [SerializeField] private GameObject subBulletPrefab;
    [Tooltip("円形弾を放つ間隔(秒)")]
    [SerializeField] private float ringInterval = 0.6f;
    [Tooltip("円形弾の最小速度")]
    [SerializeField] private float minSubSpeed = 3.0f;
    [Tooltip("円形弾の最大速度")]
    [SerializeField] private float maxSubSpeed = 7.0f;
    [Tooltip("円形弾のダメージ")]
    [SerializeField] private float subDamage = 10f;

    private PlayField targetField;
    private Vector3 moveDirection;
    private float moveSpeed;
    private float mainDamage;
    private int ringBulletCount;
    private float ringTimer;

    public void Initialize(PlayField field, Vector3 dir, float speed, float damage, int ringCount)
    {
        targetField = field;
        moveDirection = dir.normalized;
        moveSpeed = speed;
        mainDamage = damage;
        ringBulletCount = ringCount;
        ringTimer = ringInterval;

        // 弾の進行方向に向ける
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        // 直進移動
        transform.position += moveDirection * (moveSpeed * Time.deltaTime);

        // 一定時間ごとに円形弾を発射
        ringTimer -= Time.deltaTime;
        if (ringTimer <= 0f)
        {
            ringTimer = ringInterval;
            SpawnRingBarrage();
        }

        // フィールド枠外デスポーン
        if (targetField != null)
        {
            Rect bounds = targetField.bounds;
            float margin = 1.0f;
            if (transform.position.x < bounds.xMin - margin || transform.position.x > bounds.xMax + margin ||
                transform.position.y < bounds.yMin - margin || transform.position.y > bounds.yMax + margin)
            {
                Destroy(gameObject);
            }
        }
    }

    private void SpawnRingBarrage()
    {
        if (subBulletPrefab == null || ringBulletCount <= 0) return;

        float angleStep = 360f / ringBulletCount;
        float randomBaseAngle = Random.Range(0f, 360f);

        for (int i = 0; i < ringBulletCount; i++)
        {
            float angleDeg = randomBaseAngle + (angleStep * i);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            // 弾ごとに速度にバラツキを持たせる
            float randomSpeed = Random.Range(minSubSpeed, maxSubSpeed);

            GameObject subObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(subBulletPrefab, transform.position, Quaternion.identity)
                : Instantiate(subBulletPrefab, transform.position, Quaternion.identity);

            EnemyBullet eb = subObj.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(targetField, shootDir, randomSpeed, subDamage);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerShip player = other.GetComponent<PlayerShip>() ?? other.GetComponentInParent<PlayerShip>();
        if (player != null)
        {
            player.TakeDamage(mainDamage);
            Destroy(gameObject);
        }
    }
}