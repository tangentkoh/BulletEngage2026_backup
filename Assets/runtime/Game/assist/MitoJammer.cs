using UnityEngine;

public class MitoJammer : MonoBehaviour, IAssistantJammer
{
    [Header("加速弾プレハブ (AcceleratingBullet付き)")]
    [SerializeField] private GameObject acceleratingBulletPrefab;

    [Header("発射設定")]
    [SerializeField] private int bulletCount = 8;
    [SerializeField] private float edgeMarginX = 0.3f;

    public void TriggerJammer(PlayField targetField)
    {
        if (targetField == null || acceleratingBulletPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        Rect bounds = targetField.bounds;
        float minX = bounds.xMin + edgeMarginX;
        float maxX = bounds.xMax - edgeMarginX;
        float spawnY = bounds.yMax - 0.2f;


        // 真下 (-transform.up) を向けるため、RotZ = 0 度で生成
        Quaternion downwardRot = Quaternion.Euler(0f, 0f, 0f);

        // 画面上部からランダムな X 座標へ 8 個の加速弾を生成
        for (int i = 0; i < bulletCount; i++)
        {
            float spawnX = Random.Range(minX, maxX);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            GameObject bObj = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(acceleratingBulletPrefab, spawnPos, downwardRot)
                : Instantiate(acceleratingBulletPrefab, spawnPos, downwardRot);

            AcceleratingBullet bullet = bObj.GetComponent<AcceleratingBullet>();
            if (bullet != null)
            {
                bullet.Initialize(targetField);
            }
        }

        Destroy(gameObject);
    }
}