using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using LitMotion;

public class BattleGameManager : MonoBehaviour
{
    public static BattleGameManager Instance { get; private set; }

    [System.Serializable]
    public struct CharacterPortraitSet
    {
        [Tooltip("通常立ち絵 (スタート時)")]
        public Sprite normal;
        [Tooltip("勝利立ち絵 (リザルト時)")]
        public Sprite win;
        [Tooltip("敗北立ち絵 (リザルト時)")]
        public Sprite lose;
        [Tooltip("対戦相手別 勝利メッセージSO")]
        public VictoryMessageSO victoryMessageSO;
    }

    [Header("UI")]
    [Tooltip("リザルト・カウントダウン共通テキスト")]
    [SerializeField] private TextMeshProUGUI resultText;
    [Tooltip("試合時間表示テキスト (画面中央真ん中上 / 00:00形式)")]
    [SerializeField] private TextMeshProUGUI matchTimerText;

    [Header("勝利メッセージ UI")]
    [Tooltip("メッセージウィンドウ (Image)")]
    [SerializeField] private GameObject messageWindow;
    [Tooltip("メッセージ表示テキスト (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("スタート & リザルト時キャラ立ち絵 UI")]
    [Tooltip("1P立ち絵Image (画面左)")]
    [SerializeField] private Image p1StartPortrait;
    [Tooltip("2P立ち絵Image (画面右)")]
    [SerializeField] private Image p2StartPortrait;
    [Tooltip("キャラクター立ち絵 & メッセージデータリスト")]
    [SerializeField] private List<CharacterPortraitSet> characterPortraits = new();
    [Tooltip("立ち絵のスライド量")]
    [SerializeField] private float slideOffset = 100f;

    [Header("シーン遷移設定")]
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private float fadeDuration = 1.0f;

    public bool IsGameStarted { get; private set; } = false;
    public bool IsGameOver { get; private set; } = false;
    private float matchTime = 0f;
    private bool isMatchTimerRunning = false;

    private Vector2 p1BasePos;
    private Vector2 p2BasePos;
    private MotionHandle p1Motion;
    private MotionHandle p2Motion;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (resultText != null) resultText.gameObject.SetActive(false);
        if (messageWindow != null) messageWindow.SetActive(false);

        if (p1StartPortrait != null)
        {
            p1BasePos = p1StartPortrait.rectTransform.anchoredPosition;
            p1StartPortrait.gameObject.SetActive(false);
        }
        if (p2StartPortrait != null)
        {
            p2BasePos = p2StartPortrait.rectTransform.anchoredPosition;
            p2StartPortrait.gameObject.SetActive(false);
        }
        if (matchTimerText != null)
        {
            matchTimerText.text = "00:00";
        }

        IsGameOver = false;
        IsGameStarted = false;
    }

    private void Start()
    {
        StartCoroutine(GameStartSequence());
    }

    private void Update()
    {
        // 試合タイマーの更新
        if (isMatchTimerRunning)
        {
            matchTime += Time.deltaTime;
            UpdateMatchTimerUI();
        }
    }

    private void UpdateMatchTimerUI()
    {
        if (matchTimerText == null) return;

        int minutes = Mathf.FloorToInt(matchTime / 60f);
        int seconds = Mathf.FloorToInt(matchTime % 60f);
        matchTimerText.text = $"{minutes:00}:{seconds:00}";
    }

    // --- ゲーム開始カウントダウン ---
    private IEnumerator GameStartSequence()
    {
        IsGameStarted = false;
        SetPlayersControlActive(false);
        yield return null;

        int p1Idx = CharacterSelectManager.Instance != null ? CharacterSelectManager.Instance.P1CharIndex : 0;
        int p2Idx = CharacterSelectManager.Instance != null ? CharacterSelectManager.Instance.P2CharIndex : 0;

        ShowPortraitWithSlide(p1StartPortrait, p1BasePos, p1Idx, PortraitType.Normal, isP1: true, isWinner: true);
        ShowPortraitWithSlide(p2StartPortrait, p2BasePos, p2Idx, PortraitType.Normal, isP1: false, isWinner: true);

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = "Attention!";
        }
        yield return new WaitForSeconds(2.0f);

        HidePortraits();

        if (resultText != null)
        {
            resultText.text = "Go!";
        }

        isMatchTimerRunning = true;
        IsGameStarted = true;
        SetPlayersControlActive(true);

        yield return new WaitForSeconds(2.0f);

        if (resultText != null && !IsGameOver)
        {
            resultText.gameObject.SetActive(false);
        }
    }

    public void OnPlayerDefeated(PlayerShip loser)
    {
        if (IsGameOver) return;
        IsGameOver = true;

        StartCoroutine(GameOverSequence(loser));
    }

