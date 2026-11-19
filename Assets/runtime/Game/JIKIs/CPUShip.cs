using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerShip))]
public class CPUShip : MonoBehaviour
{
    [SerializeField] private CPUDifficulty difficulty = CPUDifficulty.Normal;

    // 内部調整用パラメータ
    private float dangerRadius = 2.5f;
    private float decisionInterval = 0.08f;

    // 人間らしい入力再現（キーボード/D-Pad風の軸切り替え・入力ノイズ）
    private float inputSnappiness = 20.0f;       // 入力のキレ（高いほどパッパッと切り替わる）
    private bool allowDiagonalMove = true;       // 斜め移動を許可するか
    private float decisionErrorRate = 0f;        // 回避判断の失策率 (Easy用)

    private PlayerShip targetShip;
    private PlayField myField;
    private float nextDecisionTime = 0f;

    // 移動制御
    private Vector2 desiredMoveInput = Vector2.zero;
    private Vector2 currentSmoothInput = Vector2.zero;
    private bool isSlow = false;
    private bool isChargingNow = false;

    // 攻撃戦術用 (動的目標レベル: 1, 2, 3)
    private int nextTargetAttackLevel = 2;

    // 上下ゆらぎ・位置取り用
    private float verticalWanderTimer = 0f;
    private float targetYRatio = 0.2f;

    // GC Alloc 削減用コライダーキャッシュ & フィルター
    private readonly Collider2D[] hitBulletBuffer = new Collider2D[64]; // バッファ拡張(64)
    private ContactFilter2D bulletContactFilter;

    private void Awake()
    {
        targetShip = GetComponent<PlayerShip>();

        bulletContactFilter = new ContactFilter2D();
        bulletContactFilter.useTriggers = true;
        bulletContactFilter.SetLayerMask(Physics2D.AllLayers);
    }

    private void Start()
    {
        var pInput = GetComponent<PlayerInput>();
        if (pInput != null) pInput.enabled = false;

        PlayField[] fields = FindObjectsByType<PlayField>(FindObjectsInactive.Exclude);
        foreach (var f in fields)
        {
            if (transform.position.x >= f.bounds.xMin && transform.position.x <= f.bounds.xMax)
            {
                myField = f;
                break;
            }
        }

        ApplyDifficultySettings();
        DecideNextAttackLevelDynamic();
        StartCoroutine(CombatActionLoop());
    }

    public void SetDifficulty(CPUDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        ApplyDifficultySettings();
    }

    private void ApplyDifficultySettings()
    {
        switch (difficulty)
        {
            case CPUDifficulty.Easy:
                dangerRadius = 1.6f;
                decisionInterval = 0.20f;        // 思考が遅い (0.2s間隔)
                inputSnappiness = 8.0f;          // もっさりした移動
                allowDiagonalMove = false;       // 斜め移動をほぼしない
                decisionErrorRate = 0.35f;       // 35%の確率で回避方向を誤る・見落とす
                break;

            case CPUDifficulty.Normal:
                dangerRadius = 2.2f;
                decisionInterval = 0.08f;        // 標準思考 (0.08s)
                inputSnappiness = 16.0f;
                allowDiagonalMove = true;
                decisionErrorRate = 0.10f;       // 10%の確率で判断ミス
                break;

            case CPUDifficulty.Hard:
                dangerRadius = 3.0f;             // 視界拡大
                decisionInterval = 0.033f;       // 高速思考 (約30fps毎)
                inputSnappiness = 30.0f;         // キレのあるキリッとした移動
                allowDiagonalMove = true;
                decisionErrorRate = 0.0f;
                break;

            case CPUDifficulty.Lunatic:
                dangerRadius = 3.8f;             // 超広範警戒
                decisionInterval = 0.016f;       // 毎フレーム思考 (60fps)
                inputSnappiness = 50.0f;         // 瞬時に軸を合わせる神回避
                allowDiagonalMove = true;
                decisionErrorRate = 0.0f;
                break;
        }
    }

    private void Update()
    {
        if (PauseManager.IsPaused || targetShip == null) return;

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;
            ThinkTactics();
        }

