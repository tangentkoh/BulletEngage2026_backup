using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] private PlayerShip playerShip;

    private void Awake()
    {
        if (playerShip == null)
        {
            playerShip = GetComponentInParent<PlayerShip>();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            playerShip.TakeDamage(10f);
        }
    }
}