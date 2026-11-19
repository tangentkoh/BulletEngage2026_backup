using UnityEngine;
using UnityEngine.Pool;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("スプライトバリエーション")]
    [SerializeField] private Sprite[] spriteVariations;
    [SerializeField] private bool randomizeSprite = true;
    [SerializeField] private int specificSpriteIndex = 0;

    private float speed = 5f;
    private Vector3 direction = Vector3.down;
    private PlayField targetField;
    private bool hasHit = false;

    private IObjectPool<GameObject> pool;
    private Collider2D myCollider;

    public void SetPool(IObjectPool<GameObject> targetPool) => pool = targetPool;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        ApplySprite();
    }

    public void Initialize(PlayField field, Vector3 dir, float moveSpeed, float dmg)
    {
        targetField = field;
        direction = dir.normalized;
        speed = moveSpeed;
        damage = dmg;

        // プールから取り出された時の状態リセット
        hasHit = false;
        if (myCollider != null) myCollider.enabled = true;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        transform.position += direction * (speed * Time.deltaTime);

        if (targetField != null)
        {
            float margin = 0.5f;
            if (transform.position.y < targetField.bounds.yMin - margin ||
                transform.position.y > targetField.bounds.yMax + margin ||
                transform.position.x < targetField.bounds.xMin - margin ||
                transform.position.x > targetField.bounds.xMax + margin)
            {
                ReleaseToPool();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        PlayerShip player = other.GetComponent<PlayerShip>() ?? other.GetComponentInParent<PlayerShip>();
        if (player != null)
        {
            hasHit = true;
            if (myCollider != null) myCollider.enabled = false;
            player.TakeDamage(damage);
            ReleaseToPool();
        }
    }

    public void ReleaseToPool()
    {
        if (!gameObject.activeSelf) return;

        if (pool != null) pool.Release(gameObject);
        else Destroy(gameObject);
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