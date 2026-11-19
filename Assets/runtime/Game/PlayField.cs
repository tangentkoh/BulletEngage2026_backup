using UnityEngine;
using TMPro;

public class PlayField : MonoBehaviour
{
    [Header("プレイヤーID (0: 1P左, 1: 2P右)")]
    public int fieldIndex = 0;

    [Header("フィールド枠 (ワールド座標の境界)")]
    public Rect bounds = new Rect(-8.5f, -4.8f, 8.0f, 9.6f);

    [Header("対戦相手のフィールド参照")]
    [SerializeField] private PlayField targetOpponentField;

    [Header("ボス演出 UI (フィールド固定)")]
    [Tooltip("ボス滞在タイマーテキスト (30.00 形式)")]
    [SerializeField] private TextMeshProUGUI bossTimeText;

    [Tooltip("エンゲージアタック中の暗幕パネル (画面半分を覆う半透明黒UI)")]
    [SerializeField] private GameObject darkOverlayPanel;

    public PlayField OpponentField => targetOpponentField;

    private void Awake()
    {
        SetBossTimerVisible(false);
    }

    // ボスタイマーおよび暗幕パネルの表示・非表示一括制御
    public void SetBossTimerVisible(bool visible)
    {
        if (bossTimeText != null)
        {
            bossTimeText.gameObject.SetActive(visible);
        }

        if (darkOverlayPanel != null)
        {
            darkOverlayPanel.SetActive(visible);
        }
    }

    // 小数点第2位までフォーマットして表示 (例: 30.00, 04.25)
    public void UpdateBossTimer(float seconds)
    {
        if (bossTimeText != null)
        {
            float clampedSeconds = Mathf.Max(0f, seconds);
            bossTimeText.text = clampedSeconds.ToString("00.00");
        }
    }

    public Vector3 ClampPosition(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, bounds.xMin, bounds.xMax);
        pos.y = Mathf.Clamp(pos.y, bounds.yMin, bounds.yMax);
        return pos;
    }

    public Vector3 MirrorToOpponentField(Vector3 currentWorldPos)
    {
        if (targetOpponentField == null) return currentWorldPos;

        float normalizedX = Mathf.InverseLerp(bounds.xMin, bounds.xMax, currentWorldPos.x);
        float normalizedY = Mathf.InverseLerp(bounds.yMin, bounds.yMax, currentWorldPos.y);

        float targetX = Mathf.Lerp(targetOpponentField.bounds.xMin, targetOpponentField.bounds.xMax, normalizedX);
        float targetY = Mathf.Lerp(targetOpponentField.bounds.yMin, targetOpponentField.bounds.yMax, normalizedY);

        return new Vector3(targetX, targetY, 0f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = fieldIndex == 0 ? Color.cyan : Color.magenta;
        Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.center.y, 0), new Vector3(bounds.size.x, bounds.size.y, 0));
    }
}