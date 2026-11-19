using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro; // ★ TextMeshProUGUI 用に追加
using LitMotion;
using LitMotion.Extensions;

public class VerticalReelMenuController : MonoBehaviour, UIControls.IUIActions
{
    [Header("UI Elements (Buttons)")]
    [Tooltip("上から Arcade, OneMatch, Memory, Manual, Option, Exit の順に登録")]
    [SerializeField] private List<Button> buttonList;

    // ★ 追加: モード説明文 UI & テキストデータ
    [Header("説明文 UI")]
    [SerializeField] private TextMeshProUGUI modeDescriptionText;

    [Header("各項目の説明文 (buttonList と同じ順番で設定)")]
    [Tooltip("上から Arcade, OneMatch, Memory, Manual, Option, Exit の説明文を順に登録")]
    [SerializeField] [TextArea(2, 4)] private List<string> modeDescriptions = new();

    [Header("Reel Layout Settings")]
    [SerializeField] private Vector2 centerOffset = Vector2.zero;
    [SerializeField] private float itemSpacing = 85f;
    [SerializeField] private float drumTiltAngle = 24f;

    [Header("Scale & Alpha Settings")]
    [SerializeField] private float selectedScale = 1.15f;
    [SerializeField] private float unselectedScale = 0.85f;
    [SerializeField] private float selectedAlpha = 1.0f;
    [SerializeField] private float unselectedAlpha = 0.45f;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Input Repeat")]
    [SerializeField] private float repeatDelay = 0.22f;

    private UIControls controls;
    private int currentIndex = 0;
    private float nextInputTime = 0f;
    private readonly List<MotionHandle> activeMotions = new();
    private CanvasGroup rootCanvasGroup;

    public int CurrentIndex => currentIndex;

    private void Awake()
    {
        controls = new UIControls();
        controls.UI.SetCallbacks(this);

        rootCanvasGroup = GetComponent<CanvasGroup>();
        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        DisableButtonNavigationsAndMouse();
        EnsureCanvasGroups();
    }

    private void OnEnable()
    {
        controls?.UI.Enable();
    }

    private void OnDisable()
    {
        controls?.UI.Disable();
        CancelAllMotions();
    }

    private void OnDestroy()
    {
        controls?.Dispose();
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currentIndex = 0;
        UpdateMenuPosition(immediate: true);
    }

    public void SetMenuVisible(bool visible, float duration = 0.2f)
    {
        if (visible)
        {
            gameObject.SetActive(true);
            rootCanvasGroup.blocksRaycasts = false;

            LMotion.Create(rootCanvasGroup.alpha, 1f, duration)
                .WithEase(Ease.OutQuad)
                .Bind(a => rootCanvasGroup.alpha = a);

            enabled = true;
            controls?.UI.Enable();

            StartCoroutine(RestoreFocusRoutine());
        }
        else
        {
            enabled = false;
            controls?.UI.Disable();

            LMotion.Create(rootCanvasGroup.alpha, 0f, duration)
                .WithEase(Ease.InQuad)
                .Bind(a => {
                    if (rootCanvasGroup != null) rootCanvasGroup.alpha = a;
                });

            StartCoroutine(DisableAfterFade(duration));
        }
    }

    private IEnumerator DisableAfterFade(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!enabled)
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator RestoreFocusRoutine()
    {
        yield return null;

        if (buttonList.Count > currentIndex && buttonList[currentIndex] != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            buttonList[currentIndex].Select();
        }
        UpdateDescriptionUI(); // ★ 戻ってきた際に説明文を再適用
    }

    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (Time.unscaledTime < nextInputTime) return;

        Vector2 dir = context.ReadValue<Vector2>();

        if (dir.y > 0.5f && currentIndex > 0)
        {
            SelectPrevious();
            SoundManager.Instance?.PlayUIMoveSE();
            nextInputTime = Time.unscaledTime + repeatDelay;
        }
        else if (dir.y < -0.5f && currentIndex < buttonList.Count - 1)
        {
            SelectNext();
            SoundManager.Instance?.PlayUIMoveSE();
            nextInputTime = Time.unscaledTime + repeatDelay;
        }
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (currentIndex >= 0 && currentIndex < buttonList.Count && buttonList[currentIndex] != null)
        {
            SoundManager.Instance?.PlayUISelectSE();
            buttonList[currentIndex].onClick.Invoke();
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        int exitIndex = buttonList.Count - 1;
        if (currentIndex != exitIndex)
        {
            currentIndex = exitIndex;
            SoundManager.Instance?.PlayUICancelSE();
            UpdateMenuPosition(immediate: false);
        }
    }

