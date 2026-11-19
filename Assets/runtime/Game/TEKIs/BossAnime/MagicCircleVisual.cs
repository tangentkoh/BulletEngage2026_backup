using UnityEngine;

public class MagicCircleVisual : MonoBehaviour
{
    [Header("回転速度 (度/秒)")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(20f, 35f, 60f);

    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}