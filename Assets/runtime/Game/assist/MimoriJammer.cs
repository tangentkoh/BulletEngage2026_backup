using System.Collections;
using UnityEngine;

public class MimoriJammer : MonoBehaviour, IAssistantJammer
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int shotCount = 8;
    [SerializeField] private float fireInterval = 0.12f;
    [SerializeField] private float bulletSpeed = 9.0f;
    [SerializeField] private float bulletDamage = 10f;

    [Tooltip("上下へのブレ角度 (±度)")]
    [SerializeField] private float spreadAngle = 10.0f;

    public void TriggerJammer(PlayField targetField)
    {
        StartCoroutine(FireHorizontalBullets(targetField));
    }

    private IEnumerator FireHorizontalBullets(PlayField field)
    {
        if (field == null || bulletPrefab == null) yield break;

        // 相手フィールドの右端から発射
        for (int i = 0; i < shotCount; i++)
        {
            float spawnY = Random.Range(field.bounds.yMin + 1.0f, field.bounds.yMax - 2.0f);
            Vector3 spawnPos = new Vector3(field.bounds.xMax, spawnY, 0f);

            // 左向き (180度) をベースに上下へのブレ角度を加算
            float randomOffset = Random.Range(-spreadAngle, spreadAngle);
            float angle = 180f + randomOffset;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 shootDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            GameObject b = EnemyBulletPool.Instance != null
                ? EnemyBulletPool.Instance.Get(bulletPrefab, spawnPos, Quaternion.identity)
                : Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

            EnemyBullet eb = b.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.Initialize(field, shootDir, bulletSpeed, bulletDamage);
            }

            yield return new WaitForSeconds(fireInterval);
        }

        Destroy(gameObject);
    }
}