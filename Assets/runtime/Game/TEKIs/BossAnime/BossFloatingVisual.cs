using UnityEngine;

public class BossFloatingVisual : MonoBehaviour
{
    [Header("浮遊設定")]
    [SerializeField] private float floatAmplitude = 0.12f; // 上下の揺れ幅
    [SerializeField] private float floatFrequency = 2.0f;   // 揺れる速度

    private Vector3 initialLocalPos;

    private void Start()
    {
        initialLocalPos = transform.localPosition;
    }

    private void Update()
    {
        float offsetY = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.localPosition = initialLocalPos + new Vector3(0f, offsetY, 0f);
    }
}