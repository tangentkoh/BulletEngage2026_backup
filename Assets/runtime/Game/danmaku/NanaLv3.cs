using System.Collections;
using UnityEngine;

public class NanaLv3 : EngageBossBase
{
    [Header("特殊弾プレハブ")]
    [Tooltip("NanaLv3Bulletコンポーネントがアタッチされた特殊弾プレハブ")]
    [SerializeField] private GameObject specialBulletPrefab;

    [Header("特殊弾パラメータ")]
    [SerializeField] private float specialBulletSpeed = 5.5f;
    [SerializeField] private float specialBulletDamage = 20.0f;

    [Header("連射速度設定")]
    [Tooltip("Power 1.0 時の発射間隔(秒)")]
    [SerializeField] private float baseInterval = 2.6f;
    [Tooltip("Power 1.0 あたりの短縮時間(秒)")]
    [SerializeField] private float intervalReductionPerPower = 0.2f;

    [Header("円形散弾数設定")]
    [Tooltip("Power 1.0〜4.5 時の円形散弾数")]
    [SerializeField] private int baseRingCount = 9;

    private Coroutine barrageCoroutine;

    protected override void StartBarrage()
    {
        barrageCoroutine = StartCoroutine(SpecialBarrageLoop());
    }

    protected override void StopBarrage()
    {
        if (barrageCoroutine != null)
        {
            StopCoroutine(barrageCoroutine);
            barrageCoroutine = null;
        }
    }

    private IEnumerator SpecialBarrageLoop()
    {
        if (specialBulletPrefab == null) yield break;

        while (true)
        {
            // --- パワー値に応じたパラメータ計算 ---
            // 1. 連射間隔: 1.0あたり 0.2s 短縮 (例: 1.0 -> 3.2s, 9.0 -> 1.6s)
            float currentInterval = Mathf.Max(0.5f, baseInterval - ((currentPower - 1.0f) * intervalReductionPerPower));

            // 2. 円形散弾数: 初期値9個。5.0到達で+9、9.0到達でさらに+9
            int currentRingCount = baseRingCount;
            if (currentPower >= 5.0f) currentRingCount += 9;
            if (currentPower >= 9.0f) currentRingCount += 9;

            // ターゲット（自機）の方向を取得
            Transform playerT = GetTargetPlayer();
            Vector3 targetPos = (playerT != null) ? playerT.position : (transform.position + Vector3.down);
            Vector3 shootDir = (targetPos - transform.position).normalized;

            // 特殊弾の生成と初期化
            GameObject bObj = Instantiate(specialBulletPrefab, transform.position, Quaternion.identity);
            BoomBullet nBullet = bObj.GetComponent<BoomBullet>();
            if (nBullet != null)
            {
                nBullet.Initialize(targetField, shootDir, specialBulletSpeed, specialBulletDamage, currentRingCount);
            }

            yield return new WaitForSeconds(currentInterval);
        }
    }

    private Transform GetTargetPlayer()
    {
        if (targetField == null) return null;
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            Rect bounds = targetField.bounds;
            if (p.transform.position.x >= bounds.xMin && p.transform.position.x <= bounds.xMax)
            {
                return p.transform;
            }
        }
        return null;
    }
}