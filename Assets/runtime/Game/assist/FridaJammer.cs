using System.Collections;
using UnityEngine;

public class FridaJammer : MonoBehaviour, IAssistantJammer
{
    [SerializeField] private GameObject arrowBulletPrefab;
    [SerializeField] private int shotCount = 5;
    [SerializeField] private float fireInterval = 0.12f;
    [SerializeField] private float arrowSpeed = 9.0f;
    [SerializeField] private float arrowDamage = 10f;

    public void TriggerJammer(PlayField targetField)
    {
        StartCoroutine(FireHorizontalArrows(targetField));
    }

    private IEnumerator FireHorizontalArrows(PlayField field)
    {
        if (field == null || arrowBulletPrefab == null) yield break;

        // 相手フィールドの左端、Y座標はプレイヤーが居る高さ付近（ランダム）
        for (int i = 0; i < shotCount; i++)
        {
            float spawnY = Random.Range(field.bounds.yMin + 1.0f, field.bounds.yMax - 2.0f);
            Vector3 spawnPos = new Vector3(field.bounds.xMin, spawnY, 0f);

            // 右向き(Vector3.right)へ水平発射、矢の角度を右(-90度補正など)に合わせる
            Quaternion rot = Quaternion.Euler(0f, 0f, -90f);
            GameObject b = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(arrowBulletPrefab, spawnPos, rot)
                : Instantiate(arrowBulletPrefab, spawnPos, rot);
            
            EnemyBullet eb = b.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(field, Vector3.right, arrowSpeed, arrowDamage);
            }

            yield return new WaitForSeconds(fireInterval);
        }

        Destroy(gameObject);
    }
}