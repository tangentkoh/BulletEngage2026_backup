using UnityEngine;

public class CharlotteSupport : MonoBehaviour, IAssistantSupport
{
    [Tooltip("高速移動速度の加算値")]
    [SerializeField] private float speedAddAmount = 1.5f;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;
        ship.AddNormalSpeed(speedAddAmount);
    }
}