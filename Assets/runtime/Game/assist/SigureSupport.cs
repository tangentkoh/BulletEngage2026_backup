using UnityEngine;

public class SigureSupport : MonoBehaviour, IAssistantSupport
{
    [Tooltip("追加されるボム回数")]
    [SerializeField] private int extraBombCount = 2;
    [Tooltip("ボムペナルティ減少量")]
    [SerializeField] private int extraBombEnergyPenalty = 30;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;
        ship.AddMaxBombCount(extraBombCount);
        ship.ReduceEP(extraBombEnergyPenalty);
    }
}