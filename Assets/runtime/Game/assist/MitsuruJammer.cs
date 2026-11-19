using UnityEngine;

public class MitsuruJammer : MonoBehaviour, IAssistantJammer
{
    [SerializeField] private GameObject coinBulletPrefab;
    [SerializeField] private int coinCount = 3;
    [SerializeField] private float marginX = 0.5f;

    public void TriggerJammer(PlayField targetField)
    {
        if (targetField == null || coinBulletPrefab == null) return;

        float spawnY = targetField.bounds.yMax + 0.3f;
        float minX = targetField.bounds.xMin + marginX;
        float maxX = targetField.bounds.xMax - marginX;

        // ランダムなX座標から3発落下
        for (int i = 0; i < coinCount; i++)
        {
            float spawnX = Random.Range(minX, maxX);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            GameObject coin = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(coinBulletPrefab, spawnPos, Quaternion.identity)
                : Instantiate(coinBulletPrefab, spawnPos, Quaternion.identity);
            AcceleratingBullet bulletScript = coin.GetComponent<AcceleratingBullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(targetField);
            }
        }

        Destroy(gameObject);
    }
}