using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using LitMotion;

public class CharSelectPanel : MonoBehaviour
{
    private enum Step { P1_Main, P1_Assist, P2_Main, P2_Assist, CPUDifficulty, ReadyToStart }

    [Header("パネル & ボタン")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private List<Button> charButtons = new();
    [SerializeField] private int columns = 2;

    [Header("SO データ参照")]
    [SerializeField] private List<CharacterDataSO> characterDataList = new();
    [SerializeField] private List<AssistantDataSO> assistantDataList = new();

    [Header("立ち絵 UI (0:1P主, 1:1P助, 2:2P主, 3:2P助)")]
    [SerializeField] private Image p1MainPortrait;
    [SerializeField] private Image p1AssistPortrait;
    [SerializeField] private Image p2MainPortrait;
    [SerializeField] private Image p2AssistPortrait;
    [SerializeField] private List<Sprite> characterPortraits = new();
    [SerializeField] private Sprite dummyPortraitSprite;
    [SerializeField] private float slideOffset = 60f;

    [Header("詳細ポップアップ UI (Shift / L・R)")]
    [SerializeField] private GameObject p1DetailPanel;
    [SerializeField] private TextMeshProUGUI p1DetailText;
    [SerializeField] private GameObject p2DetailPanel;
    [SerializeField] private TextMeshProUGUI p2DetailText;

    [Header("カーソル & テキスト UI")]
    [SerializeField] private RectTransform p1Cursor;
    [SerializeField] private RectTransform p2Cursor;
    [SerializeField] private TextMeshProUGUI p1SelectionText;
    [SerializeField] private TextMeshProUGUI p2SelectionText;
    [SerializeField] private TextMeshProUGUI p1DescriptionText;
    [SerializeField] private TextMeshProUGUI p2DescriptionText;

    [Header("難易度 & 開始ボタン")]
    [SerializeField] private GameObject cpuDifficultyGroup;
    [SerializeField] private List<Button> difficultyButtons = new();
    [SerializeField] private Button startBattleButton;
    [SerializeField] private string battleSceneName = "Game";
    [SerializeField] private TextMeshProUGUI handicapStatusText;

    private MatchType matchType;
    private Action onBackCallback;
    private Step step = Step.P1_Main;
    private bool isOpen, p1PvPDone, p2PvPDone, p2MainDone, p1DetailOpen, p2DetailOpen, isStarting;
    private int p1CursorIdx, p2CursorIdx = 1, cpuDiffIdx;

    private UIControls p1Controls, p2Controls;
    private readonly Vector2[] basePortraitPos = new Vector2[4];
    private readonly CanvasGroup[] portraitCgs = new CanvasGroup[4];
    private readonly MotionHandle[] portraitMotions = new MotionHandle[4];

    private Vector2 p1NavInput, p2NavInput;
    private bool p1IsHolding, p2IsHolding;
    private float p1NextRepeat, p2NextRepeat;

    private const float DeadZone = 0.5f;
    private const float InitialRepeatDelay = 0.25f;
    private const float RepeatInterval = 0.12f;

    private Image[] GetPortraits() => new[] { p1MainPortrait, p1AssistPortrait, p2MainPortrait, p2AssistPortrait };

    private void Awake()
    {
        p1Controls = new UIControls();
        p2Controls = new UIControls();
        BindInputs();
        DisableNavigations();

        Image[] ports = GetPortraits();
        for (int i = 0; i < 4; i++)
        {
            if (ports[i] == null) continue;
            basePortraitPos[i] = ports[i].rectTransform.anchoredPosition;
            if (!ports[i].TryGetComponent(out portraitCgs[i]))
                portraitCgs[i] = ports[i].gameObject.AddComponent<CanvasGroup>();
        }

        if (panelRect != null) panelRect.anchoredPosition = new Vector2(1920f, panelRect.anchoredPosition.y);
        p1DetailPanel?.SetActive(false);
        p2DetailPanel?.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnDestroy() { p1Controls?.Dispose(); p2Controls?.Dispose(); }

    private void Update()
    {
        if (!isOpen) return;
        CheckHandicapInput();

        ProcessStickInput(p1NavInput, ref p1IsHolding, ref p1NextRepeat, isP1: true);
        if (matchType == MatchType.HumanVsHuman)
            ProcessStickInput(p2NavInput, ref p2IsHolding, ref p2NextRepeat, isP1: false);

        CheckDetailToggle(isP1: true);
        if (matchType == MatchType.HumanVsHuman) CheckDetailToggle(isP1: false);
    }

    private void ProcessStickInput(Vector2 input, ref bool isHolding, ref float nextRepeat, bool isP1)
    {
        if (input.magnitude < DeadZone) { isHolding = false; return; }

        if (!isHolding || Time.unscaledTime >= nextRepeat)
        {
            Vector2 cardinalDir = (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                ? new Vector2(Mathf.Sign(input.x), 0f)
                : new Vector2(0f, Mathf.Sign(input.y));

            if (isP1) OnP1Navigate(cardinalDir);
            else OnP2Navigate(cardinalDir);

            nextRepeat = Time.unscaledTime + (isHolding ? RepeatInterval : InitialRepeatDelay);
            isHolding = true;
        }
    }

    // --- 割り当てデバイスごとの正確な詳細切り替え判定 ---
    private void CheckDetailToggle(bool isP1)
    {
        if (!IsDetailPressed(isP1)) return;

        if (isP1) { p1DetailOpen = !p1DetailOpen; p1DetailPanel?.SetActive(p1DetailOpen); }
        else { p2DetailOpen = !p2DetailOpen; p2DetailPanel?.SetActive(p2DetailOpen); }

        UpdateDetailText(isP1);
    }

    private bool IsDetailPressed(bool isP1)
    {
        var devices = isP1 ? p1Controls.devices : p2Controls.devices;
        if (devices == null) return false;

        foreach (var dev in devices)
        {
            if (dev is Keyboard kb && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)) return true;
            if (dev is Gamepad pad && (pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)) return true;
        }
        return false;
    }

    // --- 詳細テキスト作成 (1P:青 / 2P:赤) ---
    private void UpdateDetailText(bool isP1)
    {
        TextMeshProUGUI tmp = isP1 ? p1DetailText : p2DetailText;
        int idx = isP1 ? p1CursorIdx : p2CursorIdx;
        if (tmp == null || idx < 0 || idx >= characterDataList.Count) return;

        var c = characterDataList[idx];
        var a = (idx < assistantDataList.Count) ? assistantDataList[idx] : null;

        string colorTag = isP1 ? "<color=#9999FF>" : "<color=#FF9999>";
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

        tmp.text = desc;
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
        float startX = (slot < 2) ? basePosX - slideOffset : basePosX + slideOffset;

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

    public void OpenPanel(MatchType type, Action onBack)
    {
        matchType = type;
        onBackCallback = onBack;
        isOpen = true;
        isStarting = p1DetailOpen = p2DetailOpen = p1PvPDone = p2PvPDone = p2MainDone = p1IsHolding = p2IsHolding = false;
        p1NavInput = p2NavInput = Vector2.zero;

        step = Step.P1_Main;
        p1CursorIdx = 0; p2CursorIdx = Mathf.Min(1, charButtons.Count - 1); cpuDiffIdx = 0;

        AssignDevices();
        p1Controls.UI.Enable();
        if (matchType == MatchType.HumanVsHuman && Gamepad.all.Count > 0) p2Controls.UI.Enable();
        else p2Controls.UI.Disable();

        var bsd = BattleSettingData.Instance;
        if (bsd != null)
        {
            bsd.ResetArcadeData(); // ★ 確実にアーケードフラグを解除(false)にする
            bsd.ResetHandicaps();
            bsd.CurrentMatchType = type;
            bsd.P1Data.isCPU = (type == MatchType.CPUVsCPU);
            bsd.P2Data.isCPU = (type != MatchType.HumanVsHuman);
        }

        cpuDifficultyGroup?.SetActive(false);
        if (startBattleButton != null) startBattleButton.interactable = false;
        p1Cursor?.gameObject.SetActive(true);
        p2Cursor?.gameObject.SetActive(matchType == MatchType.HumanVsHuman);

        for (int i = 0; i < 4; i++) SetDummyPortrait(i);

        gameObject.SetActive(true);
        LMotion.Create(panelRect.anchoredPosition.x, 0f, 0.28f).WithEase(Ease.OutCubic)
            .Bind(x => panelRect.anchoredPosition = new Vector2(x, panelRect.anchoredPosition.y));

        StartCoroutine(InitRoutine());
        UpdateHandicapTextUI();
    }

    private IEnumerator InitRoutine()
    {
        Canvas.ForceUpdateCanvases();
        yield return null;
        UpdateCursors();
        SlidePortrait(0, p1CursorIdx);
        if (matchType == MatchType.HumanVsHuman) SlidePortrait(2, p2CursorIdx);
        UpdateInfoUI();
    }

    public void ClosePanel()
    {
        if (!isOpen) return;
        isOpen = false;
        p1Controls.UI.Disable(); p2Controls.UI.Disable();
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
        SoundManager.Instance?.PlayUIMoveSE();

        if (step == Step.CPUDifficulty && difficultyButtons.Count > 0)
        {
            if (dir.x > 0.5f && cpuDiffIdx < difficultyButtons.Count - 1) cpuDiffIdx++;
            else if (dir.x < -0.5f && cpuDiffIdx > 0) cpuDiffIdx--;
            UpdateDifficultyCursor();
        }
        else if (step != Step.ReadyToStart)
        {
            int prev = p1CursorIdx;
            p1CursorIdx = CalcGrid(p1CursorIdx, dir);
            if (prev != p1CursorIdx)
            {
                UpdateCursors();
                int slot = (matchType != MatchType.HumanVsHuman && step >= Step.P2_Main) ? (step == Step.P2_Main ? 2 : 3) : (step == Step.P1_Main ? 0 : 1);
                SlidePortrait(slot, p1CursorIdx);
                if (p1DetailOpen) UpdateDetailText(true);
                UpdateInfoUI();
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

            if (BattleSettingData.Instance != null) BattleSettingData.Instance.IsConfigured = true;

            var fieldInfo = typeof(Initiate).GetField("areWeFading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            fieldInfo?.SetValue(null, false);

            Initiate.Fade(battleSceneName, Color.black, 1.0f);
            return;
        }

        var bsd = BattleSettingData.Instance;
        if (step == Step.CPUDifficulty)
        {
            bsd.P2Data.cpuDifficulty = (CPUDifficulty)cpuDiffIdx;
            if (matchType == MatchType.CPUVsCPU) bsd.P1Data.cpuDifficulty = (CPUDifficulty)cpuDiffIdx;
            cpuDifficultyGroup?.SetActive(false);
            step = Step.ReadyToStart;
            ReadyStart();
            return;
        }

        if (charButtons.Count == 0) return;

        if (matchType == MatchType.HumanVsHuman)
        {
            if (step == Step.P1_Main) { bsd.P1Data.mainCharIndex = p1CursorIdx; step = Step.P1_Assist; AdvanceCursor(ref p1CursorIdx, 1); }
            else if (step == Step.P1_Assist && p1CursorIdx != bsd.P1Data.mainCharIndex) { bsd.P1Data.assistCharIndex = p1CursorIdx; p1PvPDone = true; CheckBothPvP(); }
        }
        else
        {
            if (step == Step.P1_Main) { bsd.P1Data.mainCharIndex = p1CursorIdx; step = Step.P1_Assist; AdvanceCursor(ref p1CursorIdx, 1); }
            else if (step == Step.P1_Assist && p1CursorIdx != bsd.P1Data.mainCharIndex) { bsd.P1Data.assistCharIndex = p1CursorIdx; step = Step.P2_Main; p1CursorIdx = 0; SlidePortrait(2, 0); }
            else if (step == Step.P2_Main) { bsd.P2Data.mainCharIndex = p1CursorIdx; step = Step.P2_Assist; AdvanceCursor(ref p1CursorIdx, 3); }
            else if (step == Step.P2_Assist && p1CursorIdx != bsd.P2Data.mainCharIndex)
            {
                bsd.P2Data.assistCharIndex = p1CursorIdx;
                step = Step.CPUDifficulty;
                cpuDifficultyGroup?.SetActive(true);
                UpdateDifficultyCursor();
            }
        }
        UpdateCursors();
        UpdateInfoUI();
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
            if (matchType == MatchType.HumanVsHuman) { p1PvPDone = false; step = Step.P1_Assist; p1CursorIdx = bsd.P1Data.assistCharIndex; SlidePortrait(1, p1CursorIdx); }
            else { step = Step.CPUDifficulty; cpuDifficultyGroup?.SetActive(true); UpdateDifficultyCursor(); }
        }
        else if (step == Step.CPUDifficulty) { cpuDifficultyGroup?.SetActive(false); step = Step.P2_Assist; p1CursorIdx = bsd.P2Data.assistCharIndex; SlidePortrait(3, p1CursorIdx); }
        else if (step == Step.P2_Assist) { SetDummyPortrait(3); step = Step.P2_Main; p1CursorIdx = bsd.P2Data.mainCharIndex; SlidePortrait(2, p1CursorIdx); }
        else if (step == Step.P2_Main) { SetDummyPortrait(2); SetDummyPortrait(3); step = Step.P1_Assist; p1CursorIdx = bsd.P1Data.assistCharIndex; SlidePortrait(1, p1CursorIdx); }
        else if (step == Step.P1_Assist) { SetDummyPortrait(1); step = Step.P1_Main; p1CursorIdx = bsd.P1Data.mainCharIndex; SlidePortrait(0, p1CursorIdx); }
        else if (step == Step.P1_Main) ClosePanel();

        UpdateCursors();
        UpdateInfoUI();
    }

    private void OnP2Navigate(Vector2 dir)
    {
        if (!isOpen || p2DetailOpen || matchType != MatchType.HumanVsHuman || p2PvPDone) return;
        SoundManager.Instance?.PlayUIMoveSE();
        
        int prev = p2CursorIdx;
        p2CursorIdx = CalcGrid(p2CursorIdx, dir);
        if (prev != p2CursorIdx)
        {
            UpdateCursors();
            SlidePortrait(p2MainDone ? 3 : 2, p2CursorIdx);
            if (p2DetailOpen) UpdateDetailText(false);
            UpdateInfoUI();
        }
    }

    private void OnP2Submit()
    {
        if (!isOpen || matchType != MatchType.HumanVsHuman) return;
        if (p2DetailOpen) { p2DetailOpen = false; p2DetailPanel?.SetActive(false); return; }
        SoundManager.Instance?.PlayUISelectSE();

        var bsd = BattleSettingData.Instance;
        if (!p2MainDone) { bsd.P2Data.mainCharIndex = p2CursorIdx; p2MainDone = true; AdvanceCursor(ref p2CursorIdx, 3); }
        else if (!p2PvPDone && p2CursorIdx != bsd.P2Data.mainCharIndex) { bsd.P2Data.assistCharIndex = p2CursorIdx; p2PvPDone = true; CheckBothPvP(); }
        UpdateCursors();
        UpdateInfoUI();
    }

    private void OnP2Cancel()
    {
        if (!isOpen || matchType != MatchType.HumanVsHuman) return;
        if (p2DetailOpen) { p2DetailOpen = false; p2DetailPanel?.SetActive(false); return; }
        SoundManager.Instance?.PlayUICancelSE();

        var bsd = BattleSettingData.Instance;
        if (step == Step.ReadyToStart) { if (startBattleButton != null) startBattleButton.interactable = false; p2PvPDone = false; }
        else if (p2PvPDone) { p2PvPDone = false; p2CursorIdx = bsd.P2Data.assistCharIndex; SlidePortrait(3, p2CursorIdx); }
        else if (p2MainDone) { SetDummyPortrait(3); p2MainDone = false; p2CursorIdx = bsd.P2Data.mainCharIndex; SlidePortrait(2, p2CursorIdx); }
        UpdateCursors();
        UpdateInfoUI();
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

    private void CheckBothPvP() { if (p1PvPDone && p2PvPDone) { step = Step.ReadyToStart; ReadyStart(); } }

    private void ReadyStart()
    {
        if (startBattleButton != null) { startBattleButton.interactable = true; startBattleButton.Select(); }
        if (p1Cursor != null && startBattleButton != null) { p1Cursor.gameObject.SetActive(true); p1Cursor.position = startBattleButton.transform.position; }
        UpdateInfoUI();
    }

    private void UpdateDifficultyCursor()
    {
        if (difficultyButtons.Count > cpuDiffIdx && difficultyButtons[cpuDiffIdx] != null)
        {
            difficultyButtons[cpuDiffIdx].Select();
            if (p1Cursor != null) { p1Cursor.gameObject.SetActive(true); p1Cursor.position = difficultyButtons[cpuDiffIdx].transform.position; }
        }
    }

    private void UpdateCursors()
    {
        if (step == Step.ReadyToStart) return;
        if (step == Step.CPUDifficulty) { UpdateDifficultyCursor(); return; }

        if (p1Cursor != null && charButtons.Count > p1CursorIdx && charButtons[p1CursorIdx] != null)
        {
            p1Cursor.gameObject.SetActive(true);
            p1Cursor.position = charButtons[p1CursorIdx].transform.position;
        }
        if (p2Cursor != null && charButtons.Count > p2CursorIdx && charButtons[p2CursorIdx] != null && matchType == MatchType.HumanVsHuman)
        {
            p2Cursor.position = charButtons[p2CursorIdx].transform.position;
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
            int p1ActiveIdx = (matchType != MatchType.HumanVsHuman && step >= Step.P2_Main) ? b.P1Data.mainCharIndex : p1CursorIdx;
            p1DescriptionText.text = getDesc(p1ActiveIdx);
        }

        string p2Main = "???";
        string p2Assist = "???";
        int p2ActiveIdx = -1;

        if (matchType == MatchType.HumanVsHuman)
        {
            p2Main = p2MainDone ? getName(b.P2Data.mainCharIndex) : getName(p2CursorIdx);
            p2Assist = p2MainDone ? (p2PvPDone ? getName(b.P2Data.assistCharIndex) : getName(p2CursorIdx)) : "???";
            p2ActiveIdx = p2CursorIdx;
        }
        else
        {
            if (step >= Step.P2_Main) p2Main = (step == Step.P2_Main) ? getName(p1CursorIdx) : getName(b.P2Data.mainCharIndex);
            if (step >= Step.P2_Assist) { p2Assist = (step == Step.P2_Assist) ? getName(p1CursorIdx) : getName(b.P2Data.assistCharIndex); p2ActiveIdx = p1CursorIdx; }
            if (step == Step.P2_Main) p2ActiveIdx = p1CursorIdx;
        }

        if (p2SelectionText != null) p2SelectionText.text = $"{p2Main} &\n{p2Assist}";
        if (p2DescriptionText != null) p2DescriptionText.text = (p2ActiveIdx >= 0) ? getDesc(p2ActiveIdx) : "";
    }

    private void DisableNavigations()
    {
        Action<Selectable> dis = s => {
            if (s == null) return;
            Navigation nav = s.navigation; nav.mode = Navigation.Mode.None; s.navigation = nav;
            foreach (var g in s.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
        };
        charButtons.ForEach(dis);
        difficultyButtons.ForEach(dis);
    }

    private void BindInputs()
    {
        p1Controls.UI.Navigate.performed += ctx => p1NavInput = ctx.ReadValue<Vector2>();
        p1Controls.UI.Navigate.canceled += _ => p1NavInput = Vector2.zero;
        p1Controls.UI.Submit.performed += _ => OnP1Submit();
        p1Controls.UI.Cancel.performed += _ => OnP1Cancel();

        p2Controls.UI.Navigate.performed += ctx => p2NavInput = ctx.ReadValue<Vector2>();
        p2Controls.UI.Navigate.canceled += _ => p2NavInput = Vector2.zero;
        p2Controls.UI.Submit.performed += _ => OnP2Submit();
        p2Controls.UI.Cancel.performed += _ => OnP2Cancel();
    }

    private void AssignDevices()
    {
        var pads = Gamepad.all;
        var p1D = new List<InputDevice>();
        var p2D = new List<InputDevice>();
        if (Keyboard.current != null) p1D.Add(Keyboard.current);
        if (pads.Count >= 2) { p1D.Add(pads[0]); p2D.Add(pads[1]); }
        else if (pads.Count == 1) { if (matchType == MatchType.HumanVsHuman) p2D.Add(pads[0]); else p1D.Add(pads[0]); }

        p1Controls.devices = p1D.ToArray();
        p2Controls.devices = p2D.Count > 0 ? p2D.ToArray() : Array.Empty<InputDevice>();
    }

    private void CheckHandicapInput()
    {
        if (Keyboard.current == null) return;

        var bsd = BattleSettingData.Instance;
        if (bsd == null) return;

        bool updated = false;

        // 「1」キー押下で 1P ハンデ切替
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            bsd.P1Handicap = !bsd.P1Handicap;
            updated = true;
            SoundManager.Instance?.PlayUIMoveSE();
        }

        // 「2」キー押下で 2P ハンデ切替
        if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            bsd.P2Handicap = !bsd.P2Handicap;
            updated = true;
            SoundManager.Instance?.PlayUIMoveSE();
        }

        if (updated)
        {
            UpdateHandicapTextUI();
        }
    }

    private void UpdateHandicapTextUI()
    {
        if (handicapStatusText == null) return;

        var bsd = BattleSettingData.Instance;
        if (bsd == null)
        {
            handicapStatusText.text = "1x2x";
            return;
        }

        string p1Mark = bsd.P1Handicap ? "o" : "x";
        string p2Mark = bsd.P2Handicap ? "o" : "x";
        handicapStatusText.text = $"1{p1Mark}2{p2Mark}";
    }
}