        // 人間らしいメリハリのある移動補間（斜め滑らか移動を抑制し、キーボード風ステップ感）
        currentSmoothInput = Vector2.MoveTowards(currentSmoothInput, desiredMoveInput, Time.deltaTime * inputSnappiness);

        targetShip.SetMoveInput(currentSmoothInput);
        targetShip.SetSlowInput(isSlow);
    }

    // --- 思考ルーチン ---
    private void ThinkTactics()
    {
        Vector2 selfPos = transform.position;
        int hitCount = Physics2D.OverlapCircle(selfPos, dangerRadius, bulletContactFilter, hitBulletBuffer);

        Vector2 bulletRepulsion = Vector2.zero;
        int threatCount = 0;
        int directCollisionCount = 0; // 自機へ直撃コースの危険弾数
        float minCollisionTime = float.MaxValue; // 直撃までの最短時間

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBulletBuffer[i];
            if (col == null || !col.CompareTag("EnemyBullet")) continue;

            Vector2 bPos = col.transform.position;
            Vector2 relPos = bPos - selfPos;
            float dist = relPos.magnitude;

            // 弾の移動速度ベクトルを取得（Rigidbody2Dまたは進行方向の推測）
            Vector2 bulletVel = Vector2.down * 5f; // デフォルト推定値
            Rigidbody2D rb = col.attachedRigidbody;
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                bulletVel = rb.linearVelocity;
            }
            else
            {
                // Transformの向き(-up等)から推定
                bulletVel = -col.transform.up * 6f;
            }

            // 【360度対応】自機に向かって接近してくる弾かを判定（内積がマイナスなら接近中）
            float approachSpeed = Vector2.Dot(relPos.normalized, bulletVel);

            // すでに通過して遠ざかっている弾は回避対象から除外
            if (approachSpeed >= 0.1f && dist > 0.5f) continue;

            // --- 直撃（衝突可能性）の予測計算 ---
            // 弾の直線軌道と自機位置の最短到達距離を算出
            Vector2 velDir = bulletVel.normalized;
            float projectedDist = Vector2.Dot(-relPos, velDir);

            if (projectedDist > 0f) // 自機に向かう方向
            {
                Vector2 closestPoint = bPos + (velDir * projectedDist);
                float missDistance = Vector2.Distance(selfPos, closestPoint);

                // 自機当たり判定（約0.25f）に掠るコースか？
                if (missDistance < 0.45f)
                {
                    float timeToImpact = projectedDist / bulletVel.magnitude;
                    if (timeToImpact < minCollisionTime) minCollisionTime = timeToImpact;
                    directCollisionCount++;
                }
            }

            // --- 避けるための反発ベクトル計算 (横・後ろ・全方位対応) ---
            float weight = Mathf.Clamp01((dangerRadius - dist) / dangerRadius);
            
            // 弾の進行方向と直角（横逃げ）のベクトルを優先
            Vector2 avoidDir = Vector2.Perpendicular(velDir);
            if (Vector2.Dot(avoidDir, -relPos) < 0f) avoidDir = -avoidDir;

            bulletRepulsion += avoidDir * weight * 3.0f;
            threatCount++;
        }

        // 移動可能領域（Y軸範囲：下端〜フィールド高さの約45%）
        float minY = (myField != null) ? myField.bounds.yMin + 0.5f : -4.2f;
        float maxY = (myField != null) ? myField.bounds.yMin + (myField.bounds.height * 0.45f) : -1.0f;

        // --- 1. 回避優先ロジック ---
        if (threatCount > 0)
        {
            // Easy時のポカミス（失策率）
            if (difficulty == CPUDifficulty.Easy && Random.value < decisionErrorRate)
            {
                bulletRepulsion = -bulletRepulsion * 0.5f; // 逆方向に避けてしまう
            }

            // 精密回避のための低速移動切り替え
            isSlow = (minCollisionTime < 0.6f) || isChargingNow;

            Vector2 moveDir = bulletRepulsion.normalized;

            // 【人間らしい軸移動】主要な軸（X軸優先）にデジタル化（斜めすべり防止）
            if (!allowDiagonalMove || difficulty == CPUDifficulty.Easy)
            {
                if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y)) moveDir.y = 0f;
                else moveDir.x = 0f;
            }

            // エリア境界ガード
            if (selfPos.y < minY && moveDir.y < 0) moveDir.y = 0.5f;
            if (selfPos.y > maxY && moveDir.y > 0) moveDir.y = -0.5f;

            desiredMoveInput = new Vector2(
                Mathf.Clamp(moveDir.x, -1f, 1f),
                Mathf.Clamp(moveDir.y, -1f, 1f)
            );

            // --- ボム使用判断（「当たらない弾での誤作動」を完全根絶） ---
            bool isLastStand = targetShip.IsLastStand;
            bool shouldBomb = false;

            // 「本当に直撃する弾があり、衝突まで1秒以内」の場合のみボムの検討に入る
            if (directCollisionCount > 0 && minCollisionTime < 0.45f)
            {
                if (isLastStand)
                {
                    // 食いしばり中は後がないため即ボム
                    shouldBomb = true;
                }
                else
                {
                    // 難易度ごとにボムを切る窮地の基準を変える
                    switch (difficulty)
                    {
                        case CPUDifficulty.Easy:
                            shouldBomb = (directCollisionCount >= 2);
                            break;
                        case CPUDifficulty.Normal:
                            shouldBomb = (directCollisionCount >= 3 && minCollisionTime < 0.25f);
                            break;
                        case CPUDifficulty.Hard:
                            shouldBomb = (directCollisionCount >= 4 && minCollisionTime < 0.18f); // 粘る
                            break;
                        case CPUDifficulty.Lunatic:
                            // Lunaticは極限まで自力回避し、回避不能時間(0.1s未満)かつ密集時のみ
                            shouldBomb = (directCollisionCount >= 5 && minCollisionTime < 0.10f);
                            break;
                    }
                }
            }

            if (shouldBomb)
            {
                targetShip.ExecuteBombDirect();
            }
        }
        // --- 2. 迎撃・ポジショニング ---
        else
        {
            Transform targetObj = FindHighestPriorityTarget();

            verticalWanderTimer -= decisionInterval;
            if (verticalWanderTimer <= 0f)
            {
                verticalWanderTimer = Random.Range(1.5f, 3.5f);
                targetYRatio = Random.Range(0.15f, 0.40f);
            }

            float idealY = (myField != null) 
                ? myField.bounds.yMin + (myField.bounds.height * targetYRatio) 
                : -3.0f;

            if (targetObj != null)
            {
                float targetX = targetObj.position.x;
                float diffX = targetX - selfPos.x;
                float diffY = idealY - selfPos.y;

                bool onTargetLine = Mathf.Abs(diffX) < 0.4f;
                isSlow = onTargetLine || isChargingNow;

                // 軸合わせ移動（X軸を合わせてから前後に微調整する人間的な動き）
                float moveX = 0f;
                if (Mathf.Abs(diffX) > 0.15f)
                {
                    moveX = Mathf.Sign(diffX) * (difficulty == CPUDifficulty.Lunatic ? 1.0f : 0.8f);
                }

                float moveY = 0f;
                if (Mathf.Abs(diffY) > 0.3f && onTargetLine)
                {
                    moveY = Mathf.Sign(diffY) * 0.6f;
                }

                desiredMoveInput = new Vector2(moveX, moveY);
            }
            else
            {
                float diffY = idealY - selfPos.y;
                float moveY = Mathf.Abs(diffY) > 0.3f ? Mathf.Sign(diffY) * 0.5f : 0f;

                isSlow = isChargingNow;
                desiredMoveInput = new Vector2(0f, moveY);
            }
        }

        // 壁際脱出
        if (myField != null)
        {
            float wallMargin = 0.4f;
            if (selfPos.x < myField.bounds.xMin + wallMargin && desiredMoveInput.x < 0) desiredMoveInput.x = 0.8f;
            if (selfPos.x > myField.bounds.xMax - wallMargin && desiredMoveInput.x > 0) desiredMoveInput.x = -0.8f;
        }
    }

    // --- ボス・敵最優先の索敵 ---
    private Transform FindHighestPriorityTarget()
    {
        if (myField == null) return null;

        EngageBossBase[] bosses = FindObjectsByType<EngageBossBase>(FindObjectsInactive.Exclude);
        foreach (var boss in bosses)
        {
            if (boss != null && !boss.IsDead)
            {
                if (boss.transform.position.x >= myField.bounds.xMin && boss.transform.position.x <= myField.bounds.xMax)
                {
                    return boss.transform;
                }
            }
        }

        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        EnemyBase midBoss = null;
        EnemyBase closestRegular = null;
        float lowestY = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            float ex = enemy.transform.position.x;
            if (ex < myField.bounds.xMin || ex > myField.bounds.xMax) continue;

            if (enemy.name.Contains("MidBoss") || enemy.name.Contains("Boss"))
            {
                midBoss = enemy;
                break;
            }

            if (enemy.transform.position.y < lowestY)
            {
                lowestY = enemy.transform.position.y;
                closestRegular = enemy;
            }
        }

        if (midBoss != null) return midBoss.transform;
        if (closestRegular != null) return closestRegular.transform;

        return null;
    }

    // --- 動的な攻撃目標レベル決定 ---
    private void DecideNextAttackLevelDynamic()
    {
        if (targetShip == null) return;

        bool isLastStand = targetShip.IsLastStand;
        bool hasEnemyBossOnField = false;

        if (myField != null)
        {
            EngageBossBase[] bosses = FindObjectsByType<EngageBossBase>(FindObjectsInactive.Exclude);
            foreach (var b in bosses)
            {
                if (b != null && !b.IsDead && b.transform.position.x >= myField.bounds.xMin && b.transform.position.x <= myField.bounds.xMax)
                {
                    hasEnemyBossOnField = true;
                    break;
                }
            }
        }

        if (isLastStand)
        {
            nextTargetAttackLevel = 3;
            return;
        }

        if (hasEnemyBossOnField)
        {
            nextTargetAttackLevel = (difficulty >= CPUDifficulty.Hard) ? 3 : 2;
            return;
        }

        switch (difficulty)
        {
            case CPUDifficulty.Easy:
            case CPUDifficulty.Normal:
                float rollN = Random.value;
                if (rollN < 0.3f) nextTargetAttackLevel = 1;
                else if (rollN < 0.7f) nextTargetAttackLevel = 2;
                else nextTargetAttackLevel = 3;
                break;

            case CPUDifficulty.Hard:
            case CPUDifficulty.Lunatic:
                float rollH = Random.value;
                if (rollH < 0.1f) nextTargetAttackLevel = 1;
                else if (rollH < 0.4f) nextTargetAttackLevel = 2;
                else nextTargetAttackLevel = 3; // 高レベル技を積極使用
                break;
        }
    }

    private IEnumerator CombatActionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            if (PauseManager.IsPaused || targetShip == null) continue;

            targetShip.SetShotInput(true);

            bool readyToAttack = false;
            float holdDuration = 0f;

            switch (nextTargetAttackLevel)
            {
                case 1:
                    if (targetShip.CurrentEnergy >= 40f)
                    {
                        readyToAttack = true;
                        holdDuration = 1.7f;
                    }
                    break;

                case 2:
                    if (targetShip.CurrentEnergy >= 70f)
                    {
                        readyToAttack = true;
                        holdDuration = 2.9f;
                    }
                    break;

                case 3:
                    if (targetShip.CurrentEnergy >= 100f)
                    {
                        readyToAttack = true;
                        holdDuration = 4.1f;
                    }
                    break;
            }

            if (readyToAttack)
            {
                isChargingNow = true;
                yield return StartCoroutine(ExecuteChargeSequence(holdDuration));
                isChargingNow = false;

                DecideNextAttackLevelDynamic();
            }
        }
    }

    private IEnumerator ExecuteChargeSequence(float chargeHoldTime)
    {
        targetShip.SetChargeInput(true);
        yield return new WaitForSeconds(chargeHoldTime);
        if (targetShip != null)
        {
            targetShip.SetChargeInput(false);
        }
    }
}