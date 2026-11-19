using UnityEngine;

public class NanaSupport : MonoBehaviour, IAssistantSupport
{
    [Tooltip("オプションショットの火力倍率")]
    [SerializeField] private float optionDamageMultiplier = 1.2f;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;
        ship.SetOptionDamageMultiplier(optionDamageMultiplier);
    }
}