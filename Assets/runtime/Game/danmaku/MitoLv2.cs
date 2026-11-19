using System.Collections;
using UnityEngine;

public class MitoLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName = "事象の収束";
    public string AttackName => attackName;

    [Header("使用する弾プレハブ (EnemyBullet付き)")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("基本スペック")]
    [SerializeField] private float bulletSpeed = 10.0f;
    [SerializeField] private float baseDamage = 15.0f;
    [SerializeField] private float baseFireInterval = 0.5f;

    [Header("基本回数・弾数")]
    [SerializeField] private int baseShotCount = 8;     // 同時発射数
    [SerializeField] private int baseWaveCount = 3;     // 発射回数

    private PlayField targetField;
    private float powerLevel = 1.0f;

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(EventConvergenceRoutine());
    }

    private IEnumerator EventConvergenceRoutine()
    {
        if (targetField == null || bulletPrefab == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // --- パワー値に応じた各種パラメータ計算 ---
        // 1. 同時発射数: 1.0につき+1 (初期8個)
        int currentShotCount = baseShotCount + Mathf.FloorToInt(powerLevel - 1.0f);

        // 2. 発射回数: 初期3回、3.0, 5.0, 7.0, 9.0 到達で+1ずつ増加 (最大7回)
        int currentWaveCount = baseWaveCount;
        if (powerLevel >= 3.0f) currentWaveCount++;
        if (powerLevel >= 5.0f) currentWaveCount++;
        if (powerLevel >= 7.0f) currentWaveCount++;
        if (powerLevel >= 9.0f) currentWaveCount++;

        // 3. 連射速度: Power 5.0 以上で 0.1s 短縮 (0.5s -> 0.4s)
        float currentInterval = (powerLevel >= 5.0f) ? (baseFireInterval - 0.1f) : baseFireInterval;

        // 4. ダメージ: Power 9.0 以上で 5f 増加 (15f -> 20f)
        float currentDamage = (powerLevel >= 9.0f) ? (baseDamage + 5.0f) : baseDamage;

        // 相手フィールドの中心座標を取得
        Vector3 centerPos = targetField.bounds.center;

        // 連射ループ
        for (int w = 0; w < currentWaveCount; w++)
        {
            for (int i = 0; i < currentShotCount; i++)
            {
                // 画面上半分の画面端からランダムな生成座標を決定
                Vector3 spawnPos = GetRandomUpperEdgePosition(targetField.bounds);

                // 中央へ向かうベクトルを計算
                Vector3 shootDir = (centerPos - spawnPos).normalized;

                // 弾の生成と初期化
                GameObject bObj = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(bulletPrefab, spawnPos, Quaternion.identity)
                    : Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

                EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(targetField, shootDir, bulletSpeed, currentDamage);
                }
            }

            yield return new WaitForSeconds(currentInterval);
        }

        Destroy(gameObject);
    }

    private Vector3 GetRandomUpperEdgePosition(Rect bounds)
    {
        float xMin = bounds.xMin;
        float xMax = bounds.xMax;
        float yCenter = bounds.center.y;
        float yMax = bounds.yMax;

        // 3つのセグメント（左上・上・右上）の長さを計算して均等に抽選
        float leftLen = yMax - yCenter;
        float topLen = xMax - xMin;
        float rightLen = yMax - yCenter;
        float totalLen = leftLen + topLen + rightLen;

        float rnd = Random.Range(0f, totalLen);

        if (rnd < leftLen)
        {
            // 左辺の上半分
            return new Vector3(xMin, yCenter + rnd, 0f);
        }
        else if (rnd < leftLen + topLen)
        {
            // 上辺（天井）
            return new Vector3(xMin + (rnd - leftLen), yMax, 0f);
        }
        else
        {
            // 右辺の上半分
            return new Vector3(xMax, yCenter + (rnd - leftLen - topLen), 0f);
        }
    }
}