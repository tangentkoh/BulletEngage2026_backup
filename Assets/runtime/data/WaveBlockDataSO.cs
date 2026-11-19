using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EnemySpawnData
{
    [Tooltip("ブロック開始からのスポーン秒数 (0.0〜20.0s)")]
    public float spawnTime;

    [Tooltip("出現させる敵プレハブ")]
    public GameObject enemyPrefab;

    [Tooltip("フィールド内の出現相対X座標 (0.0: 左端, 0.5: 中央, 1.0: 右端)")]
    [Range(0.05f, 0.95f)]
    public float normalizedX;
}

[CreateAssetMenu(fileName = "WaveBlock_PatternA", menuName = "BulletEngage/Wave Block Data")]
public class WaveBlockDataSO : ScriptableObject
{
    public string blockName = "Pattern";
    public float blockDuration = 20.0f; // 1ブロック20秒
    public List<EnemySpawnData> spawns = new List<EnemySpawnData>();
}