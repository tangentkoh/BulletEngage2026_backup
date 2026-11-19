using UnityEngine;

public class SigureJammer : MonoBehaviour, IAssistantJammer
{
    [SerializeField] private GameObject paintPrefab;
    [SerializeField] private float marginX = 1.5f;
    [SerializeField] private float marginY = 1.5f;

    public void TriggerJammer(PlayField targetField)
    {
        if (targetField == null || paintPrefab == null) return;

        // 端を除外したランダム座標
        float minX = targetField.bounds.xMin + marginX;
        float maxX = targetField.bounds.xMax - marginX;
        float minY = targetField.bounds.yMin + marginY;
        float maxY = targetField.bounds.yMax - marginY;

        Vector3 spawnPos = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
        Instantiate(paintPrefab, spawnPos, Quaternion.identity);

        Destroy(gameObject);
    }
}