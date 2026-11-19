using System.Collections.Generic;
using UnityEngine;

public class CommonBomb : MonoBehaviour
{
    [Header("ボム設定")]
    [SerializeField] private float duration = 2.0f;
    [SerializeField] private float startRadius = 1.0f;
    [SerializeField] private float endRadius = 12.0f;
    [SerializeField] private float damage = 150f;

    [Header("ビジュアル")]
    [SerializeField] private SpriteRenderer circleRenderer;

    public PlayField OwnerField { get; set; }

    private float timer = 0f;
    private HashSet<Collider2D> damagedTargets = new HashSet<Collider2D>();

    private void OnEnable()
    {
        timer = 0f;
        damagedTargets.Clear();
        transform.localScale = Vector3.one * startRadius;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / duration);

        float currentRadius = Mathf.Lerp(startRadius, endRadius, progress);
        transform.localScale = Vector3.one * currentRadius;

        if (circleRenderer != null)
        {
            Color c = circleRenderer.color;
            c.a = Mathf.Lerp(0.5f, 0f, progress);
            circleRenderer.color = c;
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 敵弾の消去判定 (プール返却優先)
        if (other.CompareTag("EnemyBullet"))
        {
            if (OwnerField == null || OwnerField.bounds.Contains(other.transform.position))
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

        // 2. 敵本体へのダメージ判定
        if (other.CompareTag("Enemy") && !damagedTargets.Contains(other))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && (OwnerField == null || enemy.CurrentField == OwnerField))
            {
                damagedTargets.Add(other);
                enemy.TakeDamage(damage);
            }
        }
    }
}