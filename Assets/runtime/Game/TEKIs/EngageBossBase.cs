using System.Collections;
using UnityEngine;

public abstract class EngageBossBase : MonoBehaviour, IAttackEmitter
{
    [Header("技名設定 (カットイン等で使用)")]
    [SerializeField] private string attackName = "エンゲージアタック";
    public string AttackName => attackName;

    [Header("基本ステータス (共通)")]
    [SerializeField] protected float maxHP = 1000f;
    [SerializeField] protected float maxDuration = 30.0f;
    [SerializeField] protected float moveSmoothTime = 0.8f;
    [SerializeField] protected float stayTime = 1.2f;

    [Header("撃破報酬 & 演出")]
    [SerializeField] protected GameObject defeatExplosionPrefab;
    [SerializeField] protected float defeatEnergyReward = 12.0f;

    protected float currentHP;
    protected float remainingTime;
    protected float currentPower = 1.0f;
    protected PlayField targetField;
    protected bool isDead = false;
    protected bool isRetreating = false;

    public bool IsDead => isDead || isRetreating;

    private Vector3 currentVelocity;
    private Vector3 targetMovePosition;
    private Coroutine movementRoutine;

    public virtual void Initialize(PlayField field, float power)
    {
        targetField = field;
        currentPower = Mathf.Clamp(power, 1.0f, 9.0f);
        currentHP = maxHP;
        remainingTime = maxDuration;

        // 出現時にフィールド側のタイマーUIを起動
        if (targetField != null)
        {
            targetField.SetBossTimerVisible(true);
            targetField.UpdateBossTimer(remainingTime);
        }

        float startX = targetField.bounds.center.x;
        float startY = targetField.bounds.yMax + 1.2f;
        transform.position = new Vector3(startX, startY, 0f);

        StartCoroutine(BossLifecycleRoutine());
    }

    private IEnumerator BossLifecycleRoutine()
    {
        Vector3 entryPos = new Vector3(targetField.bounds.center.x, targetField.bounds.yMax - 1.2f, 0f);
        float t = 0f;
        Vector3 spawnPos = transform.position;
        while (t < 1.0f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(spawnPos, entryPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        movementRoutine = StartCoroutine(RoamMovementRoutine());
        StartBarrage();

        // 30秒カウントダウン & フィールドUI更新
        while (remainingTime > 0f && !isDead)
        {
            remainingTime -= Time.deltaTime;
            if (targetField != null)
            {
                targetField.UpdateBossTimer(remainingTime);
            }
            yield return null;
        }

        if (!isDead)
        {
            Retreat();
        }
    }

    private IEnumerator RoamMovementRoutine()
    {
        float upperMinY = targetField.bounds.center.y + 0.8f;
        float upperMaxY = targetField.bounds.yMax - 0.8f;
        float minX = targetField.bounds.xMin + 0.8f;
        float maxX = targetField.bounds.xMax - 0.8f;

        while (!isDead && !isRetreating)
        {
            targetMovePosition = new Vector3(Random.Range(minX, maxX), Random.Range(upperMinY, upperMaxY), 0f);

            float moveTimer = 0f;
            while (moveTimer < moveSmoothTime)
            {
                moveTimer += Time.deltaTime;
                transform.position = Vector3.SmoothDamp(transform.position, targetMovePosition, ref currentVelocity, moveSmoothTime);
                yield return null;
            }

            yield return new WaitForSeconds(stayTime);
        }
    }

    private void Retreat()
    {
        isRetreating = true;
        HideFieldTimer();
        StopBarrage();
        if (movementRoutine != null) StopCoroutine(movementRoutine);
        StartCoroutine(RetreatRoutine());
    }

    private IEnumerator RetreatRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 exitPos = new Vector3(transform.position.x, targetField.bounds.yMax + 1.5f, 0f);
        float t = 0f;

        while (t < 1.0f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, exitPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        Destroy(gameObject);
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead || isRetreating) return;

        currentHP -= damage;
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        HideFieldTimer();
        StopBarrage();
        if (movementRoutine != null) StopCoroutine(movementRoutine);

        if (defeatExplosionPrefab != null)
        {
            GameObject exp = Instantiate(defeatExplosionPrefab, transform.position, Quaternion.identity);
            CommonBomb bomb = exp.GetComponent<CommonBomb>();
            if (bomb != null) bomb.OwnerField = targetField;
        }

        RewardDefeatEnergy();
        Destroy(gameObject);
    }

    private void HideFieldTimer()
    {
        if (targetField != null)
        {
            targetField.SetBossTimerVisible(false);
        }
    }

    private void RewardDefeatEnergy()
    {
        if (targetField == null) return;

        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (p.transform.position.x >= targetField.bounds.xMin && p.transform.position.x <= targetField.bounds.xMax)
            {
                p.AddEnergy(defeatEnergyReward);
                break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerBullet pb = other.GetComponent<PlayerBullet>();
        if (pb != null)
        {
            TakeDamage(pb.Damage);
            pb.ReleaseToPool();
        }
    }

    protected abstract void StartBarrage();
    protected abstract void StopBarrage();
    public void BuffMaxHP(float multiplier)
    {
        maxHP *= multiplier;
        currentHP *= multiplier;
    }
    public void AddRemainingTime(float seconds)
    {
        maxDuration += seconds;
        remainingTime += seconds;
        if (targetField != null)
        {
            targetField.UpdateBossTimer(remainingTime);
        }
    }
    public void MultiplyStayTime(float multiplier)
    {
        stayTime *= multiplier;
    }
}