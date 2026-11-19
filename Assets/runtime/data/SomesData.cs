using System.Collections;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

// 攻撃を発射するためのインターフェース（消さないこと）
public interface IAttackEmitter
{
    void Initialize(PlayField targetField, float powerLevel);
    string AttackName { get; }
}

// IPlayerOption
public abstract class PlayerOptionBase : MonoBehaviour
{
    public abstract void UpdatePosition(bool isSlow);
    public abstract void FireOptionShot(GameObject bulletPrefab, float bulletSpeed, PlayField field, float damageMultiplier);
}

// 1. サポートスキル (自機バフ用)
public interface IAssistantSupport
{
    void ApplySupport(PlayerShip ship);
}

// 2. ジャマースキル (相手妨害用)
public interface IAssistantJammer
{
    void TriggerJammer(PlayField targetField);
}

// 3. エンゲージスキル (相乗りボス用)
public abstract class AssistantBossBase : MonoBehaviour
{
    protected PlayField targetField;
    protected EngageBossBase linkedMainBoss;

    [Header("徘徊・移動設定")]
    [SerializeField] private float moveInterval = 2.5f;
    [SerializeField] private float moveDuration = 1.2f;

    [Header("演出用")]
    [SerializeField] private SpriteRenderer bossSprite;

    private Coroutine wanderCoroutine;
    private bool isDismissing = false;

    public virtual void Initialize(PlayField field, EngageBossBase mainBoss)
    {
        targetField = field;
        linkedMainBoss = mainBoss;

        // 相手フィールドの上部からフェードイン出現
        Vector3 startPos = new Vector3(targetField.bounds.center.x, targetField.bounds.yMax + 2.0f, 0f);
        Vector3 enterPos = new Vector3(
            Random.Range(targetField.bounds.xMin + 1.0f, targetField.bounds.xMax - 1.0f),
            targetField.bounds.yMax - 1.8f,
            0f
        );

        transform.position = startPos;

        if (bossSprite != null)
        {
            Color c = bossSprite.color;
            c.a = 0f;
            bossSprite.color = c;
            LMotion.Create(0f, 1f, 1.0f).Bind(a => {
                if (bossSprite != null) { Color col = bossSprite.color; col.a = a; bossSprite.color = col; }
            }).AddTo(gameObject);
        }

        LMotion.Create(startPos, enterPos, 1.2f)
            .WithEase(Ease.OutCubic)
            .BindToPosition(transform)
            .AddTo(gameObject);

        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    protected virtual void Update()
    {
        // 主役ボスが撤退・撃破されたら即座に連動撤退
        if (!isDismissing && (linkedMainBoss == null || linkedMainBoss.IsDead))
        {
            Dismiss();
        }
    }

    // 相手フィールド内の徘徊ループ
    private IEnumerator WanderRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            if (targetField == null) yield break;

            // 相手フィールド上部範囲でランダム移動
            float targetX = Random.Range(targetField.bounds.xMin + 0.8f, targetField.bounds.xMax - 0.8f);
            float targetY = Random.Range(targetField.bounds.yMax - 2.8f, targetField.bounds.yMax - 1.2f);
            Vector3 dest = new Vector3(targetX, targetY, 0f);

            LMotion.Create(transform.position, dest, moveDuration)
                .WithEase(Ease.InOutSine)
                .BindToPosition(transform)
                .AddTo(gameObject);

            yield return new WaitForSeconds(moveInterval);
        }
    }

    protected virtual void Dismiss()
    {
        if (isDismissing) return;
        isDismissing = true;

        if (wanderCoroutine != null) StopCoroutine(wanderCoroutine);

        // 上空へフェードアウトしながら退却
        Vector3 exitPos = transform.position + Vector3.up * 3.0f;
        LMotion.Create(transform.position, exitPos, 0.8f)
            .WithEase(Ease.InQuad)
            .BindToPosition(transform)
            .AddTo(gameObject);

        if (bossSprite != null)
        {
            LMotion.Create(bossSprite.color.a, 0f, 0.8f).Bind(a => {
                if (bossSprite != null) { Color col = bossSprite.color; col.a = a; bossSprite.color = col; }
            }).AddTo(gameObject);
        }

        Destroy(gameObject, 0.85f);
    }
}