using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

public class FriendRoomController : MonoBehaviour
{
    public string friendUID;
    public Transform furnitureParent; // 必須在 Canvas 下
    public GameObject friendRoomPanel;

    private DatabaseReference dbRef;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        if (friendRoomPanel != null)
            friendRoomPanel.SetActive(false); // 一開始隱藏
    }

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
        // 1️⃣ 清空舊的家具
        foreach (Transform child in furnitureParent)
            Destroy(child.gameObject);

        // 2️⃣ 定義家具的層級表
        Dictionary<int, int> furnitureSortOrder = new Dictionary<int, int>()
    {
        { 1, 0 },
        { 2, 2 },
        { 3, 1 },
        { 4, 1 },
        { 5, 0 },
        { 6, 0 },
        { 7, 2 },
        { 8, 1 },
        { 9, 3 },
        { 10, 1 },
        { 11, 1 },
        { 12, 2 },
        { 13, 1 },
        { 14, 1 },
        { 15, 3 },
        { 16, 0 },
        { 17, 0 },
        { 18, 1 },
        { 19, 2 },
        { 20, 1 },
        { 21, 0 },
        { 22, 0 }
    };

        // 3️⃣ 依層級排序（數字小的在前面）
        furnitureIds.Sort((a, b) =>
        {
            int orderA = furnitureSortOrder.ContainsKey(a) ? furnitureSortOrder[a] : 0;
            int orderB = furnitureSortOrder.ContainsKey(b) ? furnitureSortOrder[b] : 0;
            return orderA.CompareTo(orderB);
        });

        // 4️⃣ 按照排序結果生成家具
        for (int i = 0; i < furnitureIds.Count; i++)
        {
            int id = furnitureIds[i];
            Sprite sprite = Resources.Load<Sprite>($"FriendRoomResources/{id}");
            if (sprite == null)
            {
                Debug.LogWarning($"Sprite not found: {id}");
                continue;
            }

            GameObject newItem = new GameObject($"Furniture_{id}", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            newItem.transform.SetParent(furnitureParent, false);

            UnityEngine.UI.Image img = newItem.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = Color.white;

            RectTransform rt = newItem.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1460f, 1095f);
            rt.localScale = Vector3.one;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            // 🧩 關鍵在這裡：根據排序確保顯示順序正確
            int sortOrder = furnitureSortOrder.ContainsKey(id) ? furnitureSortOrder[id] : 0;
            newItem.transform.SetSiblingIndex(i);  // 確保依生成順序顯示
        }
    }



    public void CloseFriendRoom()
    {
        friendRoomPanel.SetActive(false);
    }
}
