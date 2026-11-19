using System.Collections;
using UnityEngine;
using LitMotion;

public class PaintObstacle : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5.0f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("スプライトバリエーション")]
    [SerializeField] private Sprite[] spriteVariations;
    [SerializeField] private bool randomizeSprite = true;
    [SerializeField] private int specificSpriteIndex = 0;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        ApplySprite();
    }

    private void Start()
    {
        StartCoroutine(LifeRoutine());
    }

    private IEnumerator LifeRoutine()
    {
        // 発生時のポップ演出 (スケール 0 -> 1)
        Vector3 targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
        LMotion.Create(Vector3.zero, targetScale, 0.25f)
            .WithEase(Ease.OutBack)
            .Bind(s => transform.localScale = s);

        yield return new WaitForSeconds(lifeTime - fadeOutDuration);

        // フェードアウト消滅
        if (spriteRenderer != null)
        {
            Color initialColor = spriteRenderer.color;
            LMotion.Create(initialColor.a, 0f, fadeOutDuration)
                .WithEase(Ease.InQuad)
                .Bind(a => {
                    if (spriteRenderer != null)
                    {
                        Color c = initialColor;
                        c.a = a;
                        spriteRenderer.color = c;
                    }
                });
        }

        yield return new WaitForSeconds(fadeOutDuration);
        Destroy(gameObject);
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