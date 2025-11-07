using UnityEngine;

public class AvatarCustomizer : MonoBehaviour
{
    public SpriteRenderer clothingRenderer; // 衣服 Renderer
    public SpriteRenderer hairstyleRenderer; // 髮型 Renderer

    public Sprite[] clothingSprites; // 衣服圖片
    public Sprite[] hairstyleSprites; // 髮型圖片

    public void SetClothing(int clothingId)
    {
        if (clothingId >= 0 && clothingId < clothingSprites.Length)
            clothingRenderer.sprite = clothingSprites[clothingId];
    }

    public void SetHairstyle(int hairstyleId)
    {
        if (hairstyleId >= 0 && hairstyleId < hairstyleSprites.Length)
            hairstyleRenderer.sprite = hairstyleSprites[hairstyleId];
    }
}
