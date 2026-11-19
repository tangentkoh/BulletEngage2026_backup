using UnityEngine;
using UnityEngine.Pool;

public class CurvingEnemyBullet : MonoBehaviour, IPooledBullet
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private float speed = 5.0f;
    private Vector3 direction = Vector3.down;
    private float angularVelocity = 0f;
    private PlayField targetField;
    private bool hasHit = false;
    private bool isReleased = false;

    private IObjectPool<GameObject> bulletPool;
    private Collider2D col;

    [Header("スプライトバリエーション")]
    [SerializeField] private Sprite[] spriteVariations;
    [SerializeField] private bool randomizeSprite = true;
    [SerializeField] private int specificSpriteIndex = 0;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        ApplySprite();
    }

    public void SetPool(IObjectPool<GameObject> pool)
    {
        bulletPool = pool;
    }

    public void InitializeCurving(PlayField field, Vector3 initialDir, float moveSpeed, float dmg, float curveSpeed)
    {
        targetField = field;
        direction = initialDir.normalized;
        speed = moveSpeed;
        damage = dmg;
        angularVelocity = curveSpeed;

        hasHit = false;
        isReleased = false;
        if (col != null) col.enabled = true; // コライダーを再有効化

        ApplySprite();
        UpdateRotation();
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

    private void Update()
    {
        if (Mathf.Abs(angularVelocity) > 0.01f)
        {
            float angleDelta = angularVelocity * Time.deltaTime;
            direction = Quaternion.Euler(0, 0, angleDelta) * direction;
            UpdateRotation();
        }

        transform.position += direction * (speed * Time.deltaTime);

        // フィールド枠外デスポーン -> プール返却
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

    private void UpdateRotation()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || isReleased) return;

        PlayerShip player = other.GetComponent<PlayerShip>() ?? other.GetComponentInParent<PlayerShip>();
        if (player != null)
        {
            hasHit = true;
            if (col != null) col.enabled = false;
            player.TakeDamage(damage);
            ReleaseToPool();
        }
    }

    public void ReleaseToPool()
    {
        if (isReleased) return;
        isReleased = true;

        if (bulletPool != null)
        {
            bulletPool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}