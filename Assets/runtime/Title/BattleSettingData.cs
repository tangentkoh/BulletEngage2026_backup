using System.Collections.Generic;
using UnityEngine;

public enum MatchType
{
    HumanVsHuman,
    HumanVsCPU,
    CPUVsCPU
}

public class PlayerSelectData
{
    public int mainCharIndex = 0;
    public int assistCharIndex = 1;
    public bool isCPU = false;
    public CPUDifficulty cpuDifficulty = CPUDifficulty.Normal;
}

public enum CPUDifficulty
{
    Easy,
    Normal,
    Hard,
    Lunatic
}

[System.Serializable]
public struct ArcadeOpponentData
{
    public int mainCharIndex;
    public int assistCharIndex;
}

public class BattleSettingData : MonoBehaviour
{
    private static BattleSettingData instance;
    public static BattleSettingData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<BattleSettingData>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("BattleSettingData");
                    instance = obj.AddComponent<BattleSettingData>();
                }
            }
            return instance;
        }
    }

    public MatchType CurrentMatchType { get; set; } = MatchType.HumanVsCPU;
    public PlayerSelectData P1Data { get; private set; } = new PlayerSelectData();
    public PlayerSelectData P2Data { get; private set; } = new PlayerSelectData();
    public bool IsConfigured { get; set; } = false;

    // --- アーケードモード用データ ---
    public bool IsArcadeMode { get; set; } = false;
    public int CurrentArcadeStage { get; set; } = 1;      // 1 ~ 3
    public float TotalArcadeTime { get; set; } = 0f;       // 純粋な試合時間の合計

    // 3戦分の事前確定敵データ
    public ArcadeOpponentData[] ArcadeOpponents { get; private set; } = new ArcadeOpponentData[3];

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// アーケードモードの進行状態およびフラグを完全に初期化
    /// </summary>
    public void ResetArcadeData()
    {
        IsArcadeMode = false;
        CurrentArcadeStage = 1;
        TotalArcadeTime = 0f;
        ArcadeOpponents = new ArcadeOpponentData[3];
    }

    /// <summary>
    /// 1P主役と被らず、かつ3戦の中で敵主役が絶対重複しないように3戦分の敵を一括抽選
    /// </summary>
    public void GenerateArcadeOpponents(int p1MainIndex, int totalCharCount)
    {
        ResetArcadeData();
        IsArcadeMode = true;

        // 1P主役以外の利用可能主役インデックスリストを作成
        List<int> availableMainIndices = new List<int>();
        for (int i = 0; i < totalCharCount; i++)
        {
            if (i != p1MainIndex)
            {
                availableMainIndices.Add(i);
            }
        }

        // リストをフィッシャー–イェーツ法でシャッフル
        for (int i = availableMainIndices.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            int temp = availableMainIndices[i];
            availableMainIndices[i] = availableMainIndices[rnd];
            availableMainIndices[rnd] = temp;
        }

        // 3戦分の敵（主役・助手）を確定保存
        for (int stage = 0; stage < 3; stage++)
        {
            int enemyMain = (stage < availableMainIndices.Count)
                ? availableMainIndices[stage]
                : Random.Range(0, totalCharCount); // キャラ総数が少ない場合の安全弁

            // 助手はそのステージの敵主役と重複しないものを抽選
            int enemyAssist;
            do
            {
                enemyAssist = Random.Range(0, totalCharCount);
            } while (enemyAssist == enemyMain && totalCharCount > 1);

            ArcadeOpponents[stage] = new ArcadeOpponentData
            {
                mainCharIndex = enemyMain,
                assistCharIndex = enemyAssist
            };
        }
    }

    public bool P1Handicap { get; set; } = false;
    public bool P2Handicap { get; set; } = false;

    public void ResetHandicaps()
    {
        P1Handicap = false;
        P2Handicap = false;
    }
}