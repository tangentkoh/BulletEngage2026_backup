using System.Collections;
using UnityEngine;

public class FridaLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName;

    [Header("矢弾プレハブ")]
    [SerializeField] private GameObject arrowBulletPrefab;

    [Header("持続時間設定 (パワー依存)")]
    [Tooltip("Power 1.0 時の持続時間 (秒)")]
    [SerializeField] private float baseDuration = 1.8f;
    [Tooltip("Power 1.0 ごとの延長時間 (秒)")]
    [SerializeField] private float durationPerPower = 0.2f;

    [Header("矢弾スペック")]
    [SerializeField] private float baseArrowSpeed = 10.0f;
    [SerializeField] private float arrowDamage = 15f;
    [SerializeField] private float fireInterval = 0.1f;

    [Header("掃射移動設定 (速度一定)")]
    [SerializeField] private float sweepSpeed = 4.5f;
    [SerializeField] private float edgeMarginX = 0.2f;

    private PlayField targetField;
    private float powerLevel = 1.0f;
    public string AttackName => attackName;

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(PursuitArrowRoutine());
    }

    private IEnumerator PursuitArrowRoutine()
    {
        if (targetField == null || arrowBulletPrefab == null) yield break;

        // パワーに応じた持続時間を算出
        float totalDuration = baseDuration + ((powerLevel - 1.0f) * durationPerPower);

        // Power 9.0 到達で弾速1.5倍
        float currentArrowSpeed = (powerLevel >= 9.0f) ? (baseArrowSpeed * 1.5f) : baseArrowSpeed;

        // Power 5.0 以上で右端からの同時放流フラグを有効化
        bool enableDoubleSweep = powerLevel >= 5.0f;

        float leftX = targetField.bounds.xMin + edgeMarginX;
        float rightX = targetField.bounds.xMax - edgeMarginX;
        float spawnY = targetField.bounds.yMax - 0.15f;

        // 左端スタート用
        float posX_LeftStart = leftX;
        int dir_LeftStart = 1;

        // 右端スタート用 (Power 5.0以上)
        float posX_RightStart = rightX;
        int dir_RightStart = -1;

        float timer = 0f;
        float fireTimer = 0f;

        while (timer < totalDuration)
        {
            float dt = Time.deltaTime;
            timer += dt;
            fireTimer += dt;

            // 1. 通常掃射（左スタート）の往復計算
            posX_LeftStart += dir_LeftStart * sweepSpeed * dt;
            if (posX_LeftStart >= rightX)
            {
                posX_LeftStart = rightX;
                dir_LeftStart = -1;
            }
            else if (posX_LeftStart <= leftX)
            {
                posX_LeftStart = leftX;
                dir_LeftStart = 1;
            }

            // 2. 追加掃射（右スタート）の往復計算
            if (enableDoubleSweep)
            {
                posX_RightStart += dir_RightStart * sweepSpeed * dt;
                if (posX_RightStart <= leftX)
                {
                    posX_RightStart = leftX;
                    dir_RightStart = 1;
                }
                else if (posX_RightStart >= rightX)
                {
                    posX_RightStart = rightX;
                    dir_RightStart = -1;
                }
            }

            // 発射処理
            if (fireTimer >= fireInterval)
            {
                fireTimer -= fireInterval;

                // 左スタートラインからの発射
                SpawnArrow(new Vector3(posX_LeftStart, spawnY, 0f), currentArrowSpeed);

                // Power 5.0以上なら右スタートラインからも同時に発射
                if (enableDoubleSweep)
                {
                    SpawnArrow(new Vector3(posX_RightStart, spawnY, 0f), currentArrowSpeed);
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawnArrow(Vector3 position, float speed)
    {
        GameObject arrow = EnemyBulletPool.Instance != null
            ? EnemyBulletPool.Instance.Get(arrowBulletPrefab, position, Quaternion.identity)
            : Instantiate(arrowBulletPrefab, position, Quaternion.identity);
        EnemyBullet eb = arrow.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.Initialize(targetField, Vector3.down, speed, arrowDamage);
        }
    }
}