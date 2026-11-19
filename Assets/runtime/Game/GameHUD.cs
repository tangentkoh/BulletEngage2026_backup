using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("ターゲットプレイヤー")]
    [SerializeField] private PlayerShip targetPlayer;

    [Header("HP & エネルギーゲージ (左下グループ)")]
    [Tooltip("HPゲージ (Image Type: Filled / 色: ピンク)")]
    [SerializeField] private Image hpBarFill;

    [Tooltip("エネルギーゲージ (Image Type: Filled / 色: 緑)")]
    [SerializeField] private Image energyBarFill;

    [Tooltip("チャージゲージ (Image Type: Filled / 色: 黄緑 / エネルギーバーに重ねる)")]
    [SerializeField] private Image chargeBarFill;

    [Header("パワー & ボム数値 (右下グループ)")]
    [Tooltip("パワー現在値テキスト (例: 1.0)")]
    [SerializeField] private TextMeshProUGUI powerText;

    [Tooltip("ボム現在値テキスト (例: 3)")]
    [SerializeField] private TextMeshProUGUI bombText;

    private void Update()
    {
        if (targetPlayer == null) return;

        // 1. HP ゲージ更新 (ピンク)
        if (hpBarFill != null && targetPlayer.MaxHP > 0f)
        {
            hpBarFill.fillAmount = Mathf.Clamp01(targetPlayer.CurrentHP / targetPlayer.MaxHP);
        }

        // 2. エネルギー ゲージ更新 (緑)
        if (energyBarFill != null && targetPlayer.MaxEnergy > 0f)
        {
            energyBarFill.fillAmount = Mathf.Clamp01(targetPlayer.CurrentEnergy / targetPlayer.MaxEnergy);
        }

        // 3. チャージ ゲージ更新 (黄緑)
        if (chargeBarFill != null && targetPlayer.MaxEnergy > 0f)
        {
            chargeBarFill.fillAmount = Mathf.Clamp01(targetPlayer.CurrentChargeProgress / targetPlayer.MaxEnergy);
        }

        // 4. パワー 数値更新
        if (powerText != null)
        {
            powerText.text = $"{targetPlayer.CurrentPower:F1}";
        }

        // 5. ボム 数値更新
        if (bombText != null)
        {
            bombText.text = $"{targetPlayer.CurrentBombCount}";
        }
    }

    public void SetTargetPlayer(PlayerShip ship)
    {
        targetPlayer = ship;
    }

    private void SetLineXPosition(RectTransform lineRect, float xPos)
    {
        if (lineRect == null) return;
        Vector2 pos = lineRect.anchoredPosition;
        pos.x = xPos;
        lineRect.anchoredPosition = pos;
    }
}