using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("フィールド参照")]
    [SerializeField] private PlayField field1P;
    [SerializeField] private PlayField field2P;

    [Header("ブロックデータ一覧 (ここからランダム抽選)")]
    [SerializeField] private List<WaveBlockDataSO> waveBlocks;

    private int lastBlockIndex = -1;
    private Coroutine waveCoroutine;

    private void Start()
    {
        if (waveBlocks != null && waveBlocks.Count > 0)
        {
            waveCoroutine = StartCoroutine(WaveLoopRoutine());
        }
    }

    private IEnumerator WaveLoopRoutine()
    {
        yield return new WaitUntil(() => BattleGameManager.Instance != null && BattleGameManager.Instance.IsGameStarted);
        while (true)
        {
            // 前回と重複しないブロックをランダム抽選
            int blockIndex = Random.Range(0, waveBlocks.Count);
            if (waveBlocks.Count > 1 && blockIndex == lastBlockIndex)
            {
                blockIndex = (blockIndex + 1) % waveBlocks.Count;
            }
            lastBlockIndex = blockIndex;

            WaveBlockDataSO currentBlock = waveBlocks[blockIndex];
            Debug.Log($"[WaveManager] 新ブロック開始: {currentBlock.blockName} (20秒)");

            // 1ブロック（20秒）の実行
            float timer = 0f;
            int spawnIndex = 0;

            // スポーン時刻順にソートしたリストを走査
            var sortedSpawns = new List<EnemySpawnData>(currentBlock.spawns);
            sortedSpawns.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

            while (timer < currentBlock.blockDuration)
            {
                timer += Time.deltaTime;

                while (spawnIndex < sortedSpawns.Count && sortedSpawns[spawnIndex].spawnTime <= timer)
                {
                    SpawnEnemyBothSides(sortedSpawns[spawnIndex]);
                    spawnIndex++;
                }

                yield return null;
            }
        }
    }

    private void SpawnEnemyBothSides(EnemySpawnData data)
    {
        if (data.enemyPrefab == null) return;

        // 1P フィールドに生成
        if (field1P != null)
        {
            SpawnAtField(field1P, data);
        }

        // 2P フィールドに同一条件で生成
        if (field2P != null)
        {
            SpawnAtField(field2P, data);
        }
    }

    private void SpawnAtField(PlayField field, EnemySpawnData data)
    {
        float posX = Mathf.Lerp(field.bounds.xMin, field.bounds.xMax, data.normalizedX);
        float posY = field.bounds.yMax + 0.3f; // 画面上部から出現
        Vector3 spawnPos = new Vector3(posX, posY, 0f);

        GameObject enemyObj = Instantiate(data.enemyPrefab, spawnPos, Quaternion.identity);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.CurrentField = field;
        }
    }

    public void StopSpawning()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        StopAllCoroutines();
        this.enabled = false;
    }
}