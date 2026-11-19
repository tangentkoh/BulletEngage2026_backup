using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using LitMotion;
using LitMotion.Extensions;

public class OneMatchModeSelectPanel : MonoBehaviour, UIControls.IUIActions
{
    [Header("パネル RectTransform")]
    [SerializeField] private RectTransform panelRect;

    [Header("モード選択ボタン (横並び 3つ)")]
    [Tooltip("左から HumanVsHuman, HumanVsCPU, CPUVsCPU の順")]
    [SerializeField] private List<Button> modeButtons;

    [Header("スライド設定")]
    [SerializeField] private float hiddenPosX = 1200f;
    [SerializeField] private float showPosX = 0f;
    [SerializeField] private float slideDuration = 0.28f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;
    [SerializeField] private CharSelectPanel charSelectPanel;

    [Header("入力クールダウン設定")]
    [SerializeField] private float inputRepeatDelay = 0.22f; // 入力の過剰反応防止インターバル

    private UIControls controls;
    private int currentIndex = 1; // 初期値: 人間 vs CPU
    private float nextInputTime = 0f;
    private Action onClosedCallback;
    private bool isOpen = false;
    private MotionHandle activeSlideMotion;

    private void Awake()
    {
        controls = new UIControls();
        controls.UI.SetCallbacks(this);

        // Unity標準のUI自動移動を無効化 (二重移動を阻止)
        DisableButtonNavigations();

        if (panelRect != null)
        {
            panelRect.anchoredPosition = new Vector2(hiddenPosX, panelRect.anchoredPosition.y);
        }
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        controls?.Dispose();
    }

    private void DisableButtonNavigations()
    {
        if (modeButtons == null) return;
        foreach (var btn in modeButtons)
        {
            if (btn == null) continue;

            // 1. 標準ナビゲーションをOFFにしてスクリプト制御に一本化
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            // 2. マウスの誤ホバー・誤クリック防止
            var graphics = btn.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                g.raycastTarget = false;
            }
        }
    }

    public void OpenPanel(Action onClose)
    {
        onClosedCallback = onClose;
        gameObject.SetActive(true);
        isOpen = true;
        currentIndex = 1; // 初期選択: 人間 vs CPU
        nextInputTime = Time.unscaledTime + 0.15f; // 開いた直後の誤入力防止

        controls.UI.Enable();

        if (activeSlideMotion.IsActive()) activeSlideMotion.Cancel();

        activeSlideMotion = LMotion.Create(panelRect.anchoredPosition.x, showPosX, slideDuration)
            .WithEase(slideEase)
            .Bind(x =>
            {
                if (panelRect != null)
                    panelRect.anchoredPosition = new Vector2(x, panelRect.anchoredPosition.y);
            });

        HighlightCurrentButton();
    }

    public void ClosePanelToMain()
    {
        if (!isOpen) return;
        isOpen = false;
        controls.UI.Disable();

        if (activeSlideMotion.IsActive()) activeSlideMotion.Cancel();

        activeSlideMotion = LMotion.Create(panelRect.anchoredPosition.x, hiddenPosX, slideDuration)
            .WithEase(Ease.InCubic)
            .Bind(x =>
            {
                if (panelRect != null)
                    panelRect.anchoredPosition = new Vector2(x, panelRect.anchoredPosition.y);
            });

        DOVirtualDelay(slideDuration, () =>
        {
            gameObject.SetActive(false);
            onClosedCallback?.Invoke();
        });
    }

    private void ClosePanelForNext()
    {
        if (!isOpen) return;
        isOpen = false;
        controls.UI.Disable();

        if (activeSlideMotion.IsActive()) activeSlideMotion.Cancel();

        if (panelRect != null)
        {
            panelRect.anchoredPosition = new Vector2(hiddenPosX, panelRect.anchoredPosition.y);
        }
        gameObject.SetActive(false);
    }

    private void HighlightCurrentButton()
    {
        if (modeButtons != null && currentIndex >= 0 && currentIndex < modeButtons.Count)
        {
            modeButtons[currentIndex].Select();
        }
    }

    // --- UIControls コールバック ---

    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;

        // クールダウン経過前なら無視して連続入力を防止
        if (Time.unscaledTime < nextInputTime) return;

        Vector2 dir = context.ReadValue<Vector2>();

        // 左右キー/スティック判定 (1入力につき厳格に1つだけ移動)
        if (dir.x < -0.5f && currentIndex > 0)
        {
            currentIndex--;
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
            SoundManager.Instance?.PlayUIMoveSE();
            HighlightCurrentButton();
        }
        else if (dir.x > 0.5f && currentIndex < modeButtons.Count - 1)
        {
            currentIndex++;
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
            SoundManager.Instance?.PlayUIMoveSE();
            HighlightCurrentButton();
        }
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;

        MatchType selected = (MatchType)currentIndex;
        SoundManager.Instance?.PlayUISelectSE();
        ClosePanelForNext();

        if (charSelectPanel != null)
        {
            charSelectPanel.OpenPanel(selected, () =>
            {
                OpenPanel(onClosedCallback);
            });
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;
        SoundManager.Instance?.PlayUICancelSE();
        ClosePanelToMain();
    }

    private void DOVirtualDelay(float delay, Action onComplete)
    {
        StartCoroutine(DelayRoutine(delay, onComplete));
    }

    private System.Collections.IEnumerator DelayRoutine(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }
}