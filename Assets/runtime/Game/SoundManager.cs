using UnityEngine;
using UnityEngine.SceneManagement; // ★追加

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("タイトル BGM & UI SE")]
    [SerializeField] private AudioClip titleBGM;           // タイトル画面BGM (1曲固定)
    [SerializeField] private AudioClip seUIMove;           // カーソル移動音
    [SerializeField] private AudioClip seUISelect;         // 決定音
    [SerializeField] private AudioClip seUICancel;         // キャンセル音

    [Header("自機 SE")]
    [SerializeField] private AudioClip sePlayerHit;        // 被弾時
    [SerializeField] private AudioClip sePlayerLastStand;  // 食いしばり発動時
    [SerializeField] private AudioClip sePlayerDefeat;     // 撃墜時
    [SerializeField] private AudioClip sePlayerBomb;       // ボム発動時
    [SerializeField] private AudioClip sePlayerCharging;   // チャージ中

    [Header("敵 SE")]
    [SerializeField] private AudioClip seEnemyHit;         // ショット命中時
    [SerializeField] private AudioClip seEnemyDefeat;      // 撃破時

    [Header("その他 SE")]
    [SerializeField] private AudioClip sePause;            // ポーズ時
    [SerializeField] private AudioClip seCutinCommon;
    [SerializeField] private AudioClip seCutinLv3;

    private const string BGM_VOL_KEY = "BGM_Volume";
    private const string SE_VOL_KEY = "SE_Volume";

    public int BGMVolume { get; private set; } = 100;
    public int SEVolume { get; private set; } = 100;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // ★シーンロード監視イベントの登録
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureAudioSources();
        LoadVolumeSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // イベント解除
        }
    }

    // ★シーン遷移時に自動実行される処理
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. ゲーム終了時の Pause 状態を確実に解除
        OnPauseStateChanged(false);

        // 2. タイトルシーンに戻ってきた場合はタイトルBGMを再生
        if (scene.name == "Title") // ※プロジェクトのタイトルシーン名に合わせて調整してください
        {
            PlayTitleBGM();
        }
    }

    private void EnsureAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
        }
    }

    private void LoadVolumeSettings()
    {
        BGMVolume = PlayerPrefs.GetInt(BGM_VOL_KEY, 100);
        SEVolume = PlayerPrefs.GetInt(SE_VOL_KEY, 100);
        ApplyVolume();
    }

    public void SetBGMVolume(int volume)
    {
        BGMVolume = Mathf.Clamp(volume, 0, 100);
        PlayerPrefs.SetInt(BGM_VOL_KEY, BGMVolume);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    public void SetSEVolume(int volume)
    {
        SEVolume = Mathf.Clamp(volume, 0, 100);
        PlayerPrefs.SetInt(SE_VOL_KEY, SEVolume);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    public void ApplyVolume()
    {
        if (bgmSource != null) bgmSource.volume = BGMVolume / 100f;
        if (seSource != null) seSource.volume = SEVolume / 100f;
    }

    // --- BGM 制御 ---
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSources();

        // Pause 状態から復帰できるよう UnPause を実行
        bgmSource.UnPause();

        bgmSource.clip = clip;
        bgmSource.volume = BGMVolume / 100f;
        bgmSource.Play();
    }

    public void PlayTitleBGM()
    {
        if (titleBGM == null) return;
        EnsureAudioSources();

        // すでにタイトルBGMが再生中の場合は最初から鳴らし直さない
        if (bgmSource.isPlaying && bgmSource.clip == titleBGM) return;

        PlayBGM(titleBGM);
    }

    // --- UI SE 再生窓口 ---
    public void PlayUIMoveSE() => PlaySE(seUIMove, 1.0f);
    public void PlayUISelectSE() => PlaySE(seUISelect, 1.0f);
    public void PlayUICancelSE() => PlaySE(seUICancel, 1.0f);

    // --- ゲーム内 SE 再生窓口 ---
    public void PlayEnemyHitSE() => PlaySE(seEnemyHit, 0.4f);
    public void PlayEnemyDefeatSE() => PlaySE(seEnemyDefeat, 0.8f);

    public void PlayPlayerHitSE() => PlaySE(sePlayerHit, 1.0f);
    public void PlayPlayerLastStandSE() => PlaySE(sePlayerLastStand, 1.0f);
    public void PlayPlayerDefeatSE() => PlaySE(sePlayerDefeat, 1.0f);
    public void PlayPlayerBombSE() => PlaySE(sePlayerBomb, 1.0f);
    public void PlayPlayerChargeSE() => PlaySE(sePlayerCharging, 1.0f);
    public void PlayPauseSE() => PlaySE(sePause, 1.0f);
    public void PlayCutinCommonSE() => PlaySE(seCutinCommon, 1.0f);
    public void PlayCutinLv3SE() => PlaySE(seCutinLv3, 1.0f);

    private void PlaySE(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null) return;
        EnsureAudioSources();
        seSource.PlayOneShot(clip, volume * (SEVolume / 100f));
    }

    // --- ポーズ連携 ---
    public void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused)
        {
            if (bgmSource != null && bgmSource.isPlaying) bgmSource.Pause();
            if (seSource != null) seSource.Pause();
        }
        else
        {
            if (bgmSource != null) bgmSource.UnPause();
            if (seSource != null) seSource.UnPause();
        }
    }
}