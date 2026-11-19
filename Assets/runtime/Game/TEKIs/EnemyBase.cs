using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public enum EnemyMovePattern
{
    Linear,
    ZigZag,
    UTurn,
    Roam
}

public enum MidBossRewardType
{
    None,
    HealHP50,
    HealEnergy50
}

public class EnemyBase : MonoBehaviour
{
    [Header("移動パターン選択")]
    [SerializeField] private EnemyMovePattern movePattern = EnemyMovePattern.Linear;

    [Header("1. 直線 (Linear) 設定")]
    [SerializeField] private Vector2 linearDirection = Vector2.down;
    [SerializeField] private float linearSpeed = 3.0f;

    [Header("2. ジグザグ (ZigZag) 設定")]
    [SerializeField] private float zigZagSpeed = 2.5f;
    [SerializeField] private float horizontalDistance = 1.8f;
    [SerializeField] private float stepDownDistance = 1.2f;
    [SerializeField] private float zigZagBaseAngle = -90f;
    [SerializeField] private bool startMovingRight = true;

    [Header("3. Uターン (UTurn) 設定")]
    [SerializeField] private Vector2 uTurnEnterDir = Vector2.down;
    [SerializeField] private float uTurnEnterDist = 3.5f;
    [SerializeField] private float uTurnEnterTime = 1.5f;
    [SerializeField] private float uTurnStayTime = 2.5f;
    [SerializeField] private float uTurnExitTime = 1.5f;

    [Header("4. 徘徊 (Roam / 特殊敵) 設定")]
    [SerializeField] private float roamEnterTime = 1.2f;
    [SerializeField] private float roamDuration = 8.0f;
    [SerializeField] private float roamSpeed = 2.2f;
    [SerializeField] private float roamExitSpeed = 5.0f;
    [SerializeField] private float roamBaseAngle = 0f; // 徘徊の基準方向（0度:水平基準、±30度等で斜め徘徊傾向に）

    [Header("敵ステータス")]
    [SerializeField] private float maxHP = 20f;
    [SerializeField] private float energyReward = 2.0f;
    [SerializeField] private bool isMidBoss = false;
    [SerializeField] private MidBossRewardType rewardType = MidBossRewardType.None;

    [Header("特殊敵：花火弾幕設定")]
    [SerializeField] private GameObject specialBulletPrefab;
    [SerializeField] private int fireworkBulletCount = 12;
    [SerializeField] private float fireworkBulletSpeed = 3.5f;
    [SerializeField] private float fireworkInterval = 1.8f;
    [SerializeField] private float rotationSpeedPerWave = 15f;

    [Header("共通エフェクト")]
    [SerializeField] private GameObject explosionPrefab;

    private float currentHP;
    private bool isDead = false;
    public PlayField CurrentField { get; set; }

    private Vector3 startPosition;
    private MotionHandle activeMotion;
    private float currentFireworkAngle = 0f;

    private void Start()
    {
        currentHP = maxHP;
        startPosition = transform.position;

        if (CurrentField == null)
        {
            PlayField[] fields = FindObjectsByType<PlayField>(FindObjectsInactive.Exclude);
            foreach (var f in fields)
            {
                if (transform.position.x >= f.bounds.xMin && transform.position.x <= f.bounds.xMax)
                {
                    CurrentField = f;
                    break;
                }
            }
        }

        switch (movePattern)
        {
            case EnemyMovePattern.ZigZag:
                ZigZagLoopAsync().Forget();
                break;
            case EnemyMovePattern.UTurn:
                UTurnSequenceAsync().Forget();
                break;
            case EnemyMovePattern.Roam:
                RoamSequenceAsync().Forget();
                break;
        }

        if (isMidBoss && specialBulletPrefab != null)
        {
            FireworkBarrageLoopAsync().Forget();
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (movePattern == EnemyMovePattern.Linear)
        {
            transform.position += (Vector3)(linearDirection.normalized * (linearSpeed * Time.deltaTime));
            CheckDespawn();
        }
    }

    // --- ジグザグ移動 (UniTask) ---
    private async UniTaskVoid ZigZagLoopAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        int dirX = startMovingRight ? 1 : -1;
        float rad = zigZagBaseAngle * Mathf.Deg2Rad;
        Vector3 downDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        while (!isDead)
        {
            float traveledX = 0f;
            Vector3 sideDir = Vector3.Cross(downDir, Vector3.forward) * dirX;
            while (traveledX < horizontalDistance)
            {
                float step = zigZagSpeed * Time.deltaTime;
                transform.position += sideDir * step;
                traveledX += step;
                await UniTask.Yield(ct);
            }

            float traveledY = 0f;
            while (traveledY < stepDownDistance)
            {
                float step = zigZagSpeed * Time.deltaTime;
                transform.position += downDir * step;
                traveledY += step;
                await UniTask.Yield(ct);
            }

            dirX = -dirX;
            CheckDespawn();
        }
    }

