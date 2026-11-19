using System.Collections.Generic;
using UnityEngine;

public class ChainExplosion : MonoBehaviour
{
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float maxRadius = 1.5f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float timer = 0f;
    private HashSet<Collider2D> hitColliders = new HashSet<Collider2D>();
    public PlayField Field { get; set; }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / duration);
        transform.localScale = Vector3.one * Mathf.Lerp(0.2f, maxRadius * 2f, progress);

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(0.8f, 0f, progress);
            spriteRenderer.color = c;
        }

        if (timer >= duration) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 敵弾の消滅 (自フィールド枠内のみ・プール返却優先)
        if (other.CompareTag("EnemyBullet"))
        {
            if (Field == null || Field.bounds.Contains(other.transform.position))
            {
                EnemyBullet eb = other.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.ReleaseToPool();
                }
                else
                {
                    Destroy(other.gameObject);
                }
            }
        }

        // 近くの敵への誘爆ダメージ
        if (other.CompareTag("Enemy") && !hitColliders.Contains(other))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && (Field == null || enemy.CurrentField == Field))
            {
                hitColliders.Add(other);
                enemy.TakeDamage(damage, isChainExplosion: true);
            }
        }
    }
}