using UnityEngine;

public class DeleteBullet : MonoBehaviour
{
    [SerializeField] private float damage = 15.0f;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("貫通・判定動作フラグ")]
    [Tooltip("ON: 自機弾を打ち消さずそのまま貫通させる / OFF: 自機弾を打ち消す")]
    [SerializeField] private bool passThroughPlayerBullets = false;
    [Tooltip("ON: 自機に接触しても消滅せず貫通する / OFF: 着弾時に消滅する")]
    [SerializeField] private bool passThroughPlayer = false;

    [Header("スプライトバリエーション")]
    [SerializeField] private Sprite[] spriteVariations;
    [SerializeField] private bool randomizeSprite = false;
    [SerializeField] private int specificSpriteIndex = 0;

    private Vector3 direction = Vector3.down;
    private PlayField targetField;
    private bool hasHitPlayer = false;
    private float lastDamageTime = -999f;
    private const float HitCooldown = 0.5f; // 自機貫通時の連続ヒット防止クールタイム

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        ApplySprite();
    }

    public void Initialize(PlayField field, Vector3 dir, float moveSpeed, float dmg, bool isStarMode = false)
    {
        targetField = field;
        direction = dir.normalized;
        speed = moveSpeed;
        damage = dmg;

        // 落下星モードの場合は貫通フラグを有効化
        if (isStarMode)
        {
            passThroughPlayerBullets = true;
            passThroughPlayer = true;
        }

        // 進行方向に回転を合わせる
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        transform.position += direction * (speed * Time.deltaTime);

        // フィールド外に出たらデスポーン
        if (targetField != null)
        {
            Rect bounds = targetField.bounds;
            float margin = 1.0f; // 星のサイズに合わせて大きめのマージン
            if (transform.position.y < bounds.yMin - margin ||
                transform.position.y > bounds.yMax + margin ||
                transform.position.x < bounds.xMin - margin ||
                transform.position.x > bounds.xMax + margin)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 自機弾 (PlayerBullet) 判定
        PlayerBullet playerBullet = other.GetComponent<PlayerBullet>();
        if (playerBullet != null)
        {
            if (!passThroughPlayerBullets)
            {
                playerBullet.ReleaseToPool(); // 通常消失弾: 自機弾を一方的に消去
            }
            // passThroughPlayerBullets == true の場合は何もしない（自機弾がそのまま貫通する）
            return;
        }

        // 2. 自機 (PlayerShip) 判定
        PlayerShip player = other.GetComponent<PlayerShip>() ?? other.GetComponentInParent<PlayerShip>();
        if (player != null)
        {
            if (passThroughPlayer)
            {
                // 自機貫通モード: 一定クールタイムごとにダメージを与える（消滅しない）
                if (Time.time >= lastDamageTime + HitCooldown)
                {
                    lastDamageTime = Time.time;
                    player.TakeDamage(damage);
                }
            }
            else if (!hasHitPlayer)
            {
                // 通常モード: ダメージを与えて消滅
                hasHitPlayer = true;
                player.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
    private void ApplySprite()
    {
        if (spriteRenderer == null || spriteVariations == null || spriteVariations.Length == 0) return;
        if (randomizeSprite)
        {
            int rnd = Random.Range(0, spriteVariations.Length);
            spriteRenderer.sprite = spriteVariations[rnd];
        }
        else
        {
            int index = Mathf.Clamp(specificSpriteIndex, 0, spriteVariations.Length - 1);
            spriteRenderer.sprite = spriteVariations[index];
        }
    }
}