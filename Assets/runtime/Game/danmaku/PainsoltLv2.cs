using System.Collections;
using UnityEngine;

public class PainsoltLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName = "フォールスウィート";
    public string AttackName => attackName;

    [Header("使用する弾プレハブ (FallingSweetBullet付き)")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("弾数設定 (パワー依存)")]
    [Tooltip("Power 1.0 時の1ウェーブあたり弾数")]
    [SerializeField] private int baseBulletCount = 20;
    [Tooltip("Power 1.0 ごとの増加弾数")]
    [SerializeField] private int countPerPower = 4;

    [Header("待機時間・速度ランダム設定")]
    [SerializeField] private float fallDelay = 0.5f;             // 静止待機時間
    [SerializeField] private float minAccel = 4.0f;               // 最小加速度
    [SerializeField] private float maxAccel = 5.0f;               // 最大加速度
    [SerializeField] private float edgeMarginX = 0.5f;

    private PlayField targetField;
    private float powerLevel = 1.0f;

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(FallSweetRoutine());
    }

    private IEnumerator FallSweetRoutine()
    {
        if (targetField == null || bulletPrefab == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // --- パワー値による補正計算 ---
        // 1. 1ウェーブあたりの弾数 (初期20個、Power 1.0ごとに+4個)
        int bulletCount = baseBulletCount + Mathf.RoundToInt((powerLevel - 1.0f) * countPerPower);

        // 2. 威力 (Power 9.0以上で 25、未満は 15)
        float bulletDamage = (powerLevel >= 9.0f) ? 25.0f : 15.0f;

        // 3. ウェーブ数 (Power 5.0以上で 2ウェーブ)
        int waveCount = (powerLevel >= 5.0f) ? 2 : 1;

        for (int w = 0; w < waveCount; w++)
        {
            SpawnSweetWave(bulletCount, bulletDamage);

            // 2ウェーブ目の場合は1.5秒待機
            if (w < waveCount - 1)
            {
                yield return new WaitForSeconds(1.5f);
            }
        }

        // 弾の全放出完了後、オブジェクトを破棄 (各弾は独立して制御)
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }


    private void SpawnSweetWave(int count, float damage)
    {
        float minX = targetField.bounds.xMin + edgeMarginX;
        float maxX = targetField.bounds.xMax - edgeMarginX;
        float minY = targetField.bounds.yMax - 0.8f;
        float maxY = targetField.bounds.yMax - 0.2f;

        for (int i = 0; i < count; i++)
        {
            // 画面上部エリア内のランダム位置
            float spawnX = Random.Range(minX, maxX);
            float spawnY = Random.Range(minY, maxY);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            // 下方向ベース + わずかな角度ランダム（±10度）
            float randomAngle = Random.Range(-100f, -80f) * Mathf.Deg2Rad;
            Vector3 moveDir = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f).normalized;

            // 初速（0〜1.0）と加速度のランダム化
            float startSpeed = Random.Range(0f, 1.0f);
            float accel = Random.Range(minAccel, maxAccel);

            GameObject bObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            FallingBullet bullet = bObj.GetComponent<FallingBullet>();
            if (bullet != null)
            {
                bullet.Initialize(targetField, fallDelay, moveDir, startSpeed, accel, damage);
            }
        }
    }
}