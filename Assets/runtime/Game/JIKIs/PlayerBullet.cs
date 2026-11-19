using UnityEngine;
using UnityEngine.Pool;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float damage = 10f;

    public float Damage => damage;

    private IObjectPool<PlayerBullet> pool;
    private float currentLifeTime;
    private PlayField ownerField;

    public void SetPool(IObjectPool<PlayerBullet> targetPool)
    {
        pool = targetPool;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetField(PlayField field)
    {
        ownerField = field;
    }

    private void OnEnable()
    {
        currentLifeTime = 0f;
    }

    private void Update()
    {
        transform.position += transform.up * (speed * Time.deltaTime);

        // 自フィールドの境界（左右・上下）を少しでも越えたら消滅
        if (ownerField != null)
        {
            float margin = 0.15f;
            if (transform.position.x < ownerField.bounds.xMin - margin ||
                transform.position.x > ownerField.bounds.xMax + margin ||
                transform.position.y > ownerField.bounds.yMax + margin ||
                transform.position.y < ownerField.bounds.yMin - margin)
            {
                ReleaseToPool();
                return;
            }
        }

        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= lifeTime)
        {
            ReleaseToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && (ownerField == null || enemy.CurrentField == ownerField))
            {
                enemy.TakeDamage(damage);
            }
            ReleaseToPool();
        }
    }

    public void ReleaseToPool()
    {
        if (!gameObject.activeSelf) return;

        if (pool != null)
        {
            pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}