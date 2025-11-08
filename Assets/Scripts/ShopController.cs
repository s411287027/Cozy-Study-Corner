using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    public GameObject loginPanel, signupPanel, profilePanel, forgetPasswordPanel, notificationPanel, shopPanel;
    public FirebaseDatabaseController dbController;
    public GameObject shopItemPrefab;
    public Transform shopContent;
    public TMP_Text UserCoins_Text, UserName_Text, UserLevel_Text;

    public List<ShopItem> shopItems = new List<ShopItem>();

    void Awake()
    {
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

    void Start()
    {
        GenerateShopUI();
        dbController.OnDataLoaded += () =>
        {
            UpdateCoinsUI();
            RefreshOwnedItemsUI();
        };
    }

    void GenerateShopUI()
    {
        foreach (Transform child in shopContent)
            Destroy(child.gameObject);

        foreach (var item in shopItems)
        {
            GameObject obj = Instantiate(shopItemPrefab, shopContent);
            ShopItemUI ui = obj.GetComponent<ShopItemUI>();
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
            List<int> OwnedList = dbController.dts.ownedItems.GetList(item.itemType);

            if (!OwnedList.Contains(item.itemId))
            {
                // 🔹 改成這樣（不再呼叫 SaveDataFn）
                dbController.UpdatePurchase(item.itemType, item.itemId, item.price);

                UpdateCoinsUI();

                // ✅ 灰化購買的物品 UI
                foreach (Transform child in shopContent)
                {
                    var ui = child.GetComponent<ShopItemUI>();
                    if (ui != null && ui.item.itemId == item.itemId && ui.item.itemType == item.itemType)
                    {
                        ui.SetPurchased(true);
                        Debug.Log($"✅ 灰化成功: {item.itemType} {item.itemId}");
                    }
                }
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

    void RefreshOwnedItemsUI()
    {
        if (dbController == null || dbController.dts == null) return;

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
        if (dbController != null && dbController.dts != null)
        {
            UserCoins_Text.text = dbController.dts.TotalCoins.ToString();
            UserName_Text.text = dbController.dts.UserName;
            UserLevel_Text.text = dbController.dts.CrrLevel.ToString();
        }
        else
        {
            UserCoins_Text.text = "Loading...";
            UserName_Text.text = "Loading...";
            UserLevel_Text.text = "Loading...";
        }
    }

    public void OpenShopPanel()
    {
        Scene sceneA = SceneManager.GetSceneByName("CozyStudyCorner");
        foreach (var rootObj in sceneA.GetRootGameObjects())
        {
            Canvas canvas = rootObj.GetComponentInChildren<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 2; // 高於 SceneA
        }
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        forgetPasswordPanel.SetActive(false);
        shopPanel.SetActive(true);
        dbController.LoadDataFn();
    }
}
