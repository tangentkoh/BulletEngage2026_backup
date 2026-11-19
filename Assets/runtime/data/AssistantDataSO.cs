using UnityEngine;

[CreateAssetMenu(fileName = "NewAssistant", menuName = "BulletEngage/Assistant Data")]
public class AssistantDataSO : ScriptableObject
{
    [Header("基本情報")]
    public string assistantId;
    public string assistantName;
    public Sprite cutinPortrait;

    [Header("1. サポートスキル (自機バフ)")]
    public string supportSkillName;
    [TextArea(2, 3)] public string supportDescription;
    public GameObject supportPrefab;    // バフ処理・エフェクトを持つプレハブ

    [Header("2. ジャマースキル (相手妨害)")]
    public string jammerSkillName;
    [TextArea(2, 3)] public string jammerDescription;
    public GameObject jammerPrefab;     // 妨害弾幕・障害物エミッタープレハブ

    [Header("3. エンゲージスキル (相乗りボス)")]
    public string engageSkillName;
    [TextArea(2, 3)] public string engageDescription;
    public GameObject assistBossPrefab; // 無敵・デバフ専用の相乗りボスプレハブ
}