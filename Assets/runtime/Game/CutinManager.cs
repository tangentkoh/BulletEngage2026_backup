using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitMotion;

public class CutinManager : MonoBehaviour
{
    public static CutinManager Instance { get; private set; }

    [Header("1P / 2P 帯スプライト設定")]
    [SerializeField] private Sprite lv1Bg1P;
    [SerializeField] private Sprite lv1Bg2P;
    [SerializeField] private Sprite lv2Bg1P;
    [SerializeField] private Sprite lv2Bg2P;
    [SerializeField] private Sprite lv3Bg1P;
    [SerializeField] private Sprite lv3Bg2P;

    [Header("UI コンポーネント参照 (1P/2P 共通1セット)")]
    [SerializeField] private CanvasGroup cutinCanvasGroup;
    [SerializeField] private Image bgBannerImage;

    [Header("Lv1 / Lv2 用画像")]
    [SerializeField] private Image mainPortraitLv1;
    [SerializeField] private Image mainPortraitLv2;

    [Header("Lv3 用キャラクターImage")]
    [SerializeField] private Image mainPortraitLv3;   // 1P: 主役 / 2P: 助手側位置
    [SerializeField] private Image assistPortraitLv3; // 1P: 助手 / 2P: 主役側位置

    [Header("技名テキスト UI")]
    [SerializeField] private TextMeshProUGUI attackNameText;
    [SerializeField] private TextMeshProUGUI subAttackNameText; // Lv3 助手技名用

    [Header("演出基本設定")]
    [Tooltip("スライド開始/退場時のX軸オフセット距離")]
    [SerializeField] private float slideXOffset = 800f;

    private Coroutine cutinCoroutine;
    private readonly Dictionary<RectTransform, Vector2> basePositions = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (cutinCanvasGroup != null) cutinCanvasGroup.alpha = 0f;

        // 全 UI 要素の Inspector 上の位置(1P基準)を記録
        RegisterRect(mainPortraitLv1?.rectTransform);
        RegisterRect(mainPortraitLv2?.rectTransform);
        RegisterRect(mainPortraitLv3?.rectTransform);
        RegisterRect(assistPortraitLv3?.rectTransform);
        RegisterRect(attackNameText?.rectTransform);
        RegisterRect(subAttackNameText?.rectTransform);