    // --- ゲーム終了シーケンス ---
    private IEnumerator GameOverSequence(PlayerShip loser)
    {
        isMatchTimerRunning = false; // 試合タイマー停止
        StopAllSpawners();
        SetPlayersControlActive(false);

        PlayerShip winner = null;
        PlayerShip[] players = FindObjectsByType<PlayerShip>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (p != loser)
            {
                winner = p;
                p.SetInvincibleState();
            }
        }

        int winnerNum = (winner != null) ? winner.PlayerIndex + 1 : 1;
        bool isP1Winner = (winnerNum == 1);

        ClearAllFieldObjects();

        yield return new WaitForSeconds(1.0f);

        var bsd = BattleSettingData.Instance;
        bool isArcade = (bsd != null && bsd.IsArcadeMode);

        // --- 1. 結果テキスト表示 ---
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);

            if (isArcade)
            {
                if (isP1Winner)
                {
                    // アーケード勝利時：今回の試合時間を加算し、現在の累積タイムを表示
                    float currentTotalTime = bsd.TotalArcadeTime + matchTime;
                    resultText.text = $"1P WIN!\n{ArcadeScoreManager.FormatTime(currentTotalTime)}";
                }
                else
                {
                    // アーケード敗北時：「RETIRE」表記
                    resultText.text = "2P WIN!\nRETIRE";
                }
            }
            else
            {
                resultText.text = $"{winnerNum}P WIN!";
            }
        }

        yield return new WaitForSeconds(2.0f);

        // --- 2. 立ち絵表示 & 勝利メッセージ表示 ---
        int p1Idx = CharacterSelectManager.Instance != null ? CharacterSelectManager.Instance.P1CharIndex : 0;
        int p2Idx = CharacterSelectManager.Instance != null ? CharacterSelectManager.Instance.P2CharIndex : 0;

        int winnerCharIdx = isP1Winner ? p1Idx : p2Idx;
        int loserCharIdx = isP1Winner ? p2Idx : p1Idx;

        ShowPortraitWithSlide(p1StartPortrait, p1BasePos, p1Idx, isP1Winner ? PortraitType.Win : PortraitType.Lose, isP1: true, isWinner: isP1Winner);
        ShowPortraitWithSlide(p2StartPortrait, p2BasePos, p2Idx, !isP1Winner ? PortraitType.Win : PortraitType.Lose, isP1: false, isWinner: !isP1Winner);

        ShowVictoryMessage(winnerCharIdx, loserCharIdx, isP1Winner);

        // --- 3. 4秒待機後の進行分岐 ---
        yield return new WaitForSeconds(4.0f);

        if (isArcade)
        {
            if (isP1Winner)
            {
                // 通算タイムを更新
                bsd.TotalArcadeTime += matchTime;

                if (bsd.CurrentArcadeStage < 3)
                {
                    // ステージを進めて次の試合（ゲームシーン再ロード）
                    bsd.CurrentArcadeStage++;
                    CleanupBeforeSceneTransition(isNextStage: true);
                    Initiate.Fade(SceneManager.GetActiveScene().name, Color.black, 1.0f / fadeDuration);
                }
                else
                {
                    // 3連戦全勝クリア！ 永続ハイスコア保存してタイトルへ戻る
                    ArcadeScoreManager.SaveScore(p1Idx, bsd.TotalArcadeTime);
                    bsd.ResetArcadeData();
                    CleanupBeforeSceneTransition(isNextStage: false);
                    Initiate.Fade(titleSceneName, Color.black, 1.0f / fadeDuration);
                }
            }
            else
            {
                // リタイア：記録は残さず初期化してタイトルへ戻る
                bsd.ResetArcadeData();
                CleanupBeforeSceneTransition(isNextStage: false);
                Initiate.Fade(titleSceneName, Color.black, 1.0f / fadeDuration);
            }
        }
        else
        {
            CleanupBeforeSceneTransition(isNextStage: false);
            Initiate.Fade(titleSceneName, Color.black, 1.0f / fadeDuration);
        }
    }

    private void ShowVictoryMessage(int winnerCharIdx, int loserCharIdx, bool isP1Winner)
    {
        if (messageWindow == null || messageText == null) return;
        if (winnerCharIdx < 0 || winnerCharIdx >= characterPortraits.Count) return;

        var winnerSet = characterPortraits[winnerCharIdx];
        string msg = (winnerSet.victoryMessageSO != null)
            ? winnerSet.victoryMessageSO.GetMessage(loserCharIdx)
            : "勝利した！";

        string colorHex = isP1Winner ? "#CCCCFF" : "#FFCCCC";
        messageText.text = $"<color={colorHex}>{msg}</color>";

        messageWindow.SetActive(true);
    }

    private enum PortraitType { Normal, Win, Lose }

    private void ShowPortraitWithSlide(Image img, Vector2 basePos, int charIndex, PortraitType type, bool isP1, bool isWinner)
    {
        if (img == null || charIndex < 0 || charIndex >= characterPortraits.Count) return;

        var set = characterPortraits[charIndex];
        Sprite spriteToSet = type switch
        {
            PortraitType.Win => set.win,
            PortraitType.Lose => set.lose,
            _ => set.normal
        };

        if (spriteToSet == null) spriteToSet = set.normal;
        if (spriteToSet == null) return;

        img.sprite = spriteToSet;
        img.gameObject.SetActive(true);
        img.color = isWinner ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1.0f);

        ref MotionHandle handle = ref (isP1 ? ref p1Motion : ref p2Motion);
        if (handle.IsActive()) handle.Cancel();

        float startX = isP1 ? basePos.x - slideOffset : basePos.x + slideOffset;
        img.rectTransform.anchoredPosition = new Vector2(startX, basePos.y);

        handle = LMotion.Create(startX, basePos.x, 0.25f)
            .WithEase(Ease.OutCubic)
            .Bind(x => {
                if (img != null) img.rectTransform.anchoredPosition = new Vector2(x, basePos.y);
            });
    }

    private void HidePortraits()
    {
        if (p1Motion.IsActive()) p1Motion.Cancel();
        if (p2Motion.IsActive()) p2Motion.Cancel();

        if (p1StartPortrait != null) p1StartPortrait.gameObject.SetActive(false);
        if (p2StartPortrait != null) p2StartPortrait.gameObject.SetActive(false);
        if (messageWindow != null) messageWindow.SetActive(false);
    }

    private void SetPlayersControlActive(bool active)
    {
        var inputs = FindObjectsByType<PlayerInput>(FindObjectsInactive.Exclude);
        foreach (var pInput in inputs)
        {
            if (pInput == null || !pInput.enabled || !pInput.gameObject.activeInHierarchy) continue;

            if (active) pInput.ActivateInput();
            else pInput.DeactivateInput();
        }

        var cpus = FindObjectsByType<CPUShip>(FindObjectsInactive.Exclude);
        foreach (var cpu in cpus)
        {
            if (cpu != null) cpu.enabled = active;
        }
    }

    private void CleanupBeforeSceneTransition(bool isNextStage)
    {
        Time.timeScale = 1.0f;

        // 次のステージへ進む場合は BattleSettingData を破棄しない
        if (!isNextStage && BattleSettingData.Instance != null)
        {
            BattleSettingData.Instance.ResetHandicaps();
            Destroy(BattleSettingData.Instance.gameObject);
        }

        if (EnemyBulletPool.Instance != null)
        {
            EnemyBulletPool.Instance.ClearAllPools();
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.OnPauseStateChanged(true);
        }
    }

    private void ClearAllFieldObjects()
    {
        var bullets = FindObjectsByType<EnemyBullet>(FindObjectsInactive.Exclude);
        foreach (var b in bullets) if (b != null) b.ReleaseToPool();

        var curvingBullets = FindObjectsByType<CurvingEnemyBullet>(FindObjectsInactive.Exclude);
        foreach (var cb in curvingBullets) if (cb != null) cb.ReleaseToPool();

        var accBullets = FindObjectsByType<AcceleratingBullet>(FindObjectsInactive.Exclude);
        foreach (var ab in accBullets) if (ab != null) ab.ReleaseToPool();

        var enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        foreach (var e in enemies) Destroy(e.gameObject);

        var bosses = FindObjectsByType<EngageBossBase>(FindObjectsInactive.Exclude);
        foreach (var boss in bosses) Destroy(boss.gameObject);

        var emitters = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        foreach (var em in emitters)
        {
            if (em is IAttackEmitter && em.gameObject != null) Destroy(em.gameObject);
        }
    }

    private void StopAllSpawners()
    {
        WaveManager waveMgr = FindAnyObjectByType<WaveManager>();
        if (waveMgr != null) waveMgr.StopSpawning();

        MidBossSpawner midBossSpawner = FindAnyObjectByType<MidBossSpawner>();
        if (midBossSpawner != null) midBossSpawner.StopSpawning();
    }

    public void ReturnToTitleDirect()
    {
        isMatchTimerRunning = false;
        StopAllSpawners();
        ClearAllFieldObjects();

        if (BattleSettingData.Instance != null)
        {
            BattleSettingData.Instance.ResetArcadeData();
            BattleSettingData.Instance.ResetHandicaps();
            Destroy(BattleSettingData.Instance.gameObject);
        }

        if (EnemyBulletPool.Instance != null)
        {
            EnemyBulletPool.Instance.ClearAllPools();
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.OnPauseStateChanged(false);
        }

        Time.timeScale = 1.0f;

        var fieldInfo = typeof(Initiate).GetField("areWeFading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        fieldInfo?.SetValue(null, false);

        Initiate.Fade(titleSceneName, Color.black, 1.0f / fadeDuration);
    }
}