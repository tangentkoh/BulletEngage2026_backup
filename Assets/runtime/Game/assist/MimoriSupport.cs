using UnityEngine;

public class MimoriSupport : MonoBehaviour, IAssistantSupport
{
    [Tooltip("無敵時間の追加量 (秒)")]
    [SerializeField] private float bonusInvincibilityDuration = 0.2f;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;
        ship.AddInvincibilityDuration(bonusInvincibilityDuration);
    }
}