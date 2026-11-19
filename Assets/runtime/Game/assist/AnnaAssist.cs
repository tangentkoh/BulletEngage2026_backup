using System.Collections;
using UnityEngine;

public class AnnaAssist : AssistantBossBase
{
    [Header("弾設定")]
    [Tooltip("やや小型の星弾プレハブ (DeleteBulletコンポーネント付き)")]
    [SerializeField] private GameObject smallStarBulletPrefab;
    [SerializeField] private float starSpeed = 3.0f;
    [SerializeField] private float starDamage = 15.0f;
    [SerializeField] private float dropInterval = 2.0f;

    [Header("主役ボス延長時間設定")]
    [SerializeField] private float extraBossDuration = 5.0f;

    [Header("生成マージン設定")]
    [Tooltip("画面端からのX軸スポーン禁止マージン")]
    [SerializeField] private float sideMargin = 1.0f;

    private Coroutine barrageCoroutine;

    public override void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        base.Initialize(field, mainBoss);

        // 主役ボスの生存時間を+5秒延長
        if (mainBoss != null)
        {
            mainBoss.AddRemainingTime(extraBossDuration);
        }

        barrageCoroutine = StartCoroutine(StarRainLoop());
    }

    private IEnumerator StarRainLoop()
    {
        yield return new WaitForSeconds(0.5f); // 登場待ち

        while (true)
        {
            yield return new WaitForSeconds(dropInterval);
            if (targetField == null || smallStarBulletPrefab == null) yield break;

            Rect bounds = targetField.bounds;

            // 画面上部のランダムなX座標
            float spawnX = Random.Range(bounds.xMin + sideMargin, bounds.xMax - sideMargin);
            Vector3 spawnPos = new Vector3(spawnX, bounds.yMax + 0.6f, 0f);

            // 垂直下方向（真下）に小型星弾を射出
            GameObject bObj = Instantiate(smallStarBulletPrefab, spawnPos, Quaternion.identity);
            DeleteBullet bullet = bObj.GetComponent<DeleteBullet>();
            if (bullet != null)
            {
                // isStarMode = true で自機弾貫通 & 被弾時非消滅モードで起動
                bullet.Initialize(targetField, Vector3.down, starSpeed, starDamage, isStarMode: true);
            }
        }
    }

    protected override void Dismiss()
    {
        if (barrageCoroutine != null) StopCoroutine(barrageCoroutine);
        base.Dismiss();
    }
}