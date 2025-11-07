using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text priceText, statusText;
    public Button buyButton;

    [HideInInspector] public ShopItem item;
    private ShopController shopController;

    public void Setup(ShopItem shopItem, ShopController controller)
    {
        item = shopItem;
        shopController = controller;

        if (iconImage != null)
            iconImage.sprite = item.icon;

        if (priceText != null)
            priceText.text = item.price.ToString();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                shopController.BuyItem(item);
            });
        }

        // 預設恢復互動狀態
        SetPurchased(false);
    }

    public void SetPurchased(bool isPurchased)
    {
        if (buyButton != null)
        {
            buyButton.interactable = !isPurchased;

            // 灰化按鈕底圖
            var btnImage = buyButton.GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = isPurchased ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;
        }

        // 灰化圖示
        if (iconImage != null)
            iconImage.color = isPurchased ? new Color(1f, 1f, 1f, 0.5f) : Color.white;

        // 顯示文字提示
        if (statusText != null)
        {
            statusText.text = isPurchased ? "" : "";
            statusText.color = isPurchased ? Color.gray : Color.white;
        }
    }

    // 提供無參數呼叫
    public void SetPurchased()
    {
        SetPurchased(true);
    }
}
