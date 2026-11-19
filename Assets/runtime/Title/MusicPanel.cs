using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using LitMotion;

public class MusicPanel : MonoBehaviour, UIControls.IUIActions
{
    [System.Serializable]
    public class MusicEntry
    {
        [Tooltip("曲ボタン参照")]
        public Button songButton;
        [Tooltip("再生用 Audio Clip")]
        public AudioClip bgmClip;
        [Tooltip("曲の説明文")]
        [TextArea(2, 4)]
        public string description;
    }

    [Header("パネル RectTransform")]
    [SerializeField] private RectTransform panelRect;

    [Header("BGMリスト設定")]
    [SerializeField] private List<MusicEntry> musicList = new();

    [Header("説明文表示 UI")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("スライド設定")]
    [SerializeField] private float hiddenPosX = 1200f;
    [SerializeField] private float showPosX = 0f;
    [SerializeField] private float slideDuration = 0.28f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Header("入力クールダウン設定")]
    [SerializeField] private float inputRepeatDelay = 0.18f;

    private UIControls controls;
    private int selectedIndex = 0;
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
        foreach (var entry in musicList)
        {
            if (entry == null || entry.songButton == null) continue;

            Navigation nav = entry.songButton.navigation;
            nav.mode = Navigation.Mode.None;
            entry.songButton.navigation = nav;

            var graphics = entry.songButton.GetComponentsInChildren<Graphic>(true);
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
        selectedIndex = 0;
        nextInputTime = Time.unscaledTime + 0.15f;

        UpdateSelectionUI();

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

    private void UpdateSelectionUI()
    {
        if (musicList.Count == 0) return;

        // ボタンのハイライト
        if (selectedIndex >= 0 && selectedIndex < musicList.Count)
        {
            var currentEntry = musicList[selectedIndex];

            if (currentEntry.songButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                currentEntry.songButton.Select();
            }

            // 説明文の更新
            if (descriptionText != null)
            {
                descriptionText.text = currentEntry.description;
            }
        }
    }

    // --- UIControls コールバック ---

    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;
        if (Time.unscaledTime < nextInputTime) return;

        Vector2 dir = context.ReadValue<Vector2>();

        if (dir.y > 0.5f && selectedIndex > 0)
        {
            selectedIndex--;
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
            SoundManager.Instance?.PlayUIMoveSE();
            UpdateSelectionUI();
        }
        else if (dir.y < -0.5f && selectedIndex < musicList.Count - 1)
        {
            selectedIndex++;
            nextInputTime = Time.unscaledTime + inputRepeatDelay;
            SoundManager.Instance?.PlayUIMoveSE();
            UpdateSelectionUI();
        }
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!isOpen || !context.performed) return;

        if (selectedIndex >= 0 && selectedIndex < musicList.Count)
        {
            var currentEntry = musicList[selectedIndex];
            if (currentEntry.bgmClip != null)
            {
                SoundManager.Instance?.PlayUISelectSE();
                SoundManager.Instance?.PlayBGM(currentEntry.bgmClip);
            }
        }
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