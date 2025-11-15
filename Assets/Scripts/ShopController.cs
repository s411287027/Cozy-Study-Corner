using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class ShopItem
{
    public string itemType;
    public int itemId;
    public int price;
    public Sprite icon;
}

public class ShopController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject signupPanel;
    public GameObject profilePanel;
    public GameObject forgetPasswordPanel;
    public GameObject notificationPanel;
    public GameObject shopPanel;

    [Header("Shop")]
    public GameObject shopItemPrefab;
    public Transform shopContent;

    [Header("User Info")]
    public TMP_Text UserCoins_Text;
    public TMP_Text UserName_Text;
    public TMP_Text UserLevel_Text;

    private FirebaseDatabaseController dbController;

    public List<ShopItem> shopItems = new List<ShopItem>();

    private void OnEnable()
    {
        // 每次啟用場景都抓 Singleton
        dbController = FirebaseDatabaseController.Instance;

        if (dbController != null)
        {
            // 避免事件重複綁定
            dbController.OnDataLoaded -= OnDataLoaded;
            dbController.OnDataLoaded += OnDataLoaded;

            // 如果資料已經存在，直接更新 UI
            if (dbController.dts != null)
                OnDataLoaded();
            else
                dbController.LoadDataFn(); // 開始讀資料
        }

        InitializeShopItems();
    }

    private void OnDisable()
    {
        // 避免事件殘留
        if (dbController != null)
            dbController.OnDataLoaded -= OnDataLoaded;
    }

    private void OnDataLoaded()
    {
        GenerateShopUI();
        UpdateCoinsUI();
        RefreshOwnedItemsUI();
    }

    private void InitializeShopItems()
    {
        // 你的 shopItems 初始化程式...
        shopItems = new List<ShopItem>
        {
            new ShopItem { itemType = "face", itemId = 1, price = 100, icon = Resources.Load<Sprite>("face_1") },
            new ShopItem { itemType = "face", itemId = 2, price = 200, icon = Resources.Load<Sprite>("face_2") },
            new ShopItem { itemType = "face", itemId = 3, price = 300, icon = Resources.Load<Sprite>("face_3") },
            new ShopItem { itemType = "face", itemId = 4, price = 400, icon = Resources.Load<Sprite>("face_4") },
            new ShopItem { itemType = "face", itemId = 5, price = 500, icon = Resources.Load<Sprite>("face_5") },
            new ShopItem { itemType = "hair", itemId = 1, price = 100, icon = Resources.Load<Sprite>("hair_1") },
            new ShopItem { itemType = "hair", itemId = 2, price = 200, icon = Resources.Load<Sprite>("hair_2") },
            new ShopItem { itemType = "hair", itemId = 3, price = 300, icon = Resources.Load<Sprite>("hair_3") },
            new ShopItem { itemType = "hair", itemId = 4, price = 400, icon = Resources.Load<Sprite>("hair_4") },
            new ShopItem { itemType = "hair", itemId = 5, price = 500, icon = Resources.Load<Sprite>("hair_5") },
            new ShopItem { itemType = "pants", itemId = 1, price = 100, icon = Resources.Load<Sprite>("pants_1") },
            new ShopItem { itemType = "pants", itemId = 2, price = 200, icon = Resources.Load<Sprite>("pants_2") },
            new ShopItem { itemType = "pants", itemId = 3, price = 300, icon = Resources.Load<Sprite>("pants_3") },
            new ShopItem { itemType = "pants", itemId = 4, price = 400, icon = Resources.Load<Sprite>("pants_4") },
            new ShopItem { itemType = "pants", itemId = 5, price = 500, icon = Resources.Load<Sprite>("pants_5") },
            new ShopItem { itemType = "shoes", itemId = 1, price = 100, icon = Resources.Load<Sprite>("shoes_1") },
            new ShopItem { itemType = "shoes", itemId = 2, price = 200, icon = Resources.Load<Sprite>("shoes_2") },
            new ShopItem { itemType = "shoes", itemId = 3, price = 300, icon = Resources.Load<Sprite>("shoes_3") },
            new ShopItem { itemType = "shoes", itemId = 4, price = 400, icon = Resources.Load<Sprite>("shoes_4") },
            new ShopItem { itemType = "shoes", itemId = 5, price = 500, icon = Resources.Load<Sprite>("shoes_5") },
            new ShopItem { itemType = "other", itemId = 1, price = 100, icon = Resources.Load<Sprite>("other_1") },
            new ShopItem { itemType = "other", itemId = 2, price = 200, icon = Resources.Load<Sprite>("other_2") },
            new ShopItem { itemType = "other", itemId = 3, price = 300, icon = Resources.Load<Sprite>("other_3") },
            new ShopItem { itemType = "other", itemId = 4, price = 400, icon = Resources.Load<Sprite>("other_4") },
            new ShopItem { itemType = "other", itemId = 5, price = 500, icon = Resources.Load<Sprite>("other_5") },
            new ShopItem { itemType = "furniture", itemId = 1, price = 100, icon = Resources.Load<Sprite>("Bed-1-1") },
            new ShopItem { itemType = "furniture", itemId = 2, price = 100, icon = Resources.Load<Sprite>("Book-1-1") },
            new ShopItem { itemType = "furniture", itemId = 3, price = 100, icon = Resources.Load<Sprite>("Book-2-1") },
            new ShopItem { itemType = "furniture", itemId = 4, price = 100, icon = Resources.Load<Sprite>("Cabinet-1-1") },
            new ShopItem { itemType = "furniture", itemId = 5, price = 100, icon = Resources.Load<Sprite>("Carpet-1-1") },
            new ShopItem { itemType = "furniture", itemId = 6, price = 100, icon = Resources.Load<Sprite>("Chair-1-1") },
            new ShopItem { itemType = "furniture", itemId = 7, price = 100, icon = Resources.Load<Sprite>("Clock-1-1") },
            new ShopItem { itemType = "furniture", itemId = 8, price = 100, icon = Resources.Load<Sprite>("Closet-1-1") },
            new ShopItem { itemType = "furniture", itemId = 9, price = 100, icon = Resources.Load<Sprite>("Cup-1-1") },
            new ShopItem { itemType = "furniture", itemId = 10, price = 100, icon = Resources.Load<Sprite>("Desk-1-1") },
            new ShopItem { itemType = "furniture", itemId = 11, price = 100, icon = Resources.Load<Sprite>("Doll-1-1") },
            new ShopItem { itemType = "furniture", itemId = 12, price = 100, icon = Resources.Load<Sprite>("Lamp-1-1") },
            new ShopItem { itemType = "furniture", itemId = 13, price = 100, icon = Resources.Load<Sprite>("Pillow-1-1") },
            new ShopItem { itemType = "furniture", itemId = 14, price = 100, icon = Resources.Load<Sprite>("Plant-1-1") },
            new ShopItem { itemType = "furniture", itemId = 15, price = 100, icon = Resources.Load<Sprite>("Plant-2-1") },
            new ShopItem { itemType = "furniture", itemId = 16, price = 100, icon = Resources.Load<Sprite>("Shelf-1-1") },
            new ShopItem { itemType = "furniture", itemId = 17, price = 100, icon = Resources.Load<Sprite>("Shelf-2-1") },
            new ShopItem { itemType = "furniture", itemId = 18, price = 100, icon = Resources.Load<Sprite>("Sofa-1-1") },
            new ShopItem { itemType = "furniture", itemId = 19, price = 100, icon = Resources.Load<Sprite>("Table-1-1") },
            new ShopItem { itemType = "furniture", itemId = 20, price = 100, icon = Resources.Load<Sprite>("Toy-1-1") },
            new ShopItem { itemType = "furniture", itemId = 21, price = 100, icon = Resources.Load<Sprite>("Window-1-1") },
            new ShopItem { itemType = "furniture", itemId = 22, price = 100, icon = Resources.Load<Sprite>("Window-2-1") }

        };
    }

    private void GenerateShopUI()
    {
        if (shopContent == null || shopItemPrefab == null) return;

        foreach (Transform child in shopContent)
            Destroy(child.gameObject);

        foreach (var item in shopItems)
        {
            GameObject obj = Instantiate(shopItemPrefab, shopContent);
            ShopItemUI ui = obj.GetComponent<ShopItemUI>();
            if (ui != null)
                ui.Setup(item, this);
        }
    }

    public void BuyItem(ShopItem item)
    {
        if (dbController == null || dbController.dts == null) return;

        List<int> ownedList = dbController.dts.ownedItems.GetList(item.itemType);
        if (ownedList == null)
        {
            Debug.LogError($"Item type '{item.itemType}' not found.");
            return;
        }

        if (dbController.dts.TotalCoins >= item.price)
        {
            if (!ownedList.Contains(item.itemId))
            {
                dbController.UpdatePurchase(item.itemType, item.itemId, item.price);
                UpdateCoinsUI();
                RefreshOwnedItemsUI();
            }
            else
            {
                Debug.Log("⚠ 已經擁有此物品");
            }
        }
        else
        {
            Debug.Log("❌ 金幣不足");
        }
    }

    private void RefreshOwnedItemsUI()
    {
        if (shopContent == null || dbController == null || dbController.dts == null) return;

        foreach (Transform child in shopContent)
        {
            var ui = child.GetComponent<ShopItemUI>();
            if (ui != null)
            {
                var ownedList = dbController.dts.ownedItems.GetList(ui.item.itemType);
                bool alreadyOwned = ownedList != null && ownedList.Contains(ui.item.itemId);
                ui.SetPurchased(alreadyOwned);
            }
        }
    }

    public void UpdateCoinsUI()
    {
        if (UserCoins_Text != null)
            UserCoins_Text.text = dbController != null && dbController.dts != null ? dbController.dts.TotalCoins.ToString() : "Loading...";
        if (UserName_Text != null)
            UserName_Text.text = dbController != null && dbController.dts != null ? dbController.dts.UserName : "Loading...";
        if (UserLevel_Text != null)
            UserLevel_Text.text = dbController != null && dbController.dts != null ? dbController.dts.CrrLevel.ToString() : "Loading...";
    }

    public void OpenShopPanel()
    {
        // 隱藏其他面板
        loginPanel?.SetActive(false);
        signupPanel?.SetActive(false);
        profilePanel?.SetActive(false);
        forgetPasswordPanel?.SetActive(false);

        // 顯示商店
        shopPanel?.SetActive(true);

        // 生成 UI
        GenerateShopUI();
        RefreshOwnedItemsUI();
        UpdateCoinsUI();
    }
}


