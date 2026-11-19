using System.Collections;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

public class SigureLv2 : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定")]
    [SerializeField] private string attackName = "スプレッドグレネード";
    public string AttackName => attackName;

    [Header("使用する弾プレハブ")]
    [Tooltip("飛翔するグレネード弾(中玉など)")]
    [SerializeField] private GameObject grenadePrefab;
    [Tooltip("炸裂時に円形に広がる散弾(小玉など)")]
    [SerializeField] private GameObject shrapnelPrefab;

    [Header("グレネード飛翔設定")]
    [SerializeField] private float flyDuration = 1f;
    [SerializeField] private float grenadeDamage = 60f;

    [Header("炸裂散弾スペック")]
    [SerializeField] private float shrapnelSpeed = 6.0f;
    [SerializeField] private float shrapnelDamage = 25f;

    [Header("基本スペック (パワー値計算用)")]
    [Tooltip("Power 1.0 時の円形散弾数")]
    [SerializeField] private int baseRingCount = 10;
    [Tooltip("Power 1.0 時のグレネード発射数")]
    [SerializeField] private int baseGrenadeCount = 3;

    private PlayField targetField;
    private float powerLevel = 1.0f;

    public void Initialize(PlayField field, float power)
    {
        targetField = field;
        powerLevel = Mathf.Clamp(power, 1.0f, 9.0f);
        StartCoroutine(SpreadGrenadeRoutine());
    }

    private IEnumerator SpreadGrenadeRoutine()
    {
        if (targetField == null || grenadePrefab == null || shrapnelPrefab == null) yield break;

        // 1. 円形散弾数の計算 (0.5ごとに +1発)
        // (powerLevel - 1.0) / 0.5 = パワー上昇ステップ数
        int extraRingBullets = Mathf.FloorToInt((powerLevel - 1.0f) / 0.5f);
        int totalRingBullets = baseRingCount + extraRingBullets;

        // 2. 同時打ち出しグレネード数の計算
        int extraGrenades = 0;
        if (powerLevel >= 3.0f) extraGrenades++;
        if (powerLevel >= 5.0f) extraGrenades+=2;
        if (powerLevel >= 7.0f) extraGrenades++;
        if (powerLevel >= 9.0f) extraGrenades+=3;
        int totalGrenadeCount = baseGrenadeCount + extraGrenades;

        // 相手フィールドの最上部中央から射出
        Vector3 spawnOrigin = new Vector3(targetField.bounds.center.x, targetField.bounds.yMax - 0.1f, 0f);

        // 各グレネードを並行して発射・炸裂
        for (int i = 0; i < totalGrenadeCount; i++)
        {
            // 相手フィールドの上半分 (center.y 〜 yMax) のランダム座標を目標にする
            float targetX = Random.Range(targetField.bounds.xMin + 0.5f, targetField.bounds.xMax - 0.5f);
            float targetY = Random.Range(targetField.bounds.center.y + 0.3f, targetField.bounds.yMax - 0.5f);
            Vector3 targetPos = new Vector3(targetX, targetY, 0f);

            StartCoroutine(ExecuteSingleGrenade(spawnOrigin, targetPos, totalRingBullets));
        }

        // 全弾の飛翔・炸裂完了を待って自身を破棄
        yield return new WaitForSeconds(flyDuration + 0.1f);
        Destroy(gameObject);
    }

    private IEnumerator ExecuteSingleGrenade(Vector3 startPos, Vector3 targetPos, int ringBulletCount)
    {
        GameObject grenade = EnemyBulletPool.Instance != null
            ? EnemyBulletPool.Instance.Get(grenadePrefab, startPos, Quaternion.identity)
            : Instantiate(grenadePrefab, startPos, Quaternion.identity);
        EnemyBullet eb = grenade.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            // 飛翔中も接触判定を持たせる
            eb.Initialize(targetField, Vector3.zero, 0f, grenadeDamage);
        }

        // LitMotion で目標座標へ減速移動 (OutQuad)
        LMotion.Create(startPos, targetPos, flyDuration)
            .WithEase(Ease.OutQuad)
            .BindToPosition(grenade.transform)
            .AddTo(grenade);

        yield return new WaitForSeconds(flyDuration);

        // 炸裂位置を確定してグレネードを消滅
        Vector3 explodePos = (grenade != null) ? grenade.transform.position : targetPos;
        if (grenade != null) Destroy(grenade);

        // バトル決着済み等でフィールドが無効化されている場合は炸裂キャンセル
        if (targetField == null) yield break;

        // ランダムな初期角度から均等に円形全方位へ散弾を発射
        float randomBaseAngle = Random.Range(0f, 360f);
        float angleStep = 360f / ringBulletCount;

        for (int i = 0; i < ringBulletCount; i++)
        {
            float angleDeg = randomBaseAngle + (angleStep * i);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            GameObject shrapnel = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(shrapnelPrefab, explodePos, Quaternion.identity)
                : Instantiate(shrapnelPrefab, explodePos, Quaternion.identity);
            EnemyBullet shrapnelEb = shrapnel.GetComponent<EnemyBullet>();
            if (shrapnelEb != null)
            {
                shrapnelEb.Initialize(targetField, shootDir, shrapnelSpeed, shrapnelDamage);
            }
        }
    }
}
