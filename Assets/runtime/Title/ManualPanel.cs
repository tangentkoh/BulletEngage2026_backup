using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using LitMotion;

public class ManualPanel : MonoBehaviour, UIControls.IUIActions
{
    [Header("パネル RectTransform")]
    [SerializeField] private RectTransform panelRect;

    [Header("マニュアルページ画像リスト")]
    [Tooltip("順に表示するページ画像(Sprite)を登録")]
    [SerializeField] private List<Sprite> manualPages = new();

    [Header("UI要素参照")]
    [SerializeField] private Image pageImageDisplay;
    [Tooltip("ページ番号表示テキスト (例: 1 / 5)")]
    [SerializeField] private TextMeshProUGUI pageNumberText;

    [Header("スライド設定")]
    [SerializeField] private float hiddenPosX = 1200f;
    [SerializeField] private float showPosX = 0f;
    [SerializeField] private float slideDuration = 0.28f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Header("入力設定")]
    [SerializeField] private float inputRepeatDelay = 0.2f;

    private UIControls controls;
    private int currentPageIndex = 0;
    private float nextInputTime = 0f;
    private Action onClosedCallback;
    private bool isOpen = false;
    private MotionHandle activeSlideMotion;

    private void Awake()
    {
        controls = new UIControls();
        controls.UI.SetCallbacks(this);

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

    public void OpenPanel(Action onClose)
    {
        onClosedCallback = onClose;
        gameObject.SetActive(true);
        isOpen = true;
        currentPageIndex = 0; // 1ページ目から表示
        nextInputTime = Time.unscaledTime + 0.15f;

        UpdatePageUI();

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

    private void UpdatePageUI()
    {
        if (manualPages == null || manualPages.Count == 0) return;

        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, manualPages.Count - 1);

        // 画像の差し替え
        if (pageImageDisplay != null)
        {
            pageImageDisplay.sprite = manualPages[currentPageIndex];
        }

        // ページテキスト更新 (例: "1 / 5")
        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentPageIndex + 1} / {manualPages.Count}";
        }
    }

    // --- UIControls コールバック ---

    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;
        if (Time.unscaledTime < nextInputTime) return;

        Vector2 dir = context.ReadValue<Vector2>();

        // 次のページ (右キー または 下キー)
        if ((dir.x > 0.5f || dir.y < -0.5f) && currentPageIndex < manualPages.Count - 1)
        {
            currentPageIndex++;
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
            SoundManager.Instance?.PlayUIMoveSE();
            UpdatePageUI();
        }
        // 前のページ (左キー または 上キー)
        else if ((dir.x < -0.5f || dir.y > 0.5f) && currentPageIndex > 0)
        {
            currentPageIndex--;
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
            SoundManager.Instance?.PlayUIMoveSE();
            UpdatePageUI();
        }
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;
        Debug.Log("ManualPanelでは決定ボタンは無効です");
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