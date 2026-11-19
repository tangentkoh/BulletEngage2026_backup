using UnityEngine;

public class PainsoltJammer : MonoBehaviour, IAssistantJammer
{
    [Header("バウンドフルーツ弾プレハブ")]
    [SerializeField] private GameObject bouncingFruitBulletPrefab;

    [Header("弾スペック")]
    [SerializeField] private float bulletSpeed = 6.0f;
    [SerializeField] private float damage = 12.0f;
    [SerializeField] private int bounceCount = 3;

    public void TriggerJammer(PlayField targetField)
    {
        if (targetField == null || bouncingFruitBulletPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        Rect bounds = targetField.bounds;
        // 画面上部中央の座標
        Vector3 spawnPos = new Vector3(bounds.center.x, bounds.yMax - 0.4f, 0f);

        // 下〜斜め下方向（-135度〜-45度）のランダムな方向へ2つ打ち出す
        for (int i = 0; i < 2; i++)
        {
            float randomAngle = Random.Range(-135f, -45f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);

            GameObject bObj = Instantiate(bouncingFruitBulletPrefab, spawnPos, Quaternion.identity);
            BouncingFruitBullet bullet = bObj.GetComponent<BouncingFruitBullet>();
            if (bullet != null)
            {
                bullet.Initialize(targetField, dir, bulletSpeed, damage, bounceCount);
            }
        }

        Destroy(gameObject);
    }
}