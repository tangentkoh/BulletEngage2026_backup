using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 追加: Select処理用
using UnityEngine.InputSystem;
using TMPro;
using LitMotion;

public class OptionPanel : MonoBehaviour, UIControls.IUIActions
{
    [Header("パネル RectTransform")]
    [SerializeField] private RectTransform panelRect;

    [Header("項目ボタン参照 (ButtonのTransitionを利用)")]
    [SerializeField] private Button bgmButton;
    [SerializeField] private Button seButton;

    [Header("UIテキスト表示")]
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI seValueText;

    [Header("スライド設定")]
    [SerializeField] private float hiddenPosX = 1200f;
    [SerializeField] private float showPosX = 0f;
    [SerializeField] private float slideDuration = 0.28f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Header("入力クールダウン設定")]
    [SerializeField] private float inputRepeatDelay = 0.18f;

    private UIControls controls;
    private int selectedCategory = 0; // 0: BGM, 1: SE
    private int currentBGMVol = 100;
    private int currentSEVol = 100;

    private float nextInputTime = 0f;
    private Action onClosedCallback;
    private bool isOpen = false;
    private MotionHandle activeSlideMotion;

    private void Awake()
    {
        controls = new UIControls();
        controls.UI.SetCallbacks(this);

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
        // 自動二重移動を防ぐため Navigation を None に設定し、マウスホバーを無効化
        Button[] buttons = { bgmButton, seButton };
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

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
        selectedCategory = 0; // 初期選択: BGM
        nextInputTime = Time.unscaledTime + 0.15f;

        if (SoundManager.Instance != null)
        {
            currentBGMVol = SoundManager.Instance.BGMVolume;
            currentSEVol = SoundManager.Instance.SEVolume;
        }

        UpdateValueTexts();
        HighlightCurrentButton(); // ボタンのフォーカス・ハイライト適用

        controls.UI.Enable();

        if (activeSlideMotion.IsActive()) activeSlideMotion.Cancel();

        activeSlideMotion = LMotion.Create(panelRect.anchoredPosition.x, showPosX, slideDuration)
            .WithEase(slideEase)
            .Bind(x =>
            {
                if (panelRect != null)
                    panelRect.anchoredPosition = new Vector2(x, panelRect.anchoredPosition.y);
            });
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

        StartCoroutine(DelayRoutine(slideDuration, () =>
        {
            gameObject.SetActive(false);
            onClosedCallback?.Invoke();
        }));
    }

    private void UpdateValueTexts()
    {
        if (bgmValueText != null) bgmValueText.text = currentBGMVol.ToString();
        if (seValueText != null) seValueText.text = currentSEVol.ToString();
    }

    private void HighlightCurrentButton()
    {
        Button targetBtn = (selectedCategory == 0) ? bgmButton : seButton;
        if (targetBtn != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null); // クリアして確実に再選択
            targetBtn.Select();
        }
    }

    // --- UIControls コールバック ---

    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;
        if (Time.unscaledTime < nextInputTime) return;

        Vector2 dir = context.ReadValue<Vector2>();

        // 上下入力：項目切り替え (0: BGM ⇔ 1: SE)
        if (dir.y > 0.5f && selectedCategory > 0)
        {
            selectedCategory--;
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
            SoundManager.Instance?.PlayUIMoveSE();
            HighlightCurrentButton();
        }
        else if (dir.y < -0.5f && selectedCategory < 1)
        {
            selectedCategory++;
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
            SoundManager.Instance?.PlayUIMoveSE();
            HighlightCurrentButton();
        }

        // 左右入力：数値変更 (10刻み)
        if (dir.x < -0.5f)
        {
            ChangeVolume(-10);
            SoundManager.Instance?.PlayUIMoveSE();
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
        }
        else if (dir.x > 0.5f)
        {
            ChangeVolume(10);
            SoundManager.Instance?.PlayUIMoveSE();
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
        }
    }

    private void ChangeVolume(int delta)
    {
        if (selectedCategory == 0) // BGM
        {
            currentBGMVol = Mathf.Clamp(currentBGMVol + delta, 0, 100);
            SoundManager.Instance?.SetBGMVolume(currentBGMVol);
        }
        else
        {
            currentSEVol = Mathf.Clamp(currentSEVol + delta, 0, 100);
            SoundManager.Instance?.SetSEVolume(currentSEVol);
        }
        UpdateValueTexts();
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;
        SoundManager.Instance?.PlayUISelectSE();
        ClosePanelToMain();
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;
        SoundManager.Instance?.PlayUICancelSE();
        ClosePanelToMain();
    }

    private System.Collections.IEnumerator DelayRoutine(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }
}