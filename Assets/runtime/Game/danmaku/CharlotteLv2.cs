using System.Collections;
using UnityEngine;

public class CharlotteLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName = "スラッシュウィンド";
    public string AttackName => attackName;

    [Header("使用する弾プレハブ")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("基本スペック")]
    [SerializeField] private float baseBulletSpeed = 14.0f; // 早めの弾速
    [SerializeField] private float bulletDamage = 18f;      // 高めの威力
    [SerializeField] private float fireInterval = 0.1f;     // 連射間隔 (0.1秒固定)

    [Header("持続時間設定 (パワー依存)")]
    [Tooltip("Power 1.0 時の持続時間 (初期値: 2.0s)")]
    [SerializeField] private float baseDuration = 2.0f;
    [Tooltip("Power 1.0 ごとの延長時間 (秒)")]
    [SerializeField] private float durationPerPower = 0.2f;

    [Header("3way 拡散角度設定")]
    [SerializeField] private float wayAngle = 15f;

    [Header("出現位置マージン")]
    [SerializeField] private float edgeMarginX = 0.3f;

    private PlayField targetField;
    private float powerLevel = 1.0f;

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(SlashWindRoutine());
    }

    private IEnumerator SlashWindRoutine()
    {
        if (targetField == null || bulletPrefab == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // パワーに応じた持続時間を算出 (初期値2.0s、1.0ごとに+0.2s)
        float totalDuration = baseDuration + ((powerLevel - 1.0f) * durationPerPower);

        // Power 9.0 以上で弾速1.5倍
        float currentBulletSpeed = (powerLevel >= 9.0f) ? (baseBulletSpeed * 1.5f) : baseBulletSpeed;

        // Power 5.0 以上で 3way 攻撃化
        bool is3Way = powerLevel >= 5.0f;

        float timer = 0f;

        while (timer < totalDuration)
        {
            // 1. 相手フィールド上の自機（ターゲット）を取得
            PlayerShip targetPlayer = FindTargetPlayer();

            // 2. 画面上部 (Y座標固定) のランダムな X 座標から生成
            float spawnX = Random.Range(targetField.bounds.xMin + edgeMarginX, targetField.bounds.xMax - edgeMarginX);
            float spawnY = targetField.bounds.yMax - 0.15f;
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            // 3. 狙う方向の計算 (自機がいればその位置、不在なら真下)
            Vector3 targetPos = (targetPlayer != null) ? targetPlayer.transform.position : (spawnPos + Vector3.down);
            Vector3 baseDirection = (targetPos - spawnPos).normalized;

            // 4. 発射処理
            if (is3Way)
            {
                // 3way 発射 (中央、左へ-wayAngle度、右へ+wayAngle度)
                float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
                float[] angles = { baseAngle, baseAngle - wayAngle, baseAngle + wayAngle };

                foreach (float angle in angles)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f).normalized;
                    SpawnBullet(spawnPos, dir, currentBulletSpeed);
                }
            }
            else
            {
                // 通常 1way 発射
                SpawnBullet(spawnPos, baseDirection, currentBulletSpeed);
            }

            timer += fireInterval;
            yield return new WaitForSeconds(fireInterval);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 対象フィールド内に存在する PlayerShip を取得
    /// </summary>
    private PlayerShip FindTargetPlayer()
    {
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var player in players)
        {
            if (player != null && targetField != null)
            {
                Vector3 pos = player.transform.position;
                if (pos.x >= targetField.bounds.xMin && pos.x <= targetField.bounds.xMax)
                {
                    return player;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// プール経由で弾を生成して初期化
    /// </summary>
    private void SpawnBullet(Vector3 position, Vector3 direction, float speed)
    {
        GameObject bObj = EnemyBulletPool.Instance != null
            ? EnemyBulletPool.Instance.Get(bulletPrefab, position, Quaternion.identity)
            : Instantiate(bulletPrefab, position, Quaternion.identity);

        EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.Initialize(targetField, direction, speed, bulletDamage);
        }
    }
}