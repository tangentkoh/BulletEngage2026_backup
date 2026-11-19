using UnityEngine;

public class NanaJammer : MonoBehaviour, IAssistantJammer
{
    [Header("消失弾プレハブ")]
    [Tooltip("NanaJammerBulletコンポーネントがアタッチされた弾プレハブ")]
    [SerializeField] private GameObject disappearingBulletPrefab;

    [Header("弾パラメータ")]
    [SerializeField] private float bulletSpeed = 3.0f;
    [SerializeField] private float damage = 5.0f;

    [Header("発射設定")]
    [Tooltip("下方向(-90度)を基準としたランダムな散開角度の幅 (例: 30度なら -120度〜-60度)")]
    [SerializeField] private float spreadAngle = 35.0f;

    public void TriggerJammer(PlayField targetField)
    {
        if (targetField == null || disappearingBulletPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        Rect bounds = targetField.bounds;
        // フィールド上部の中央座標
        Vector3 spawnPos = new Vector3(bounds.center.x, bounds.yMax - 0.5f, 0f);

        // 下方向のランダムな向きに消失弾を2つ放つ
        for (int i = 0; i < 2; i++)
        {
            float randomAngle = Random.Range(-90f - spreadAngle, -90f + spreadAngle) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);

            GameObject bObj = Instantiate(disappearingBulletPrefab, spawnPos, Quaternion.identity);
            DeleteBullet bullet = bObj.GetComponent<DeleteBullet>();
            if (bullet != null)
            {
                bullet.Initialize(targetField, dir, bulletSpeed, damage);
            }
        }

        // 発射完了後に自身を破棄
        Destroy(gameObject);
    }
}