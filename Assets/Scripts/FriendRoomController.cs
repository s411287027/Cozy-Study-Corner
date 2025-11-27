using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

public class FriendRoomController : MonoBehaviour
{
    public string friendUID;

    [Header("Furniture Settings")]
    public Transform furnitureParent; // 必須在 Canvas 下
    public GameObject friendRoomPanel;

    [Header("Wardrobe Settings")]
    public GameObject friendWardrobePanel; // 新增：衣櫃介面面板
    public Transform wardrobeContentParent; // 新增：生成衣服Icon的父物件 (建議加 GridLayoutGroup)

    private DatabaseReference dbRef;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        if (friendRoomPanel != null) friendRoomPanel.SetActive(false);
        if (friendWardrobePanel != null) friendWardrobePanel.SetActive(false); // 預設隱藏衣櫃
    }

    // ================== 原有家具功能 ==================

    public void OpenFriendRoomPanel()
    {
        if (friendRoomPanel != null)
            friendRoomPanel.SetActive(true);
    }

    public void LoadFriendFurniture()
    {
        if (string.IsNullOrEmpty(friendUID)) return;

        dbRef.Child("users").Child(friendUID).Child("ownedItems").Child("furniture")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    List<int> ownedFurniture = new List<int>();
                    foreach (var child in task.Result.Children)
                    {
                        int value = int.Parse(child.Value.ToString());
                        if (value != -1)
                            ownedFurniture.Add(value);
                    }

                    DisplayFurniture(ownedFurniture);
                }
            });
    }

    private void DisplayFurniture(List<int> furnitureIds)
    {
        foreach (Transform child in furnitureParent)
            Destroy(child.gameObject);

        Dictionary<int, int> furnitureSortOrder = new Dictionary<int, int>()
        {
            { 1, 0 }, { 2, 2 }, { 3, 1 }, { 4, 1 }, { 5, 0 },
            { 6, 0 }, { 7, 2 }, { 8, 1 }, { 9, 3 }, { 10, 1 },
            { 11, 1 }, { 12, 2 }, { 13, 1 }, { 14, 1 }, { 15, 3 },
            { 16, 0 }, { 17, 0 }, { 18, 1 }, { 19, 2 }, { 20, 1 },
            { 21, 0 }, { 22, 0 }
        };

        furnitureIds.Sort((a, b) =>
        {
            int orderA = furnitureSortOrder.ContainsKey(a) ? furnitureSortOrder[a] : 0;
            int orderB = furnitureSortOrder.ContainsKey(b) ? furnitureSortOrder[b] : 0;
            return orderA.CompareTo(orderB);
        });

        for (int i = 0; i < furnitureIds.Count; i++)
        {
            int id = furnitureIds[i];
            // 注意：這裡讀取的是家具的資源
            Sprite sprite = Resources.Load<Sprite>($"FriendRoomResources/{id}");
            if (sprite == null) continue;

            GameObject newItem = new GameObject($"Furniture_{id}", typeof(RectTransform), typeof(Image));
            newItem.transform.SetParent(furnitureParent, false);

            Image img = newItem.GetComponent<Image>();
            img.sprite = sprite;

            RectTransform rt = newItem.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1460f, 1095f);
            rt.anchoredPosition = Vector2.zero;

            newItem.transform.SetSiblingIndex(i);
        }
    }

    public void CloseFriendRoom()
    {
        friendRoomPanel.SetActive(false);
        CloseFriendWardrobe(); // 關閉房間時順便關閉衣櫃
    }

    // ================== 新增：衣櫃功能 (Wardrobe) ==================

    // 1️⃣ 按鈕呼叫此函式打開衣櫃
    public void OpenFriendWardrobe()
    {
        if (friendWardrobePanel != null)
        {
            friendWardrobePanel.SetActive(true);
            LoadFriendWardrobeData();
        }
    }

    // 2️⃣ 關閉衣櫃
    public void CloseFriendWardrobe()
    {
        if (friendWardrobePanel != null)
            friendWardrobePanel.SetActive(false);
    }

    // 3️⃣ 從 Firebase 讀取資料
    private void LoadFriendWardrobeData()
    {
        if (string.IsNullOrEmpty(friendUID)) return;

        // 我們需要同時讀取 ownedItems 下的多個節點，直接讀取 ownedItems 整包比較快
        dbRef.Child("users").Child(friendUID).Child("ownedItems")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    DataSnapshot snapshot = task.Result;
                    DisplayWardrobeItems(snapshot);
                }
                else
                {
                    Debug.Log("找不到該玩家的擁有物品資料");
                }
            });
    }

    // 4️⃣ 顯示衣櫃物品 (這段邏輯不變，保持遍歷 face, hair 等類別)
    private void DisplayWardrobeItems(DataSnapshot ownedItemsSnapshot)
    {
        // 先清空舊的顯示內容
        foreach (Transform child in wardrobeContentParent)
            Destroy(child.gameObject);

        // 這些名稱必須跟你的資料庫欄位名稱、以及 Unity Resources 資料夾名稱一致
        string[] categories = { "face", "hair", "shirt", "pants", "shoes" };

        foreach (string category in categories)
        {
            if (ownedItemsSnapshot.HasChild(category))
            {
                DataSnapshot categorySnapshot = ownedItemsSnapshot.Child(category);

                // 你的資料庫結構 face: 1, 3, 7 在 Firebase 中通常會是：
                // face -> { "0": 1, "1": 3, "2": 7 }
                // 所以我們遍歷 Children 取出 Value 即可
                foreach (var item in categorySnapshot.Children)
                {
                    int itemId = int.Parse(item.Value.ToString());

                    // 呼叫生成函式，傳入 ID 和 類別
                    CreateWardrobeItemUI(itemId, category);
                }
            }
        }
    }

    // 5️⃣ 生成單個物品 UI (這裡修改路徑邏輯)
    private void CreateWardrobeItemUI(int id, string category)
    {
        // 🔹 修改重點：路徑加上 category
        // 假設你的圖片放在 Resources/Wardrobe/face/1
        string resourcePath = $"Wardrobe/{category}/{id}";

        Sprite sprite = Resources.Load<Sprite>(resourcePath);

        if (sprite == null)
        {
            // 這裡可以幫你除錯，如果沒找到圖片會顯示路徑
            Debug.LogWarning($"找不到圖片，路徑是: {resourcePath}");
            return;
        }

        // 生成物件
        GameObject newItem = new GameObject($"{category}_{id}", typeof(RectTransform), typeof(Image));
        newItem.transform.SetParent(wardrobeContentParent, false);

        // 設定圖片
        Image img = newItem.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true; // 保持原本圖片比例，不會被拉伸

        // 如果你有用 GridLayoutGroup，它會自動控制大小
        // 如果沒有，你可以手動設定一個固定大小，例如 100x100
        // RectTransform rt = newItem.GetComponent<RectTransform>();
        // rt.sizeDelta = new Vector2(150, 150); 
    }
}