using UnityEngine;

public class AnnaSupport : MonoBehaviour, IAssistantSupport
{
    [Tooltip("チャージ速度の加算値")]
    [SerializeField] private float addChargeSpeed = 10.0f;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;
        ship.AddChargeSpeed(addChargeSpeed);
    }
}