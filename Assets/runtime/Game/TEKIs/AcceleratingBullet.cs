using UnityEngine;
using UnityEngine.Pool;

public class AcceleratingBullet : MonoBehaviour, IPooledBullet
{
    [SerializeField] private float initialSpeed = 2.0f;
    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float damage = 35f;

    [Header("特殊効果")]
    [Tooltip("命中時に減少させるエネルギーの固定値 (0なら不発)")]
    [SerializeField] private float energyReduceAmount = 0f;

    private PlayField targetField;
    private float currentSpeed;
    private IObjectPool<GameObject> bulletPool;
    private bool isReleased = false;

    public void SetPool(IObjectPool<GameObject> pool)
    {
        bulletPool = pool;
    }

    public void Initialize(PlayField field)
    {
        targetField = field;
        currentSpeed = initialSpeed;
        isReleased = false;
    }

    private void Update()
    {
        currentSpeed += acceleration * Time.deltaTime;
        transform.position += (-transform.up) * (currentSpeed * Time.deltaTime);

        if (targetField != null)
        {
            Rect bounds = targetField.bounds;
            float margin = 0.5f;
            if (transform.position.y < bounds.yMin - margin || transform.position.y > bounds.yMax + margin ||
                transform.position.x < bounds.xMin - margin || transform.position.x > bounds.xMax + margin)
            {
                ReleaseToPool();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReleased) return;
        PlayerShip ship = other.GetComponent<PlayerShip>() ?? other.GetComponentInParent<PlayerShip>();
        if (ship != null)
        {
            // ダメージ付与
            ship.TakeDamage(damage);

            // エネルギー固定値減少効果があれば発動
            if (energyReduceAmount > 0f)
            {
                ship.ReduceEnergy(energyReduceAmount);
            }

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