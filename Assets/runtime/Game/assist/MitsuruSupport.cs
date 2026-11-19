using UnityEngine;

public class MitsuruSupport : MonoBehaviour, IAssistantSupport
{
    [SerializeField] private float initialEnergy = 25f; // 開始時25%付与
    [SerializeField] private float refundEnergy = 5f;    // 発動時5%固定還元

    private PlayerShip boundShip;

    public void ApplySupport(PlayerShip ship)
    {
        boundShip = ship;
        if (boundShip == null) return;

        // 1. 開始時エネルギー25%付与
        boundShip.AddEnergy(initialEnergy);

        // 2. アタック発動イベントを購読して5%還元
        boundShip.OnAttackExecuted += HandleAttackExecuted;
    }

    private void HandleAttackExecuted(PlayerShip ship)
    {
        if (ship != null)
        {
            ship.AddEnergy(refundEnergy);
        }
    }

    private void OnDestroy()
    {
        if (boundShip != null)
        {
            boundShip.OnAttackExecuted -= HandleAttackExecuted;
        }
    }
}