    // --- Uターン移動 (LitMotion & UniTask) ---
    private async UniTaskVoid UTurnSequenceAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        Vector3 enterDir = (Vector3)uTurnEnterDir.normalized;
        Vector3 targetPos = startPosition + enterDir * (uTurnEnterDist + UnityEngine.Random.Range(-0.5f, 0.5f));

        // 1. スムーズ進入 (EaseInOutQuad)
        activeMotion = LMotion.Create(startPosition, targetPos, uTurnEnterTime)
            .WithEase(Ease.InOutQuad)
            .BindToPosition(transform);

        await UniTask.WaitForSeconds(uTurnEnterTime, cancellationToken: ct);

        // 2. 滞在
        await UniTask.WaitForSeconds(uTurnStayTime, cancellationToken: ct);

        // 3. スムーズ離脱
        activeMotion = LMotion.Create(targetPos, startPosition, uTurnExitTime)
            .WithEase(Ease.InOutQuad)
            .BindToPosition(transform);

        await UniTask.WaitForSeconds(uTurnExitTime, cancellationToken: ct);

        Despawn();
    }

    // --- フィールド上半分限定 徘徊移動 (角度指定 & LitMotion) ---
    private async UniTaskVoid RoamSequenceAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        if (CurrentField == null) return;

        float fieldMidY = CurrentField.bounds.center.y;
        float upperMaxY = CurrentField.bounds.yMax - 0.8f;
        float upperMinY = fieldMidY + 0.2f;

        Vector3 enterTargetPos = new Vector3(transform.position.x, (upperMaxY + upperMinY) * 0.5f, 0f);

        // 進入 (EaseOutQuad)
        activeMotion = LMotion.Create(startPosition, enterTargetPos, roamEnterTime)
            .WithEase(Ease.OutQuad)
            .BindToPosition(transform)
            .AddTo(gameObject);

        await UniTask.WaitForSeconds(roamEnterTime, cancellationToken: ct);

        // 徘徊初期角度の設定
        float baseRad = (roamBaseAngle + UnityEngine.Random.Range(-5f, 5f)) * Mathf.Deg2Rad;
        Vector2 roamDir = new Vector2(Mathf.Cos(baseRad), Mathf.Sin(baseRad)).normalized;
        if (UnityEngine.Random.value < 0.5f) roamDir.x = -roamDir.x;

        float roamTimer = 0f;
        while (roamTimer < roamDuration)
        {
            roamTimer += Time.deltaTime;
            transform.position += (Vector3)(roamDir * (roamSpeed * Time.deltaTime));

            // 上半分エリア内での反射
            if (transform.position.x < CurrentField.bounds.xMin + 0.6f || transform.position.x > CurrentField.bounds.xMax - 0.6f)
            {
                roamDir.x = -roamDir.x;
            }
            if (transform.position.y < upperMinY || transform.position.y > upperMaxY)
            {
                roamDir.y = -roamDir.y;
            }

            await UniTask.Yield(ct);
        }

        // 下部へ加速離脱
        while (transform.position.y >= CurrentField.bounds.yMin - 1.5f)
        {
            transform.position += Vector3.down * (roamExitSpeed * Time.deltaTime);
            await UniTask.Yield(ct);
        }

        Despawn();
    }

    // --- 特殊敵：回転全方位弾幕 (UniTask) ---
    private async UniTaskVoid FireworkBarrageLoopAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        await UniTask.WaitForSeconds(1.0f, cancellationToken: ct);

        while (!isDead)
        {
            float angleStep = 360f / fireworkBulletCount;

            for (int i = 0; i < fireworkBulletCount; i++)
            {
                float angle = (i * angleStep + currentFireworkAngle) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                GameObject b = EnemyBulletPool.Instance != null
                    ? EnemyBulletPool.Instance.Get(specialBulletPrefab, transform.position, Quaternion.identity)
                    : Instantiate(specialBulletPrefab, transform.position, Quaternion.identity);
                EnemyBullet eb = b.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.Initialize(CurrentField, dir, fireworkBulletSpeed, 15f);
                }
            }

            currentFireworkAngle += rotationSpeedPerWave;
            await UniTask.WaitForSeconds(fireworkInterval, cancellationToken: ct);
        }
    }

    private void CheckDespawn()
    {
        if (CurrentField == null) return;

        float margin = 0.5f;
        if (transform.position.y < CurrentField.bounds.yMin - margin ||
            transform.position.x < CurrentField.bounds.xMin - margin ||
            transform.position.x > CurrentField.bounds.xMax + margin)
        {
            Despawn();
        }
    }

    private void Despawn()
    {
        if (isDead) return;
        isDead = true;

        if (activeMotion.IsActive()) activeMotion.Cancel();
        Destroy(gameObject);
    }

    public void TakeDamage(float damage, bool isChainExplosion = false)
    {
        if (isDead) return;
        currentHP -= damage;
        SoundManager.Instance?.PlayEnemyHitSE();
        if (currentHP <= 0f) Die(true);
    }

    private void Die(bool giveRewardAndSend)
    {
        if (isDead) return;
        isDead = true;

        if (activeMotion.IsActive()) activeMotion.Cancel();

        if (giveRewardAndSend)
        {
            // 1. 自機へのエネルギー加算
            AddEnergyToPlayer();

            // 2. 誘爆ボム
            if (explosionPrefab != null)
            {
                GameObject exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                ChainExplosion chain = exp.GetComponent<ChainExplosion>();
                if (chain != null) chain.Field = CurrentField;
            }

            // 3. 特殊敵報酬
            if (isMidBoss && CurrentField != null)
            {
                ApplyMidBossReward();
            }

            // 4. SE再生
            SoundManager.Instance?.PlayEnemyDefeatSE();

            // 5. 相手フィールドへの弾幕＆ジャマー通知
            if (CurrentField != null && CurrentField.OpponentField != null)
            {
                int pIndex = CurrentField.fieldIndex; // 倒した側のプレイヤーIndex
                PlayField opponent = CurrentField.OpponentField; // 相手側のフィールド

                // 第2引数には必ず【opponent】を渡す
                BulletSender.Instance?.NotifyEnemyDefeated(pIndex, opponent);
            }
        }

        Destroy(gameObject);
    }

    private void ApplyMidBossReward()
    {
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (p.transform.position.x >= CurrentField.bounds.xMin && p.transform.position.x <= CurrentField.bounds.xMax)
            {
                if (rewardType == MidBossRewardType.HealHP50)
                {
                    p.HealHPPercent(25f); // HP回復量は25%に変更
                }
                else if (rewardType == MidBossRewardType.HealEnergy50)
                {
                    p.AddEnergyPercent(25f); // ゲージ回復量は25%に変更
                }
                break;
            }
        }
    }

    private void AddEnergyToPlayer()
    {
        if (CurrentField == null) return;

        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (p.transform.position.x >= CurrentField.bounds.xMin && p.transform.position.x <= CurrentField.bounds.xMax)
            {
                float reward = isMidBoss ? 8.0f : energyReward;
                p.AddEnergy(reward);
                break;
            }
        }
    }
}