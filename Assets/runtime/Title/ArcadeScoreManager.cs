using UnityEngine;

public static class ArcadeScoreManager
{
    private const int MaxRankingCount = 5;
    private const float DefaultTime = 9999.99f; // 未記録時のデフォルト値

    /// <summary>
    /// 指定した主役キャラの指定順位(1~5)のクリアタイム(秒)を取得
    /// </summary>
    public static float GetBestTime(int mainCharIndex, int rank)
    {
        string key = GetKey(mainCharIndex, rank);
        return PlayerPrefs.GetFloat(key, DefaultTime);
    }

    /// <summary>
    /// 新しいクリアタイムを記録保存（上位5位に入れば自動ソートして更新）
    /// </summary>
    public static bool SaveScore(int mainCharIndex, float clearTime)
    {
        float[] times = new float[MaxRankingCount];
        for (int i = 0; i < MaxRankingCount; i++)
        {
            times[i] = GetBestTime(mainCharIndex, i + 1);
        }

        // 新記録かチェック
        bool updated = false;
        for (int i = 0; i < MaxRankingCount; i++)
        {
            if (clearTime < times[i])
            {
                // それ以降をシフト
                for (int j = MaxRankingCount - 1; j > i; j--)
                {
                    times[j] = times[j - 1];
                }
                times[i] = clearTime;
                updated = true;
                break;
            }
        }

        if (updated)
        {
            for (int i = 0; i < MaxRankingCount; i++)
            {
                PlayerPrefs.SetFloat(GetKey(mainCharIndex, i + 1), times[i]);
            }
            PlayerPrefs.Save();
        }

        return updated;
    }

    /// <summary>
    /// タイム(秒)を「00:00.00」形式の文字列に整形
    /// </summary>
    public static string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds >= DefaultTime) return "--:--:--";

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int fraction = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);

        return $"{minutes:00}:{seconds:00}.{fraction:00}";
    }

    private static string GetKey(int mainCharIndex, int rank)
    {
        return $"Arcade_BestTime_Char_{mainCharIndex}_Rank_{rank}";
    }
}