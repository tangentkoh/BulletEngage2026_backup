using UnityEngine;
using TMPro;

public class SimpleDebugHUD : MonoBehaviour
{
    [SerializeField] private PlayerShip targetPlayer;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private string playerLabel = "1P";

    private void Update()
    {
        if (targetPlayer == null || statusText == null) return;

        statusText.text = $"[ {playerLabel} STATUS ]\n" +
                          $"HP     : {targetPlayer.CurrentHP:F0} / {targetPlayer.MaxHP:F0}\n" +
                          $"ENERGY : {targetPlayer.CurrentEnergy:F0} / {targetPlayer.MaxEnergy:F0}\n" +
                          $"CHARGE : {targetPlayer.CurrentChargeProgress:F0}%\n" +
                          $"POWER  : {targetPlayer.CurrentPower:F1} / 9.0\n" +
                          $"BOMB   : {targetPlayer.CurrentBombCount} / {targetPlayer.MaxBombCount}";
    }

    public void SetTargetPlayer(PlayerShip ship)
    {
        targetPlayer = ship;
    }
}