        gameObject.SetActive(true);
    }

    private void RegisterRect(RectTransform rect)
    {
        if (rect != null && !basePositions.ContainsKey(rect))
        {
            basePositions[rect] = rect.anchoredPosition;
        }
    }

    /// <summary>
    /// カットイン演出の再生エントリーポイント
    /// </summary>
    public void PlayCutin(int playerIndex, int level, CharacterDataSO mainChar, AssistantDataSO assistChar = null)
    {
        if (cutinCoroutine != null) StopCoroutine(cutinCoroutine);

        if (level == 3)
        {
            cutinCoroutine = StartCoroutine(PlayLv3CutinRoutine(playerIndex, mainChar, assistChar));
        }
        else
        {
            cutinCoroutine = StartCoroutine(PlayLv12CutinRoutine(playerIndex, level, mainChar));
        }
    }

    private void HideAllUI()
    {
        SetTargetActive(mainPortraitLv1, false);
        SetTargetActive(mainPortraitLv2, false);
        SetTargetActive(mainPortraitLv3, false);
        SetTargetActive(assistPortraitLv3, false);
        SetTargetActive(attackNameText, false);
        SetTargetActive(subAttackNameText, false);
    }

    private void SetTargetActive(Component comp, bool active)
    {
        if (comp != null) comp.gameObject.SetActive(active);
    }

    // --- Lv1 / Lv2 カットインルーチン ---
    private IEnumerator PlayLv12CutinRoutine(int playerIndex, int level, CharacterDataSO mainChar)
    {
        bool isP1 = (playerIndex == 0);
        HideAllUI();

        // 1. 帯スプライト設定
        Sprite bgSprite = (level == 1) ? (isP1 ? lv1Bg1P : lv1Bg2P) : (isP1 ? lv2Bg1P : lv2Bg2P);
        if (bgBannerImage != null) bgBannerImage.sprite = bgSprite;

        // 2. 立ち絵画像の設定
        Image targetPortrait = (level == 1) ? mainPortraitLv1 : mainPortraitLv2;
        Sprite portraitSprite = mainChar != null ? ((level == 1) ? mainChar.cutinPortraitLv1 : mainChar.cutinPortraitLv2) : null;

        if (targetPortrait != null)
        {
            if (portraitSprite != null) targetPortrait.sprite = portraitSprite;
            targetPortrait.gameObject.SetActive(true);

            // 2P時は立ち絵画像を左右反転
            targetPortrait.rectTransform.localScale = new Vector3(isP1 ? 1f : -1f, 1f, 1f);
        }

        // 3. 技名テキスト設定
        string attackName = (level == 1) ? "ノーマルアタック" : (mainChar != null ? mainChar.specialAttackName : "");
        string colorHex = isP1 ? "#7777FF" : "#FF7777";

        if (attackNameText != null)
        {
            if (!string.IsNullOrEmpty(attackName))
            {
                attackNameText.text = $"<color={colorHex}>{attackName}</color>";
            }
            attackNameText.gameObject.SetActive(true);
            attackNameText.rectTransform.localScale = Vector3.one;
        }

        // 4. 時間低速化 & SE再生
        Time.timeScale = 0.02f;
        SoundManager.Instance?.PlayCutinCommonSE();

        if (cutinCanvasGroup != null) cutinCanvasGroup.alpha = 1f;

        // 5. アニメーション実行 (1P: 左から右 / 2P: 右から左)
        float startOffset = isP1 ? -slideXOffset : slideXOffset;

        AnimateSlideIn(targetPortrait?.rectTransform, startOffset, isP1, 0.4f);
        AnimateSlideIn(attackNameText?.rectTransform, startOffset, isP1, 0.4f);

        yield return new WaitForSecondsRealtime(1.0f); // 保持時間

        // スライドアウト
        AnimateSlideOut(targetPortrait?.rectTransform, -startOffset, isP1, 0.4f);
        AnimateSlideOut(attackNameText?.rectTransform, -startOffset, isP1, 0.4f);

        if (cutinCanvasGroup != null)
        {
            LMotion.Create(1f, 0f, 0.25f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => cutinCanvasGroup.alpha = a);
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // 6. 復元 & クリーンアップ
        Time.timeScale = 1.0f;
        if (cutinCanvasGroup != null) cutinCanvasGroup.alpha = 0f;
        HideAllUI();
        cutinCoroutine = null;
    }

    // --- Lv3 カットインルーチン ---
    private IEnumerator PlayLv3CutinRoutine(int playerIndex, CharacterDataSO mainChar, AssistantDataSO assistChar)
    {
        bool isP1 = (playerIndex == 0);
        HideAllUI();

        // 1. 帯スプライト設定
        if (bgBannerImage != null) bgBannerImage.sprite = isP1 ? lv3Bg1P : lv3Bg2P;

        // スロットは固定（1P/2Pで入れ替えない）
        // mainPortraitLv3  ＝ 主役用 Image
        // assistPortraitLv3 ＝ 助手用 Image
        Image mainImg = mainPortraitLv3;
        Image assistImg = assistPortraitLv3;

        // 主役立ち絵の設定
        if (mainImg != null)
        {
            if (mainChar != null && mainChar.cutinPortraitLv3 != null) mainImg.sprite = mainChar.cutinPortraitLv3;
            mainImg.gameObject.SetActive(true);
            mainImg.rectTransform.localScale = new Vector3(isP1 ? 1f : -1f, 1f, 1f); // 2P時は画像のみ左右反転
        }

        // 助手立ち絵の設定
        if (assistImg != null)
        {
            if (assistChar != null && assistChar.cutinPortrait != null) assistImg.sprite = assistChar.cutinPortrait;
            assistImg.gameObject.SetActive(true);
            assistImg.rectTransform.localScale = new Vector3(isP1 ? 1f : -1f, 1f, 1f); // 2P時は画像のみ左右反転
        }

        // 技名テキスト設定
        string colorHex = isP1 ? "#7777FF" : "#FF7777";
        if (attackNameText != null)
        {
            if (mainChar != null && !string.IsNullOrEmpty(mainChar.engageAttackName))
                attackNameText.text = $"<color={colorHex}>{mainChar.engageAttackName}</color>";
            attackNameText.gameObject.SetActive(true);
            attackNameText.rectTransform.localScale = Vector3.one;
        }

        if (subAttackNameText != null)
        {
            if (assistChar != null && !string.IsNullOrEmpty(assistChar.engageSkillName))
                subAttackNameText.text = $"<color={colorHex}>{assistChar.engageSkillName}</color>";
            subAttackNameText.gameObject.SetActive(true);
            subAttackNameText.rectTransform.localScale = Vector3.one;
        }

        // 2. 時間低速化 & SE再生
        Time.timeScale = 0.02f;
        SoundManager.Instance?.PlayCutinCommonSE();

        if (cutinCanvasGroup != null) cutinCanvasGroup.alpha = 1f;

        // 3. アニメーション展開
        float mainOffset = isP1 ? -slideXOffset : slideXOffset;   // 主役の移動方向 (1P:左から / 2P:右から)
        float assistOffset = isP1 ? slideXOffset : -slideXOffset; // 助手の移動方向 (1P:右から / 2P:左から)

        // ① 主役立ち絵 & 主役技名がスライドイン
        AnimateSlideIn(mainImg?.rectTransform, mainOffset, isP1, 0.4f);
        AnimateSlideIn(attackNameText?.rectTransform, mainOffset, isP1, 0.4f);

        yield return new WaitForSecondsRealtime(0.2f);

        // ② 助手立ち絵は反対側からクロス、助手技名は主役と同じ方向からスライドイン
        AnimateSlideIn(assistImg?.rectTransform, assistOffset, isP1, 0.4f);
        AnimateSlideIn(subAttackNameText?.rectTransform, mainOffset, isP1, 0.4f);

        yield return new WaitForSecondsRealtime(0.2f);
        SoundManager.Instance?.PlayCutinLv3SE();
        yield return new WaitForSecondsRealtime(1.0f); // 決めポーズ保持

        // ③ 退場アニメーション
        AnimateSlideOut(mainImg?.rectTransform, -mainOffset, isP1, 0.4f);
        AnimateSlideOut(attackNameText?.rectTransform, -mainOffset, isP1, 0.4f);

        AnimateSlideOut(assistImg?.rectTransform, -assistOffset, isP1, 0.4f);
        AnimateSlideOut(subAttackNameText?.rectTransform, -mainOffset, isP1, 0.4f);

        if (cutinCanvasGroup != null)
        {
            LMotion.Create(1f, 0f, 0.3f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => cutinCanvasGroup.alpha = a);
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // 4. 復元 & クリーンアップ
        Time.timeScale = 1.0f;
        if (cutinCanvasGroup != null) cutinCanvasGroup.alpha = 0f;
        HideAllUI();
        cutinCoroutine = null;
    }

    // --- 基準位置 (1P基準/2P反転基準) へのスライドヘルパー ---
    private void AnimateSlideIn(RectTransform rect, float startXOffset, bool isP1, float duration)
    {
        if (rect == null) return;

        if (!basePositions.TryGetValue(rect, out Vector2 basePos)) basePos = rect.anchoredPosition;

        // 1Pなら基準位置(basePos.x)、2Pなら反対側(-basePos.x)
        float targetX = isP1 ? basePos.x : -basePos.x;
        float startX = targetX + startXOffset;

        rect.anchoredPosition = new Vector2(startX, basePos.y);

        LMotion.Create(startX, targetX, duration)
            .WithEase(Ease.OutCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(x => rect.anchoredPosition = new Vector2(x, basePos.y));
    }

    private void AnimateSlideOut(RectTransform rect, float exitXOffset, bool isP1, float duration)
    {
        if (rect == null) return;

        if (!basePositions.TryGetValue(rect, out Vector2 basePos)) basePos = rect.anchoredPosition;

        float targetX = isP1 ? basePos.x : -basePos.x;
        float currentX = rect.anchoredPosition.x;
        float exitX = targetX + exitXOffset;

        LMotion.Create(currentX, exitX, duration)
            .WithEase(Ease.InCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(x => rect.anchoredPosition = new Vector2(x, basePos.y));
    }
}