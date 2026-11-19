using UnityEngine;

public class PainsoltSupport : MonoBehaviour, IAssistantSupport
{
    [Tooltip("エネルギーの毎秒自動増加量の加算値")]
    [SerializeField] private float bonusEnergyPerSec = 1.0f;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;
        ship.AddAutoEnergyPerSec(bonusEnergyPerSec);
    }
}