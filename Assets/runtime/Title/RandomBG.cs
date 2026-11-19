using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomBG : MonoBehaviour
{
    [Header("背景画像リスト")]
    [SerializeField] private List<Sprite> backgroundSprites = new List<Sprite>();

    [Header("適用先 (未設定の場合はこのオブジェクトから自動取得)")]
    [SerializeField] private Image targetUIImage;

    private void Awake()
    {
        if (targetUIImage == null) targetUIImage = GetComponent<Image>();

        SetRandomBackground();
    }

    /// <summary>
    /// リストからランダムに背景画像を選んで反映する
    /// </summary>
    public void SetRandomBackground()
    {
        if (backgroundSprites == null || backgroundSprites.Count == 0)
        {
            Debug.LogWarning("[RandomBackgroundSelector] 背景画像リストが空です！");
            return;
        }

        // ランダム抽出
        int randomIndex = Random.Range(0, backgroundSprites.Count);
        Sprite selectedSprite = backgroundSprites[randomIndex];

        // UI Image への反映
        if (targetUIImage != null)
        {
            targetUIImage.sprite = selectedSprite;
        }
    }
}