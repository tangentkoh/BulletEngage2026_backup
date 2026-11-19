using System.Collections.Generic;
using UnityEngine;

public class AssistantManager : MonoBehaviour
{
    public static AssistantManager Instance { get; private set; }

    [Header("全助手リスト (登録用)")]
    [SerializeField] private List<AssistantDataSO> availableAssistants = new List<AssistantDataSO>();

    [Header("テスト用選択")]
    [SerializeField] private int p1AssistantIndex = 2;
    [SerializeField] private int p2AssistantIndex = 0;

    [Header("適用中の助手データ (実行時確認用)")]
    [SerializeField] private AssistantDataSO assistant1P;
    [SerializeField] private AssistantDataSO assistant2P;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        // タイトル画面で選択した助手インデックスを適用
        if (BattleSettingData.Instance != null && BattleSettingData.Instance.IsConfigured)
        {
            p1AssistantIndex = BattleSettingData.Instance.P1Data.assistCharIndex;
            p2AssistantIndex = BattleSettingData.Instance.P2Data.assistCharIndex;
        }
        RefreshSelectedAssistants();
    }

    public void RefreshSelectedAssistants()
    {
        if (availableAssistants.Count > p1AssistantIndex) assistant1P = availableAssistants[p1AssistantIndex];
        if (availableAssistants.Count > p2AssistantIndex) assistant2P = availableAssistants[p2AssistantIndex];
    }

    // 自機生成完了時に呼び出してサポートスキルを適用
    public void ApplyInitialSupport(PlayerShip ship1P, PlayerShip ship2P)
    {
        RefreshSelectedAssistants();
        Debug.Log($"[AssistantManager] サポート適用開始 - 1P: {(assistant1P != null ? assistant1P.name : "null")}, 2P: {(assistant2P != null ? assistant2P.name : "null")}");

        if (ship1P != null) ApplySupportSkill(ship1P, assistant1P, 0);
        if (ship2P != null) ApplySupportSkill(ship2P, assistant2P, 1);
    }

    private void ApplySupportSkill(PlayerShip ship, AssistantDataSO data, int playerIndex)
    {
        if (ship == null)
        {
            Debug.LogWarning($"[AssistantManager] {playerIndex + 1}P の PlayerShip が null です。");
            return;
        }

        if (data == null)
        {
            Debug.LogError($"[AssistantManager] {playerIndex + 1}P の AssistantDataSO が未設定です！ Available Assistants リストまたは Index を確認してください。");
            return;
        }

        if (data.supportPrefab == null)
        {
            Debug.LogError($"[AssistantManager] {data.name} の Support Prefab が未割り当てです！");
            return;
        }

        GameObject supportObj = Instantiate(data.supportPrefab, ship.transform);
        // ルートだけでなく子階層にあっても確実に取得
        IAssistantSupport support = supportObj.GetComponentInChildren<IAssistantSupport>();
        if (support != null)
        {
            support.ApplySupport(ship);
        }
        else
        {
            Debug.LogError($"[AssistantManager] {data.supportPrefab.name} に IAssistantSupport (FridaSupport や SigureSupport) がアタッチされていません！");
        }
    }

    // BulletSender から敵撃破カウント到達時に呼ばれる
    public void ExecuteJammer(int playerIndex, PlayField targetField)
    {
        if (targetField == null) return;

        AssistantDataSO targetData = (playerIndex == 0) ? assistant1P : assistant2P;
        if (targetData == null || targetData.jammerPrefab == null) return;

        GameObject jammerObj = Instantiate(targetData.jammerPrefab);
        IAssistantJammer jammer = jammerObj.GetComponent<IAssistantJammer>();
        jammer?.TriggerJammer(targetField);
    }

    // 主人公ボス(Lv3)出現時に呼ばれる相乗りボス生成
    public void SpawnAssistBoss(int playerIndex, PlayField targetField, EngageBossBase mainBoss)
    {
        if (targetField == null || mainBoss == null) return;

        AssistantDataSO targetData = (playerIndex == 0) ? assistant1P : assistant2P;
        if (targetData == null || targetData.assistBossPrefab == null) return;

        GameObject assistBossObj = Instantiate(targetData.assistBossPrefab);
        AssistantBossBase assistBoss = assistBossObj.GetComponent<AssistantBossBase>();
        assistBoss?.Initialize(targetField, mainBoss);
    }

    public AssistantDataSO GetAssistantData(int playerIndex)
    {
        return (playerIndex == 0) ? assistant1P : assistant2P;
    }
}
