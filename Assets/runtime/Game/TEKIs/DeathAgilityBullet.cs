using UnityEngine;

public class DeathAgilityBullet : MonoBehaviour
{
    [SerializeField] private float speed = 26.0f; // 極めて早い速度
    [SerializeField] private float speedReduceAmount = 0.2f;
    [SerializeField] private float minNormalSpeed = 4.0f;

    private PlayField targetField;
    private Vector3 direction = Vector3.down;
    private bool hasHit = false;

    public void Initialize(PlayField field, Vector3 dir)
    {
        targetField = field;
        direction = dir.normalized;
        hasHit = false;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        transform.position += direction * (speed * Time.deltaTime);

        // フィールド外で自動消滅
        if (targetField != null)
        {
            float margin = 0.5f;
            if (transform.position.y < targetField.bounds.yMin - margin ||
                transform.position.y > targetField.bounds.yMax + margin ||
                transform.position.x < targetField.bounds.xMin - margin ||
                transform.position.x > targetField.bounds.xMax + margin)
            {
                Destroy(gameObject);
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
            // ダメージは発生させず、高速移動速度のみ低下させる
            player.ReduceNormalSpeed(speedReduceAmount, minNormalSpeed);
            Destroy(gameObject);
        }
    }
}