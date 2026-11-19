using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; } = false;

    [Header("ポーズUIパネル")]
    [SerializeField] private GameObject pausePanel;

    private float pauseOpenedTime = 0f;
    private const float InputCooldown = 1f; // 開いた直後の即閉じ防止クールタイム

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (pausePanel != null) pausePanel.SetActive(false);
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // ポーズ中以外、または開いた直後の入力インターバル中は処理しない
        if (!IsPaused) return;
        if (Time.unscaledTime < pauseOpenedTime + InputCooldown) return;

        if (IsResumePressed())
        {
            ResumeGame();
            return;
        }

        if (IsReturnToTitlePressed())
        {
            ReturnToTitle();
            return;
        }
    }

    private bool IsResumePressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame)
                return true;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private bool IsReturnToTitlePressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.xKey.wasPressedThisFrame)
                return true;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    public void OnPause(InputValue value)
    {
        if (value.isPressed)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPause(!IsPaused);
    }

    public void SetPause(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(pause);
        }

        if (pause)
        {
            pauseOpenedTime = Time.unscaledTime;
            SoundManager.Instance?.PlayPauseSE();
        }

        SoundManager.Instance?.OnPauseStateChanged(pause);
    }

    private void ResumeGame()
    {
        SoundManager.Instance?.PlayUISelectSE();
        SetPause(false);
    }

    private void ReturnToTitle()
    {
        SoundManager.Instance?.PlayUICancelSE();
        SetPause(false);

        // データリセットとタイトルへの遷移処理
        if (BattleGameManager.Instance != null)
        {
            BattleGameManager.Instance.ReturnToTitleDirect();
        }
        else
        {
            if (BattleSettingData.Instance != null)
            {
                BattleSettingData.Instance.ResetArcadeData();
                BattleSettingData.Instance.ResetHandicaps();
                Destroy(BattleSettingData.Instance.gameObject);
            }
            Time.timeScale = 1.0f;
            Initiate.Fade("Title", Color.black, 2.0f);
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        IsPaused = false;
    }
}