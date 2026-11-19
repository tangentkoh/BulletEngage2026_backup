using Cysharp.Threading.Tasks;
using UnityEngine;

public class BulletSender : MonoBehaviour
{
    public static BulletSender Instance { get; private set; }

    [Header("敵弾プレハブ")]
    [SerializeField] private GameObject smallBulletPrefab;
    [SerializeField] private GameObject mediumBulletPrefab;

    [Header("予告演出プレハブ")]
    [Tooltip("敵撃破によるばらまき弾幕用の警告マーカー")]
    [SerializeField] private GameObject warningMarkerPrefab;
    [Tooltip("ジャマースキル発動時専用の警告マーカー")]
    [SerializeField] private GameObject jammerWarningMarkerPrefab;

    [Header("撃破・ジャマー固定設定")]
    [SerializeField] private int deathBarrageCount = 5;  // 撃破時に送る弾数 (固定5)
    [SerializeField] private int requiredKillsForJammer = 5; // ジャマー発動に必要な撃破数 (固定5)

    // 各プレイヤーの撃破カウンター (0: 1P, 1: 2P)
    private int[] playerKillCounts = new int[2];

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 敵撃破時に呼ばれる
    public void NotifyEnemyDefeated(int killerPlayerIndex, PlayField opponentField)
    {
        if (opponentField == null || (BattleGameManager.Instance != null && BattleGameManager.Instance.IsGameOver)) return;

        // 1. 相手フィールドへ5発の撃破弾幕を送信
        SendBulletAsync(opponentField, deathBarrageCount).Forget();

        // 2. 撃破カウントを加算し、5体に到達したら警告を挟んでジャマー送信
        if (killerPlayerIndex >= 0 && killerPlayerIndex < playerKillCounts.Length)
        {
            playerKillCounts[killerPlayerIndex]++;
            if (playerKillCounts[killerPlayerIndex] >= requiredKillsForJammer)
            {
                playerKillCounts[killerPlayerIndex] = 0;
                TriggerJammerWithWarningAsync(killerPlayerIndex, opponentField).Forget();
            }
        }
    }

    // Lv1ノーマルアタック等の相手フィールド送信窓口
    public void SendAttackToOpponent(PlayField targetField)
    {
        if (targetField == null || (BattleGameManager.Instance != null && BattleGameManager.Instance.IsGameOver)) return;
        SendBulletAsync(targetField, deathBarrageCount).Forget();
    }

    // --- 撃破弾幕送信 (通常警告) ---
    private async UniTaskVoid SendBulletAsync(PlayField targetField, int count)
    {
        float spawnX = Random.Range(targetField.bounds.xMin + 0.5f, targetField.bounds.xMax - 0.5f);
        float spawnY = targetField.bounds.yMax;
        Vector3 spawnPoint = new Vector3(spawnX, spawnY, 0f);

        GameObject warning = null;
        if (warningMarkerPrefab != null)
        {
            warning = Instantiate(warningMarkerPrefab, spawnPoint, Quaternion.identity);
        }

        await UniTask.WaitForSeconds(1.0f, cancellationToken: this.GetCancellationTokenOnDestroy());

        if (warning != null) Destroy(warning);
        if (targetField == null || (BattleGameManager.Instance != null && BattleGameManager.Instance.IsGameOver)) return;

        for (int i = 0; i < count; i++)
        {
            bool isMedium = Random.value < 0.2f;
            GameObject prefab = isMedium ? mediumBulletPrefab : smallBulletPrefab;
            float damage = isMedium ? 20f : 10f;
            float speed = Random.Range(2.5f, 4.5f);

            float angle = Random.Range(-105f, -75f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            if (prefab != null)
            {
                GameObject b = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(prefab, spawnPoint, Quaternion.identity)
                    : Instantiate(prefab, spawnPoint, Quaternion.identity);
                EnemyBullet eb = b.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(targetField, dir, speed, damage);
                }
            }
        }
    }

    // --- ジャマースキル送信 (相手フィールド上部中央へジャマー専用警告) ---
    private async UniTaskVoid TriggerJammerWithWarningAsync(int playerIndex, PlayField targetField)
    {
        Vector3 spawnPoint = new Vector3(targetField.bounds.center.x, targetField.bounds.yMax, 0f);
        GameObject markerToUse = jammerWarningMarkerPrefab != null ? jammerWarningMarkerPrefab : warningMarkerPrefab;

        GameObject warning = null;
        if (markerToUse != null)
        {
            warning = Instantiate(markerToUse, spawnPoint, Quaternion.identity);
        }

        await UniTask.WaitForSeconds(1.0f, cancellationToken: this.GetCancellationTokenOnDestroy());

        if (warning != null) Destroy(warning);
        if (targetField == null || (BattleGameManager.Instance != null && BattleGameManager.Instance.IsGameOver)) return;

        AssistantManager.Instance?.ExecuteJammer(playerIndex, targetField);
    }
}