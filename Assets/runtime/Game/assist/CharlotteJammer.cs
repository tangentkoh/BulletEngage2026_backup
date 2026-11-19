using UnityEngine;

public class CharlotteJammer : MonoBehaviour, IAssistantJammer
{
    [Header("衝撃波(弾)プレハブ")]
    [SerializeField] private GameObject shockwaveBulletPrefab;

    public void TriggerJammer(PlayField targetField)
    {
        if (targetField == null || shockwaveBulletPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        // 画面上部中央の座標
        float spawnX = targetField.bounds.center.x;
        float spawnY = targetField.bounds.yMax - 0.2f;
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

        // 相手プレイヤーを取得して狙いをつける (いなければ真下)
        PlayerShip targetPlayer = FindTargetPlayer(targetField);
        Vector3 targetPos = (targetPlayer != null) ? targetPlayer.transform.position : (spawnPos + Vector3.down);
        Vector3 shootDir = (targetPos - spawnPos).normalized;

        // 衝撃波弾の生成＆初期化
        GameObject bulletObj = Instantiate(shockwaveBulletPrefab, spawnPos, Quaternion.identity);
        DeathAgilityBullet bullet = bulletObj.GetComponent<DeathAgilityBullet>();
        if (bullet != null)
        {
            bullet.Initialize(targetField, shootDir);
        }

        Destroy(gameObject);
    }

    private PlayerShip FindTargetPlayer(PlayField field)
    {
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var player in players)
        {
            if (player != null && field != null)
            {
                Vector3 pos = player.transform.position;
                if (pos.x >= field.bounds.xMin && pos.x <= field.bounds.xMax)
                {
                    return player;
                }
            }
        }
        return null;
    }
}