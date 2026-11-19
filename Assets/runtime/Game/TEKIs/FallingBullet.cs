using UnityEngine;

public class FallingBullet : MonoBehaviour
{
    private PlayField targetField;
    private Vector3 moveDirection = Vector3.down;
    [SerializeField] private float currentSpeed = 0f;
    [SerializeField] private float acceleration = 4.0f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float delayTimer = 0.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] spriteVariations;
    [SerializeField] private bool randomizeSprite = true;
    [SerializeField] private int specificSpriteIndex = 0;

    private bool isMoving = false;
    private bool hasHit = false;
    private Collider2D myCollider;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        ApplySprite();
    }

    public void Initialize(PlayField field, float delay, Vector3 direction, float initialSpeed, float accel, float dmg)
    {
        targetField = field;
        delayTimer = delay;
        moveDirection = direction.normalized;
        currentSpeed = initialSpeed;
        acceleration = accel;
        damage = dmg;

        hasHit = false;
        isMoving = false;

        // 進行方向に向けて回転
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        // 0.5秒の出現・静止待機時間
        if (!isMoving)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f)
            {
                isMoving = true;
            }
            return;
        }

        // 落下移動 (加速)
        currentSpeed += acceleration * Time.deltaTime;
        transform.position += moveDirection * (currentSpeed * Time.deltaTime);

        // 画面外で消滅
        if (targetField != null)
        {
            Rect bounds = targetField.bounds;
            float margin = 0.5f;
            if (transform.position.y < bounds.yMin - margin || transform.position.y > bounds.yMax + margin ||
                transform.position.x < bounds.xMin - margin || transform.position.x > bounds.xMax + margin)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || !isMoving) return;

        PlayerShip ship = other.GetComponent<PlayerShip>() ?? other.GetComponentInParent<PlayerShip>();
        if (ship != null)
        {
            hasHit = true;
            ship.TakeDamage(damage);
            Destroy(gameObject);
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