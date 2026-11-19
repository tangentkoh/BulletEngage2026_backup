using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VictoryMessageSO", menuName = "ScriptableObjects/VictoryMessageSO")]
public class VictoryMessageSO : ScriptableObject
{
    [Header("対戦相手キャラIDごとの勝利メッセージ")]
    [TextArea(2, 3)]
    [SerializeField] private List<string> messagesForOpponents = new();

    [Header("デフォルトメッセージ")]
    [TextArea(2, 3)]
    [SerializeField] private string defaultMessage = "勝利した！";

    public string GetMessage(int opponentIndex)
    {
        if (opponentIndex >= 0 && opponentIndex < messagesForOpponents.Count && !string.IsNullOrEmpty(messagesForOpponents[opponentIndex]))
        {
            return messagesForOpponents[opponentIndex];
        }
        return defaultMessage;
    }
}