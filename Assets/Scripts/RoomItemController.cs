using UnityEngine;
using System;
using System.Collections.Generic;

public class RoomItemController : MonoBehaviour
{
    [Serializable]
    public class FurnitureItem
    {
        public int itemId;          // 對應家具的 ID（Database 裡 List<int> 的值）
        public GameObject itemObject; // 場景中的家具物件
    }

    public List<FurnitureItem> roomFurniture; // 在 Inspector 綁定家具 ID 與 GameObject

    private void Start()
    {
        // 如果資料已經載入
        if (FirebaseDatabaseController.Instance.dts != null)
        {
            UpdateFurnitureDisplay();
        }

        // 監聽資料載入事件
        FirebaseDatabaseController.Instance.OnDataLoaded += UpdateFurnitureDisplay;
    }

    private void UpdateFurnitureDisplay()
    {
        var ownedFurniture = FirebaseDatabaseController.Instance.dts.ownedItems.furniture;

        foreach (var furniture in roomFurniture)
        {
            if (ownedFurniture.Contains(furniture.itemId))
            {
                furniture.itemObject.SetActive(true);
            }
            else
            {
                furniture.itemObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (FirebaseDatabaseController.Instance != null)
            FirebaseDatabaseController.Instance.OnDataLoaded -= UpdateFurnitureDisplay;
    }
}
