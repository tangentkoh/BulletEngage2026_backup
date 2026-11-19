using UnityEngine;

public class AnnaJammer : MonoBehaviour, IAssistantJammer
{
    [Header("落下星プレハブ")]
    [Tooltip("DeleteBulletコンポーネントがアタッチされた大サイズの星プレハブ")]
    [SerializeField] private GameObject starBulletPrefab;

    [Header("弾パラメータ")]
    [SerializeField] private float starSpeed = 2.5f;
    [SerializeField] private float damage = 15.0f;

    [Header("生成マージン設定")]
    [Tooltip("画面端からのX軸スポーン禁止マージン")]
    [SerializeField] private float sideMargin = 0.6f;

    public void TriggerJammer(PlayField targetField)
    {
        if (targetField == null || starBulletPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        Rect bounds = targetField.bounds;

        // 画面上部のランダムなX座標を決定
        float spawnX = Random.Range(bounds.xMin + sideMargin, bounds.xMax - sideMargin);
        Vector3 spawnPos = new Vector3(spawnX, bounds.yMax + 0.8f, 0f);

        // 垂直下方向（真下）に生成
        GameObject bObj = Instantiate(starBulletPrefab, spawnPos, Quaternion.identity);
        DeleteBullet bullet = bObj.GetComponent<DeleteBullet>();
        if (bullet != null)
        {
            // 第5引数 isStarMode = true で自機弾貫通 & 被弾時非消滅モードで起動
            bullet.Initialize(targetField, Vector3.down, starSpeed, damage, isStarMode: true);
        }

        Destroy(gameObject);
    }
}