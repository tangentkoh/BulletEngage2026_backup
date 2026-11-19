using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "BulletEngage/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string characterId;
    public string characterName;
    [TextArea(2, 3)] public string characterDescription;
    [Tooltip("Lv1カットイン用画像 (切り抜き済み)")]
    public Sprite cutinPortraitLv1;
    [Tooltip("Lv2カットイン用画像 (切り抜き済み)")]
    public Sprite cutinPortraitLv2;
    [Tooltip("Lv3カットイン用立ち絵 (全身)")]
    public Sprite cutinPortraitLv3;

    [Header("Option Shot (個性ショット)")]
    public string optionName;
    [TextArea(2, 3)] public string optionDescription;
    public GameObject optionBulletPrefab;
    public float optionShotInterval = 0.1f;
    public float optionBulletSpeed = 16f;

    [Header("Attacks (相手画面への送信弾幕プレハブ)")]
    [Tooltip("Lv1: 全キャラ共通ノーマルアタック発生体")]
    public GameObject normalAttackPrefab;

    [Header("Lv2: スペシャルアタック")]
    public string specialAttackName;
    [TextArea(2, 3)] public string specialAttackDescription;
    public GameObject specialAttackPrefab;

    [Header("Lv3: エンゲージアタック (ボス)")]
    public string engageAttackName;
    [TextArea(2, 3)] public string engageAttackDescription;
    public GameObject engageAttackPrefab;

    [Header("BGM")]
    public AudioClip themeBGM;
}