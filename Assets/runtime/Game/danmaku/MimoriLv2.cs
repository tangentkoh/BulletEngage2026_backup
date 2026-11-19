using System.Collections;
using UnityEngine;

public class MimoriLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName = "ドロップブレイブ";
    public string AttackName => attackName;

    [Header("使用する弾プレハブ (EnemyBullet付き)")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("基本設定")]
    [SerializeField] private float baseDuration = 1.4f;       // Power 1.0 時の効果時間 (秒)
    [SerializeField] private float durationPerPower = 0.2f;    // Power 1.0 ごとの延長時間 (秒)
    [SerializeField] private float fireInterval = 0.1f;        // 連射間隔 (秒)
    [SerializeField] private float bulletDamage = 15.0f;       // 弾の威力

    [Header("落下範囲設定")]
    [Tooltip("基準位置からの左右の落下幅")]
    [SerializeField] private float spreadRadius = 2f;

    private PlayField targetField;
    private float powerLevel = 1.0f;

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(DropBraveRoutine());
    }

    private IEnumerator DropBraveRoutine()
    {
        if (targetField == null || bulletPrefab == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // --- 基準位置の確定 (発動時点の相手X座標 / 途中で変化しない) ---
        float targetX = GetTargetPlayerX(targetField);

        // --- パワー値に応じたパラメータ計算 ---
        // 1. 効果時間: 初期1.4s (Power 1.0ごとに +0.2s)
        float totalDuration = baseDuration + ((powerLevel - 1.0f) * durationPerPower);

        // 2. 1回あたりの弾数: Power 5.0 以上で 3 発 (未満は 2 発)
        int shotCount = (powerLevel >= 5.0f) ? 3 : 2;

        // 3. 弾速: Power 9.0 以上で 12.0f (未満は 8.0f)
        float currentBulletSpeed = (powerLevel >= 9.0f) ? 12.0f : 8.0f;

        // 生成基準のY座標 (画面上部)
        float spawnY = targetField.bounds.yMax - 0.2f;

        // 落下範囲のX座標限界 (フィールド枠外に出ないよう制限)
        float minX = Mathf.Max(targetField.bounds.xMin + 0.3f, targetX - spreadRadius);
        float maxX = Mathf.Min(targetField.bounds.xMax - 0.3f, targetX + spreadRadius);

        float timer = 0f;

        while (timer < totalDuration)
        {
            // 1回の連射タイミングにつき shotCount 個の弾をランダムな位置から真下へ発射
            for (int i = 0; i < shotCount; i++)
            {
                float spawnX = Random.Range(minX, maxX);
                Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

                GameObject bObj = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(bulletPrefab, spawnPos, Quaternion.identity)
                    : Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

                EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(targetField, Vector3.down, currentBulletSpeed, bulletDamage);
                }
            }

            yield return new WaitForSeconds(fireInterval);
            timer += fireInterval;
        }

        Destroy(gameObject);
    }

    private float GetTargetPlayerX(PlayField field)
    {
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var player in players)
        {
            if (player != null && field != null)
            {
                Vector3 pos = player.transform.position;
                if (pos.x >= field.bounds.xMin && pos.x <= field.bounds.xMax)
                {
                    return pos.x;
                }
            }
        }
        return field != null ? field.bounds.center.x : 0f;
    }
}