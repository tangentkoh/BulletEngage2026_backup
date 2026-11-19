using System.Collections;
using UnityEngine;

public class MidBossSpawner : MonoBehaviour
{
    [Header("フィールド参照")]
    [SerializeField] private PlayField field1P;
    [SerializeField] private PlayField field2P;

    [Header("特殊敵プレハブ (2種: HP回復 / エネルギー回復)")]
    [SerializeField] private GameObject midBossHPPrefab;
    [SerializeField] private GameObject midBossEnergyPrefab;

    [Header("出現間隔 (秒)")]
    [SerializeField] private float minInterval = 25f;
    [SerializeField] private float maxInterval = 40f;

    private void Start()
    {
        if (field1P != null) StartCoroutine(SpawnLoop(field1P));
        if (field2P != null) StartCoroutine(SpawnLoop(field2P));
    }

    private IEnumerator SpawnLoop(PlayField targetField)
    {
        yield return new WaitUntil(() => BattleGameManager.Instance != null && BattleGameManager.Instance.IsGameStarted);
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            // どちらの特殊敵（HP回復 or エネルギー回復）にするか抽選
            GameObject selectedPrefab = (Random.value < 0.5f) ? midBossHPPrefab : midBossEnergyPrefab;

            if (selectedPrefab != null && targetField != null)
            {
                float spawnX = Random.Range(targetField.bounds.xMin + 1.0f, targetField.bounds.xMax - 1.0f);
                float spawnY = targetField.bounds.yMax + 0.5f;
                Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

                GameObject boss = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
                EnemyBase enemyScript = boss.GetComponent<EnemyBase>();
                if (enemyScript != null)
                {
                    enemyScript.CurrentField = targetField;
                }
            }
        }
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        this.enabled = false;
    }
}