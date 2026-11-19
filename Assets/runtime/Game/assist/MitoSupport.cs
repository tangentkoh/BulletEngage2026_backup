using System.Collections;
using UnityEngine;

public class MitoSupport : MonoBehaviour, IAssistantSupport
{
    [Tooltip("回復の間隔 (秒)")]
    [SerializeField] private float interval = 10.0f;

    [Tooltip("1回あたりの回復量")]
    [SerializeField] private float healAmount = 5.0f;

    private PlayerShip targetShip;
    private Coroutine regenCoroutine;

    public void ApplySupport(PlayerShip ship)
    {
        if (ship == null) return;

        targetShip = ship;
        // 自機と一緒に破棄・管理されるよう自機の子要素にアタッチ
        transform.SetParent(ship.transform);

        regenCoroutine = StartCoroutine(RegenRoutine());
    }

    private IEnumerator RegenRoutine()
    {
        while (targetShip != null)
        {
            // 10秒待機
            yield return new WaitForSeconds(interval);

            // 10秒経過した時点で食いしばり状態でなければ 5 回復
            if (!targetShip.IsLastStand)
            {
                targetShip.HealHP(healAmount);
            }
        }
    }

    private void OnDestroy()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
    }
}