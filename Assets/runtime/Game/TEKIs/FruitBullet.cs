using UnityEngine;

public class BouncingFruitBullet : MonoBehaviour
{
    [SerializeField] private float speed = 6.0f;
    [SerializeField] private float damage = 12.0f;
    [SerializeField] private int maxBounceCount = 3;

    private PlayField targetField;
    private Vector3 direction = Vector3.down;
    private int currentBounceCount = 0;
    private bool hasHit = false;

    public void Initialize(PlayField field, Vector3 dir, float moveSpeed = 6.0f, float dmg = 12.0f, int maxBounces = 3)
    {
        targetField = field;
        direction = dir.normalized;
        speed = moveSpeed;
        damage = dmg;
        maxBounceCount = maxBounces;
        currentBounceCount = 0;
        hasHit = false;

        UpdateRotation();
    }

    private void Update()
    {
        transform.position += direction * (speed * Time.deltaTime);

        if (targetField != null)
        {
            Rect bounds = targetField.bounds;
            Vector3 pos = transform.position;
            bool bounced = false;

            // 左右の端での反射
            if (pos.x < bounds.xMin)
            {
                pos.x = bounds.xMin;
                direction.x = Mathf.Abs(direction.x);
                bounced = true;
            }
            else if (pos.x > bounds.xMax)
            {
                pos.x = bounds.xMax;
                direction.x = -Mathf.Abs(direction.x);
                bounced = true;
            }

            // 上下の端での反射
            if (pos.y < bounds.yMin)
            {
                pos.y = bounds.yMin;
                direction.y = Mathf.Abs(direction.y);
                bounced = true;
            }
            else if (pos.y > bounds.yMax)
            {
                pos.y = bounds.yMax;
                direction.y = -Mathf.Abs(direction.y);
                bounced = true;
            }

            if (bounced)
            {
                transform.position = pos;
                currentBounceCount++;
                UpdateRotation();

                if (currentBounceCount >= maxBounceCount)
                {
                    Destroy(gameObject);
                    return;
                }
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
        if (hasHit) return;

        PlayerShip player = other.GetComponent<PlayerShip>() ?? other.GetComponentInParent<PlayerShip>();
        if (player != null)
        {
            hasHit = true;
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}