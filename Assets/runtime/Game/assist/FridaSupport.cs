using UnityEngine;

public class FridaSupport : MonoBehaviour, IAssistantSupport
{
    [Tooltip("ショットの火力倍率")]
    [SerializeField] private float damageMultiplier = 1.2f;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;
        ship.SetShotDamageMultiplier(damageMultiplier);
    }
}