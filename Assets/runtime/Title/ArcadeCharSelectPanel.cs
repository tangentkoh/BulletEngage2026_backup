using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using LitMotion;

public class ArcadeCharSelectPanel : MonoBehaviour
{
    private enum Step { P1_Main, P1_Assist, ReadyToStart }

    [Header("パネル & ボタン")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private List<Button> charButtons = new();
    [SerializeField] private int columns = 2;

    [Header("SO データ参照")]
    [SerializeField] private List<CharacterDataSO> characterDataList = new();
    [SerializeField] private List<AssistantDataSO> assistantDataList = new();

    [Header("立ち絵 UI (0:1P主, 1:1P助)")]
    [SerializeField] private Image p1MainPortrait;
    [SerializeField] private Image p1AssistPortrait;
    [SerializeField] private List<Sprite> characterPortraits = new();
    [SerializeField] private Sprite dummyPortraitSprite;
    [SerializeField] private float slideOffset = 60f;

    [Header("詳細ポップアップ UI (Shift / L・R)")]
    [SerializeField] private GameObject p1DetailPanel;
    [SerializeField] private TextMeshProUGUI p1DetailText;

    [Header("カーソル & テキスト UI")]
    [SerializeField] private RectTransform p1Cursor;
    [SerializeField] private TextMeshProUGUI p1SelectionText;
    [SerializeField] private TextMeshProUGUI p1DescriptionText;

    [Header("2P領域用: クリアタイムランキング表示 (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI rankingTitleText;
    [SerializeField] private TextMeshProUGUI rankingContentText;

    [Header("決定・開始ボタン")]
    [SerializeField] private Button startBattleButton;
    [SerializeField] private string battleSceneName = "Game";

    private Action onBackCallback;
    private Step step = Step.P1_Main;
    private bool isOpen, p1DetailOpen, isStarting, p1IsHolding;
    private int p1CursorIdx;

    private UIControls p1Controls;
    private readonly Vector2[] basePortraitPos = new Vector2[2];
    private readonly CanvasGroup[] portraitCgs = new CanvasGroup[2];
    private readonly MotionHandle[] portraitMotions = new MotionHandle[2];

    private Vector2 p1NavInput;
    private float p1NextRepeat;

    private const float DeadZone = 0.5f;
    private const float InitialRepeatDelay = 0.25f;
    private const float RepeatInterval = 0.12f;

    private Image[] GetPortraits() => new[] { p1MainPortrait, p1AssistPortrait };

    private void Awake()
    {
        p1Controls = new UIControls();
        BindInputs();
        DisableNavigations();

        Image[] ports = GetPortraits();
        for (int i = 0; i < 2; i++)
        {
            if (ports[i] == null) continue;
            basePortraitPos[i] = ports[i].rectTransform.anchoredPosition;
            if (!ports[i].TryGetComponent(out portraitCgs[i]))
                portraitCgs[i] = ports[i].gameObject.AddComponent<CanvasGroup>();
        }

        if (panelRect != null) panelRect.anchoredPosition = new Vector2(1920f, panelRect.anchoredPosition.y);
        p1DetailPanel?.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnDestroy() { p1Controls?.Dispose(); }

    private void Update()
    {
        if (!isOpen) return;

        ProcessStickInput(p1NavInput, ref p1IsHolding, ref p1NextRepeat);
        CheckDetailToggle();
    }

    private void ProcessStickInput(Vector2 input, ref bool isHolding, ref float nextRepeat)
    {
        if (input.magnitude < DeadZone) { isHolding = false; return; }

        if (!isHolding || Time.unscaledTime >= nextRepeat)
        {
            Vector2 cardinalDir = (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                ? new Vector2(Mathf.Sign(input.x), 0f)
                : new Vector2(0f, Mathf.Sign(input.y));

            OnP1Navigate(cardinalDir);

            nextRepeat = Time.unscaledTime + (isHolding ? RepeatInterval : InitialRepeatDelay);
            isHolding = true;
        }
    }

    private void CheckDetailToggle()
    {
        if (!IsDetailPressed()) return;

        p1DetailOpen = !p1DetailOpen;
        p1DetailPanel?.SetActive(p1DetailOpen);
        UpdateDetailText();
    }

    private bool IsDetailPressed()
    {
        var devices = p1Controls.devices;
        if (devices == null) return false;

        foreach (var dev in devices)
        {
            if (dev is Keyboard kb && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)) return true;
            if (dev is Gamepad pad && (pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)) return true;
        }
        return false;
    }

    private void UpdateDetailText()
    {
        if (p1DetailText == null || p1CursorIdx < 0 || p1CursorIdx >= characterDataList.Count) return;

        var c = characterDataList[p1CursorIdx];
        var a = (p1CursorIdx < assistantDataList.Count) ? assistantDataList[p1CursorIdx] : null;

        string colorTag = "<color=#9999FF>";
        const string end = "</color>";

        string desc = $"{colorTag}オプション：{c.optionName}{end}\n{c.optionDescription}\n" +
                      $"{colorTag}スペシャル：{c.specialAttackName}{end}\n{c.specialAttackDescription}\n" +
                      $"{colorTag}エンゲージ：{c.engageAttackName}{end}\n{c.engageAttackDescription}";

        if (a != null)
        {
            desc += $"\n\n{colorTag}サポート：{a.supportSkillName}{end}\n{a.supportDescription}\n" +
                    $"{colorTag}ジャマー：{a.jammerSkillName}{end}\n{a.jammerDescription}\n" +
                    $"{colorTag}コラボ：{a.engageSkillName}{end}\n{a.engageDescription}";
        }

        p1DetailText.text = desc;
    }

    private void SlidePortrait(int slot, int charIdx)
    {
        Image[] ports = GetPortraits();
        Image img = ports[slot];
        if (img == null) return;

        img.gameObject.SetActive(true);
        if (charIdx >= 0 && charIdx < characterPortraits.Count) img.sprite = characterPortraits[charIdx];

        if (portraitMotions[slot].IsActive()) portraitMotions[slot].Cancel();

        float basePosX = basePortraitPos[slot].x;
        float startX = basePosX - slideOffset;

        img.rectTransform.anchoredPosition = new Vector2(startX, basePortraitPos[slot].y);
        CanvasGroup cg = portraitCgs[slot];
        if (cg != null) cg.alpha = 0.3f;

        portraitMotions[slot] = LMotion.Create(startX, basePosX, 0.2f)
            .WithEase(Ease.OutCubic)
            .Bind(x => {
                if (img != null)
                {
                    img.rectTransform.anchoredPosition = new Vector2(x, basePortraitPos[slot].y);
                    if (cg != null) cg.alpha = Mathf.InverseLerp(startX, basePosX, x);
                }
            });
    }

    private void SetDummyPortrait(int slot)
    {
        Image[] ports = GetPortraits();
        Image img = ports[slot];
        if (img == null) return;

        if (portraitMotions[slot].IsActive()) portraitMotions[slot].Cancel();
        img.gameObject.SetActive(true);
        img.sprite = dummyPortraitSprite;
        img.rectTransform.anchoredPosition = basePortraitPos[slot];

        CanvasGroup cg = portraitCgs[slot];
        if (cg != null) cg.alpha = 0.5f;
    }

    public void OpenPanel(Action onBack)
    {
        onBackCallback = onBack;
        isOpen = true;
        isStarting = p1DetailOpen = p1IsHolding = false;
        p1NavInput = Vector2.zero;

        step = Step.P1_Main;
        p1CursorIdx = 0;

        AssignDevices();
        p1Controls.UI.Enable();

        var bsd = BattleSettingData.Instance;
        if (bsd != null)
        {
            bsd.ResetArcadeData(); // 一旦クリア
            bsd.CurrentMatchType = MatchType.HumanVsCPU;
            bsd.IsArcadeMode = true; // 明示的にアーケードモードフラグON
            bsd.P1Data.isCPU = false;
            bsd.P2Data.isCPU = true;
        }

        if (startBattleButton != null) startBattleButton.interactable = false;
        p1Cursor?.gameObject.SetActive(true);

        SetDummyPortrait(0);
        SetDummyPortrait(1);

        gameObject.SetActive(true);
        LMotion.Create(panelRect.anchoredPosition.x, 0f, 0.28f).WithEase(Ease.OutCubic)
            .Bind(x => panelRect.anchoredPosition = new Vector2(x, panelRect.anchoredPosition.y));

        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        Canvas.ForceUpdateCanvases();
        yield return null;
        UpdateCursors();
        SlidePortrait(0, p1CursorIdx);
        UpdateInfoUI();
        UpdateRankingDisplay();
    }

    public void ClosePanel()
    {
        if (!isOpen) return;
        isOpen = false;
        p1Controls.UI.Disable();
        LMotion.Create(panelRect.anchoredPosition.x, 1920f, 0.28f).WithEase(Ease.InCubic)
            .Bind(x => panelRect.anchoredPosition = new Vector2(x, panelRect.anchoredPosition.y));

        StartCoroutine(DelayedClose());
    }

    private IEnumerator DelayedClose()
    {
        yield return new WaitForSeconds(0.28f);
        gameObject.SetActive(false);
        onBackCallback?.Invoke();
    }

    private void OnP1Navigate(Vector2 dir)
    {
        if (!isOpen || p1DetailOpen) return;

        if (step != Step.ReadyToStart)
        {
            int prev = p1CursorIdx;
            p1CursorIdx = CalcGrid(p1CursorIdx, dir);
            if (prev != p1CursorIdx)
            {
                SoundManager.Instance?.PlayUIMoveSE();
                UpdateCursors();
                int slot = (step == Step.P1_Main) ? 0 : 1;
                SlidePortrait(slot, p1CursorIdx);
                if (p1DetailOpen) UpdateDetailText();
                UpdateInfoUI();
                UpdateRankingDisplay();
            }
        }
    }

    private void OnP1Submit()
    {
        if (!isOpen) return;
        if (p1DetailOpen) { p1DetailOpen = false; p1DetailPanel?.SetActive(false); return; }
        SoundManager.Instance?.PlayUISelectSE();

        if (step == Step.ReadyToStart)
        {
            if (isStarting) return;
            isStarting = true;

            var bsd = BattleSettingData.Instance;
            if (bsd != null)
            {
                bsd.IsConfigured = true;
                // ★ 1P決定時、事前に3戦分（ステージ1〜3）の敵（主役・助手）を一括確定・保存する
                bsd.GenerateArcadeOpponents(bsd.P1Data.mainCharIndex, characterDataList.Count);
            }

            var fieldInfo = typeof(Initiate).GetField("areWeFading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            fieldInfo?.SetValue(null, false);

            Initiate.Fade(battleSceneName, Color.black, 1.0f);
            return;
        }

        var bsdData = BattleSettingData.Instance;
        if (charButtons.Count == 0) return;

        if (step == Step.P1_Main)
        {
            if (bsdData != null) bsdData.P1Data.mainCharIndex = p1CursorIdx;
            step = Step.P1_Assist;
            AdvanceCursor(ref p1CursorIdx, 1);
        }
        else if (step == Step.P1_Assist && (bsdData == null || p1CursorIdx != bsdData.P1Data.mainCharIndex))
        {
            if (bsdData != null) bsdData.P1Data.assistCharIndex = p1CursorIdx;
            step = Step.ReadyToStart;
            ReadyStart();
        }

        UpdateCursors();
        UpdateInfoUI();
        UpdateRankingDisplay();
    }

    private void OnP1Cancel()
    {
        if (!isOpen) return;
        if (p1DetailOpen) { p1DetailOpen = false; p1DetailPanel?.SetActive(false); return; }
        SoundManager.Instance?.PlayUICancelSE();

        var bsd = BattleSettingData.Instance;
        if (step == Step.ReadyToStart)
        {
            if (startBattleButton != null) startBattleButton.interactable = false;
            step = Step.P1_Assist;
            if (bsd != null) p1CursorIdx = bsd.P1Data.assistCharIndex;
            SlidePortrait(1, p1CursorIdx);
        }
        else if (step == Step.P1_Assist)
        {
            SetDummyPortrait(1);
            step = Step.P1_Main;
            if (bsd != null) p1CursorIdx = bsd.P1Data.mainCharIndex;
            SlidePortrait(0, p1CursorIdx);
        }
        else if (step == Step.P1_Main)
        {
            ClosePanel();
        }

        UpdateCursors();
        UpdateInfoUI();
        UpdateRankingDisplay();
    }

    private void AdvanceCursor(ref int cur, int slot)
    {
        cur = (cur + 1) % charButtons.Count;
        SlidePortrait(slot, cur);
    }

    private int CalcGrid(int cur, Vector2 dir)
    {
        int cnt = charButtons.Count;
        if (cnt <= 0) return 0;
        if (dir.x > 0.5f && cur % columns < columns - 1 && cur + 1 < cnt) return cur + 1;
        if (dir.x < -0.5f && cur % columns > 0) return cur - 1;
        if (dir.y < -0.5f && cur + columns < cnt) return cur + columns;
        if (dir.y > 0.5f && cur - columns >= 0) return cur - columns;
        return cur;
    }

    private void ReadyStart()
    {
        if (startBattleButton != null) { startBattleButton.interactable = true; startBattleButton.Select(); }
        if (p1Cursor != null && startBattleButton != null) { p1Cursor.gameObject.SetActive(true); p1Cursor.position = startBattleButton.transform.position; }
        UpdateInfoUI();
    }

    private void UpdateCursors()
    {
        if (step == Step.ReadyToStart) return;

        if (p1Cursor != null && charButtons.Count > p1CursorIdx && charButtons[p1CursorIdx] != null)
        {
            p1Cursor.gameObject.SetActive(true);
            p1Cursor.position = charButtons[p1CursorIdx].transform.position;
        }
    }

    private void UpdateInfoUI()
    {
        var b = BattleSettingData.Instance;
        Func<int, string> getName = idx => (idx >= 0 && idx < characterDataList.Count && characterDataList[idx] != null) ? characterDataList[idx].characterName : "???";
        Func<int, string> getDesc = idx => (idx >= 0 && idx < characterDataList.Count && characterDataList[idx] != null) ? characterDataList[idx].characterDescription : "";

        string p1Main = (step == Step.P1_Main) ? getName(p1CursorIdx) : getName(b.P1Data.mainCharIndex);
        string p1Assist = (step == Step.P1_Main) ? "???" : ((step == Step.P1_Assist) ? getName(p1CursorIdx) : getName(b.P1Data.assistCharIndex));
        if (p1SelectionText != null) p1SelectionText.text = $"{p1Main} &\n{p1Assist}";

        if (p1DescriptionText != null)
        {
            p1DescriptionText.text = getDesc(p1CursorIdx);
        }
    }

    /// <summary>
    /// 現在ターゲットしている主役キャラのクリアタイムランキング(上位5位)を表示更新
    /// </summary>
    private void UpdateRankingDisplay()
    {
        var b = BattleSettingData.Instance;
        int targetMainIdx = (step == Step.P1_Main) ? p1CursorIdx : (b != null ? b.P1Data.mainCharIndex : p1CursorIdx);

        if (targetMainIdx >= 0 && targetMainIdx < characterDataList.Count && characterDataList[targetMainIdx] != null)
        {
            string charName = characterDataList[targetMainIdx].characterName;
            if (rankingTitleText != null)
            {
                rankingTitleText.text = $"<color=#FFD700>HIGH SCORE</color>";
            }

            string rankingStr = "";
            for (int rank = 1; rank <= 5; rank++)
            {
                float timeVal = ArcadeScoreManager.GetBestTime(targetMainIdx, rank);
                string formattedTime = ArcadeScoreManager.FormatTime(timeVal);

                string rankColor = rank switch
                {
                    1 => "<color=#FFD700>", // 金
                    2 => "<color=#C0C0C0>", // 銀
                    3 => "<color=#CD7F32>", // 銅
                    _ => "<color=#FFFFFF>"
                };

                if(rank == 1)rankingStr += $"{rankColor}{rank}st  {formattedTime}</color>\n";
                else if(rank == 2)rankingStr += $"{rankColor}{rank}nd  {formattedTime}</color>\n";
                else if(rank == 3)rankingStr += $"{rankColor}{rank}rd  {formattedTime}</color>\n";
                else rankingStr += $"{rankColor}{rank}th  {formattedTime}</color>\n";
            }

            if (rankingContentText != null)
            {
                rankingContentText.text = rankingStr;
            }
        }
    }

    private void DisableNavigations()
    {
        Action<Selectable> dis = s => {
            if (s == null) return;
            Navigation nav = s.navigation; nav.mode = Navigation.Mode.None; s.navigation = nav;
            foreach (var g in s.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
        };
        charButtons.ForEach(dis);
    }

    private void BindInputs()
    {
        p1Controls.UI.Navigate.performed += ctx => p1NavInput = ctx.ReadValue<Vector2>();
        p1Controls.UI.Navigate.canceled += _ => p1NavInput = Vector2.zero;
        p1Controls.UI.Submit.performed += _ => OnP1Submit();
        p1Controls.UI.Cancel.performed += _ => OnP1Cancel();
    }

    private void AssignDevices()
    {
        var pads = Gamepad.all;
        var p1D = new List<InputDevice>();
        if (Keyboard.current != null) p1D.Add(Keyboard.current);
        if (pads.Count >= 1) p1D.Add(pads[0]);

        p1Controls.devices = p1D.ToArray();
    }
}