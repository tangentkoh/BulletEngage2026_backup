using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleSceneManager : MonoBehaviour
{
    public static TitleSceneManager Instance { get; private set; }

    [Header("UI コントローラー")]
    [SerializeField] private VerticalReelMenuController reelMenu;
    [SerializeField] private ArcadeCharSelectPanel arcadePanel;
    [SerializeField] private OneMatchModeSelectPanel oneMatchPanel;
    [SerializeField] private OptionPanel optionPanel;
    [SerializeField] private MusicPanel musicPanel;
    [SerializeField] private ManualPanel manualPanel;

    [Header("サブメニュー展開時に非表示にするオブジェクト")]
    [Tooltip("Canvas2配下の titletext などを登録")]
    [SerializeField] private List<GameObject> hideObjectsOnSubMenu;

    [Header("フェード設定 (Exit時)")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float fadeDuration = 1.0f;

    private bool isProcessing = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnClickExit()
    {
        if (isProcessing) return;
        isProcessing = true;

        if (reelMenu != null) reelMenu.enabled = false;
        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        Initiate.Fade("", fadeColor, 1.0f / fadeDuration);
        yield return new WaitForSeconds(fadeDuration);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickArcade()
    {
        if (isProcessing) return;

        if (reelMenu != null)
        {
            reelMenu.SetMenuVisible(false, 0.2f);
        }
        SetSubMenuDecorationsVisible(false);

        if (arcadePanel != null)
        {
            arcadePanel.OpenPanel(() =>
            {
                if (reelMenu != null)
                {
                    reelMenu.SetMenuVisible(true, 0.2f);
                }
                SetSubMenuDecorationsVisible(true);
            });
        }
    }

    public void OnClickOneMatch()
    {
        if (isProcessing) return;

        if (reelMenu != null)
        {
            reelMenu.SetMenuVisible(false, 0.2f);
        }
        SetSubMenuDecorationsVisible(false);

        if (oneMatchPanel != null)
        {
            oneMatchPanel.OpenPanel(() =>
            {
                if (reelMenu != null)
                {
                    reelMenu.SetMenuVisible(true, 0.2f);
                }
                SetSubMenuDecorationsVisible(true);
            });
        }
    }

    public void OnClickOption()
    {
        if (isProcessing) return;

        if (reelMenu != null)
        {
            reelMenu.SetMenuVisible(false, 0.2f);
        }
        SetSubMenuDecorationsVisible(false);

        if (optionPanel != null)
        {
            optionPanel.OpenPanel(() =>
            {
                if (reelMenu != null)
                {
                    reelMenu.SetMenuVisible(true, 0.2f);
                }
                SetSubMenuDecorationsVisible(true);
            });
        }
    }

    public void OnClickMusic()
    {
        if (isProcessing) return;

        if (reelMenu != null)
        {
            reelMenu.SetMenuVisible(false, 0.2f);
        }
        SetSubMenuDecorationsVisible(false);

        if (musicPanel != null)
        {
            musicPanel.OpenPanel(() =>
            {
                if (reelMenu != null)
                {
                    reelMenu.SetMenuVisible(true, 0.2f);
                }
                SetSubMenuDecorationsVisible(true);
            });
        }
    }

    public void OnClickManual()
    {
        if (isProcessing) return;

        if (reelMenu != null)
        {
            reelMenu.SetMenuVisible(false, 0.2f);
        }
        SetSubMenuDecorationsVisible(false);

        if (manualPanel != null)
        {
            manualPanel.OpenPanel(() =>
            {
                if (reelMenu != null)
                {
                    reelMenu.SetMenuVisible(true, 0.2f);
                }
                SetSubMenuDecorationsVisible(true);
            });
        }
    }

    private void SetSubMenuDecorationsVisible(bool visible)
    {
        if (hideObjectsOnSubMenu == null) return;
        foreach (var obj in hideObjectsOnSubMenu)
        {
            if (obj != null) obj.SetActive(visible);
        }
    }
}