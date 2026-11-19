using System.Collections;
using UnityEngine;

public class NanaLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName;
    public string AttackName => attackName;

    [Header("弾プレハブ")]
    [Tooltip("CurvingEnemyBulletコンポーネントがアタッチされた弾プレハブ")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("基本パラメータ")]
    [SerializeField] private float baseBulletSpeed = 4.0f;
    [SerializeField] private float bulletDamage = 7.0f;
    [Tooltip("カーブの強さ（度/秒）")]
    [SerializeField] private float curveSpeedMagnitude = 35.0f;

    [Header("生成位置・マージン設定")]
    [Tooltip("フィールド上端からの基本Y座標マージン")]
    [SerializeField] private float topMargin = 0.5f;
    [SerializeField] private float verticalRange = 2.5f;
    [SerializeField] private float horizontalRangeRatio = 0.5f;

    private PlayField targetField;
    private float power;

    public void Initialize(PlayField target, float p)
    {
        targetField = target;
        power = p;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        if (targetField == null || bulletPrefab == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // 1. 1.0ごとに弾数が1増加（初期値1.0で9発）
        int bulletCount = 9 + Mathf.FloorToInt(Mathf.Max(0f, power - 1.0f));

        // 2. パワー5.0以上で1つの円形において2連射
        bool isDoubleShot = power >= 5.0f;

        // 3. パワー9.0以上で弾速1.4倍
        float speedMultiplier = (power >= 9.0f) ? 1.4f : 1.0f;
        float finalSpeed = baseBulletSpeed * speedMultiplier;

        // 計7回の円形弾幕を発射
        for (int wave = 0; wave < 7; wave++)
        {
            // 相手フィールド上部のランダムな座標 (X/Y共にばらつき適用)
            Vector3 spawnPos = GetRandomTopPosition();

            // 円形ごとにカーブの向きを左か右かに統一
            float curveSpeed = (Random.value < 0.5f) ? curveSpeedMagnitude : -curveSpeedMagnitude;

            // 1回目の円形弾幕発射
            SpawnRingBullets(spawnPos, bulletCount, finalSpeed, curveSpeed, 0f);

            // パワー5.0以上ならわずかなディレイを挟んで2連射目を発射
            if (isDoubleShot)
            {
                yield return new WaitForSeconds(0.05f);
                // 2連射目は少し角度（オフセット）をずらして隙間を埋める
                float angleOffset = (360f / bulletCount) * 0.5f;
                SpawnRingBullets(spawnPos, bulletCount, finalSpeed, curveSpeed, angleOffset);
            }

            // 0.3秒間隔で計7回
            yield return new WaitForSeconds(0.3f);
        }

        // 全ウェーブ完了後オブジェクト破棄
        Destroy(gameObject);
    }

    private void SpawnRingBullets(Vector3 centerPos, int count, float speed, float curveSpeed, float angleOffset)
    {
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = (i * angleStep) + angleOffset;
            // 全方向に広がる単位ベクトルを計算
            Vector3 dir = Quaternion.Euler(0f, 0f, currentAngle) * Vector3.down;

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(bulletPrefab, centerPos, Quaternion.identity)
                : Instantiate(bulletPrefab, centerPos, Quaternion.identity);
            CurvingEnemyBullet bullet = bObj.GetComponent<CurvingEnemyBullet>();
            if (bullet != null)
            {
                bullet.InitializeCurving(targetField, dir, speed, bulletDamage, curveSpeed);
            }
        }
    }

    private Vector3 GetRandomTopPosition()
    {
        Rect bounds = targetField.bounds;

        // --- X軸のランダム座標 ---
        float centerX = bounds.center.x;
        float width = bounds.width;
        float halfWidth = (width * horizontalRangeRatio) * 0.5f;
        float randomX = Random.Range(centerX - halfWidth, centerX + halfWidth);

        // --- Y軸のランダム座標 ---
        float highestY = bounds.yMax - topMargin;
        float lowestY = highestY - Mathf.Max(0f, verticalRange);
        float randomY = Random.Range(lowestY, highestY);

        return new Vector3(randomX, randomY, 0f);
    }
}