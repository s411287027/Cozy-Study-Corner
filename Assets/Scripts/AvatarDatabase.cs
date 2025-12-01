using UnityEngine;

// 這行屬性讓我們可以在 Project 視窗按右鍵建立這個檔案
[CreateAssetMenu(fileName = "NewAvatarDatabase", menuName = "Game/Avatar Database")]
public class AvatarDatabase : ScriptableObject
{
    [Header("頭部資源")]
    public Sprite[] hairSprites;
    public Sprite[] faceSprites;

    [Header("身體資源")]
    public Sprite[] shirtSprites;
    public Sprite[] sleeveSprites;
    public Sprite[] pantsSprites;
    public Sprite[] shoesSprites;

    // === 讀取圖片的公開方法 ===
    public Sprite GetHair(int id) => GetSpriteSafe(hairSprites, id);
    public Sprite GetFace(int id) => GetSpriteSafe(faceSprites, id);
    public Sprite GetShirt(int id) => GetSpriteSafe(shirtSprites, id);
    public Sprite GetSleeve(int id) => GetSpriteSafe(sleeveSprites, id);
    public Sprite GetPants(int id) => GetSpriteSafe(pantsSprites, id);
    public Sprite GetShoes(int id) => GetSpriteSafe(shoesSprites, id);

    // 私有輔助函式：確保 ID 不會超出範圍報錯
    private Sprite GetSpriteSafe(Sprite[] list, int id)
    {
        if (list == null || list.Length == 0) return null;
        if (id < 0 || id >= list.Length) return list[0]; // 如果 ID 不存在，預設回傳第 0 張
        return list[id];
    }
}