    public void SelectNext()
    {
        if (currentIndex < buttonList.Count - 1)
        {
            currentIndex++;
            UpdateMenuPosition(immediate: false);
        }
    }

    public void SelectPrevious()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateMenuPosition(immediate: false);
        }
    }

    private void UpdateMenuPosition(bool immediate)
    {
        CancelAllMotions();

        for (int i = 0; i < buttonList.Count; i++)
        {
            Button btn = buttonList[i];
            if (btn == null) continue;

            RectTransform rect = btn.GetComponent<RectTransform>();
            int diff = i - currentIndex;

            float targetY = centerOffset.y - (diff * itemSpacing);
            Vector2 targetPos = new Vector2(centerOffset.x, targetY);

            float rotX = Mathf.Clamp(-diff * drumTiltAngle, -65f, 65f);
            Quaternion targetRot = Quaternion.Euler(rotX, 0f, 0f);

            float targetScale = (diff == 0)
                ? selectedScale
                : Mathf.Max(0.65f, unselectedScale - Mathf.Abs(diff) * 0.05f);

            float targetAlpha = (diff == 0)
                ? selectedAlpha
                : Mathf.Max(0.15f, unselectedAlpha - (Mathf.Abs(diff) - 1) * 0.15f);

            CanvasGroup cg = btn.GetComponent<CanvasGroup>();

            if (immediate)
            {
                rect.anchoredPosition = targetPos;
                rect.localRotation = targetRot;
                rect.localScale = Vector3.one * targetScale;
                if (cg != null) cg.alpha = targetAlpha;
            }
            else
            {
                activeMotions.Add(
                    LMotion.Create(rect.anchoredPosition, targetPos, moveDuration)
                        .WithEase(moveEase)
                        .BindToAnchoredPosition(rect)
                );

                activeMotions.Add(
                    LMotion.Create(rect.localRotation, targetRot, moveDuration)
                        .WithEase(moveEase)
                        .Bind(r => { if (rect != null) rect.localRotation = r; })
                );

                activeMotions.Add(
                    LMotion.Create(rect.localScale, Vector3.one * targetScale, moveDuration)
                        .WithEase(moveEase)
                        .BindToLocalScale(rect)
                );

                if (cg != null)
                {
                    activeMotions.Add(
                        LMotion.Create(cg.alpha, targetAlpha, moveDuration)
                            .WithEase(moveEase)
                            .Bind(a => { if (cg != null) cg.alpha = a; })
                    );
                }
            }
        }

        if (buttonList.Count > currentIndex && buttonList[currentIndex] != null)
        {
            buttonList[currentIndex].Select();
        }

        UpdateDescriptionUI(); // ★ 移動時に説明文テキストを更新
    }

    // ★ 追加: 現在のインデックスに応じて説明文UIを更新
    private void UpdateDescriptionUI()
    {
        if (modeDescriptionText == null) return;

        if (currentIndex >= 0 && currentIndex < modeDescriptions.Count)
        {
            modeDescriptionText.text = modeDescriptions[currentIndex];
        }
        else
        {
            modeDescriptionText.text = "";
        }
    }

    private void CancelAllMotions()
    {
        foreach (var handle in activeMotions)
        {
            if (handle.IsActive()) handle.Cancel();
        }
        activeMotions.Clear();
    }

    private void EnsureCanvasGroups()
    {
        foreach (var btn in buttonList)
        {
            if (btn != null && btn.GetComponent<CanvasGroup>() == null)
            {
                btn.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void DisableButtonNavigationsAndMouse()
    {
        foreach (var btn in buttonList)
        {
            if (btn == null) continue;

            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            Graphic[] graphics = btn.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                g.raycastTarget = false;
            }
        }
    }
}