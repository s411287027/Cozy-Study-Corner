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
            new ShopItem { itemType = "face", itemId = 1, price = 100, icon = Resources.Load<Sprite>("Wardrobe/face/1") },
            new ShopItem { itemType = "face", itemId = 2, price = 200, icon = Resources.Load<Sprite>("Wardrobe/face/2") },
            new ShopItem { itemType = "face", itemId = 3, price = 300, icon = Resources.Load<Sprite>("Wardrobe/face/3") },
            new ShopItem { itemType = "face", itemId = 4, price = 400, icon = Resources.Load<Sprite>("Wardrobe/face/4") },
            new ShopItem { itemType = "face", itemId = 5, price = 500, icon = Resources.Load<Sprite>("Wardrobe/face/5") },
            new ShopItem { itemType = "face", itemId = 6, price = 400, icon = Resources.Load<Sprite>("Wardrobe/face/6") },
            new ShopItem { itemType = "face", itemId = 7, price = 500, icon = Resources.Load<Sprite>("Wardrobe/face/7") },
            new ShopItem { itemType = "hair", itemId = 1, price = 100, icon = Resources.Load<Sprite>("Wardrobe/hair/1") },
            new ShopItem { itemType = "hair", itemId = 2, price = 200, icon = Resources.Load<Sprite>("Wardrobe/hair/2") },
            new ShopItem { itemType = "hair", itemId = 3, price = 300, icon = Resources.Load<Sprite>("Wardrobe/hair/3") },
            new ShopItem { itemType = "hair", itemId = 4, price = 400, icon = Resources.Load<Sprite>("Wardrobe/hair/4") },
            new ShopItem { itemType = "hair", itemId = 5, price = 500, icon = Resources.Load<Sprite>("Wardrobe/hair/5") },
            new ShopItem { itemType = "hair", itemId = 6, price = 100, icon = Resources.Load<Sprite>("Wardrobe/hair/6") },
            new ShopItem { itemType = "hair", itemId = 7, price = 200, icon = Resources.Load<Sprite>("Wardrobe/hair/7") },
            new ShopItem { itemType = "hair", itemId = 8, price = 300, icon = Resources.Load<Sprite>("Wardrobe/hair/8") },
            new ShopItem { itemType = "hair", itemId = 9, price = 400, icon = Resources.Load<Sprite>("Wardrobe/hair/9") },
            new ShopItem { itemType = "hair", itemId = 10, price = 500, icon = Resources.Load<Sprite>("Wardrobe/hair/10") },
            new ShopItem { itemType = "hair", itemId = 11, price = 100, icon = Resources.Load<Sprite>("Wardrobe/hair/11") },
            new ShopItem { itemType = "hair", itemId = 12, price = 200, icon = Resources.Load<Sprite>("Wardrobe/hair/12") },
            new ShopItem { itemType = "pants", itemId = 1, price = 100, icon = Resources.Load<Sprite>("Wardrobe/pants/1") },
            new ShopItem { itemType = "pants", itemId = 2, price = 200, icon = Resources.Load<Sprite>("Wardrobe/pants/2") },
            new ShopItem { itemType = "pants", itemId = 3, price = 300, icon = Resources.Load<Sprite>("Wardrobe/pants/3") },
            new ShopItem { itemType = "pants", itemId = 4, price = 400, icon = Resources.Load<Sprite>("Wardrobe/pants/4") },
            new ShopItem { itemType = "pants", itemId = 5, price = 500, icon = Resources.Load<Sprite>("Wardrobe/pants/5") },
            new ShopItem { itemType = "pants", itemId = 6, price = 300, icon = Resources.Load<Sprite>("Wardrobe/pants/6") },
            new ShopItem { itemType = "pants", itemId = 7, price = 400, icon = Resources.Load<Sprite>("Wardrobe/pants/7") },
            new ShopItem { itemType = "pants", itemId = 8, price = 500, icon = Resources.Load<Sprite>("Wardrobe/pants/8") },
            new ShopItem { itemType = "shirt", itemId = 1, price = 100, icon = Resources.Load<Sprite>("Wardrobe/shirt/1") },
            new ShopItem { itemType = "shirt", itemId = 2, price = 200, icon = Resources.Load<Sprite>("Wardrobe/shirt/2") },
            new ShopItem { itemType = "shirt", itemId = 3, price = 300, icon = Resources.Load<Sprite>("Wardrobe/shirt/3") },
            new ShopItem { itemType = "shirt", itemId = 4, price = 400, icon = Resources.Load<Sprite>("Wardrobe/shirt/4") },
            new ShopItem { itemType = "shirt", itemId = 5, price = 500, icon = Resources.Load<Sprite>("Wardrobe/shirt/5") },
            new ShopItem { itemType = "shirt", itemId = 6, price = 300, icon = Resources.Load<Sprite>("Wardrobe/shirt/6") },
            new ShopItem { itemType = "shirt", itemId = 7, price = 400, icon = Resources.Load<Sprite>("Wardrobe/shirt/7") },
            new ShopItem { itemType = "shirt", itemId = 8, price = 500, icon = Resources.Load<Sprite>("Wardrobe/shirt/8") },
            new ShopItem { itemType = "shoes", itemId = 1, price = 100, icon = Resources.Load<Sprite>("Wardrobe/shoes/1") },
            new ShopItem { itemType = "shoes", itemId = 2, price = 200, icon = Resources.Load<Sprite>("Wardrobe/shoes/2") },
            new ShopItem { itemType = "shoes", itemId = 3, price = 300, icon = Resources.Load<Sprite>("Wardrobe/shoes/3") },
            new ShopItem { itemType = "shoes", itemId = 4, price = 400, icon = Resources.Load<Sprite>("Wardrobe/shoes/4") },
            new ShopItem { itemType = "shoes", itemId = 5, price = 500, icon = Resources.Load<Sprite>("Wardrobe/shoes/5") },
            new ShopItem { itemType = "shoes", itemId = 6, price = 300, icon = Resources.Load<Sprite>("Wardrobe/shoes/6") },
            new ShopItem { itemType = "shoes", itemId = 7, price = 400, icon = Resources.Load<Sprite>("Wardrobe/shoes/7") },
            new ShopItem { itemType = "shoes", itemId = 8, price = 500, icon = Resources.Load<Sprite>("Wardrobe/shoes/8") },
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

        // 取得本地端該類別的擁有清單
        List<int> ownedList = dbController.dts.ownedItems.GetList(item.itemType);
        if (ownedList == null)
        {
            Debug.LogError($"Item type '{item.itemType}' not found.");
            return;
        }

        // 1. 檢查金幣是否足夠
        if (dbController.dts.TotalCoins >= item.price)
        {
            // 2. 檢查是否尚未擁有
            if (!ownedList.Contains(item.itemId))
            {
                // 🔥 [修改 1] 先扣除本地端金幣 (Local Update)
                //dbController.dts.TotalCoins -= item.price;

                // 🔥 [修改 2] 先將物品加入本地清單 (Local Update)
                // 這樣 RefreshOwnedItemsUI 才能馬上把按鈕變灰色
                ownedList.Add(item.itemId);

                // 🔥 [修改 3] 呼叫 Firebase 進行雲端存檔 (Background Sync)
                // 這裡只負責送出請求，不需要等它回來 UI 就已經變了
                dbController.UpdatePurchase(item.itemType, item.itemId, item.price);

                // 🔥 [修改 4] 立即更新商店介面 (Coin & Buttons)
                UpdateCoinsUI();
                RefreshOwnedItemsUI();

                // 🔥 [修改 5] 同步通知 ProfileUIController 更新 (如果有掛載的話)
                var profileUI = FindObjectOfType<ProfileUIController>();
                if (profileUI != null)
                {
                    profileUI.UpdateUI();
                }

                Debug.Log("✅ 購買成功 (本地已更新，正在同步雲端)");
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


