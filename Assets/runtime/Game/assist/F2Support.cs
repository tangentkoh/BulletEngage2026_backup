using UnityEngine;

public class F2Support : MonoBehaviour, IAssistantSupport
{
    [Tooltip("ゲーム開始時のパワー増加量")]
    [SerializeField] private float bonusPower = 1.0f;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;
        ship.AddPower(bonusPower);
    }
}