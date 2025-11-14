using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HairSelectionUI : MonoBehaviour
{
    [Header("UI 參考")]
    public GameObject hairButtonPrefab;       // 單一髮型按鈕 prefab
    public Transform hairButtonContainer;     // 放髮型按鈕的父物件
    public Button openHairMenuButton;         // 「Hair」按鈕

    [Header("可選髮型清單")]
    public List<HairData> availableHairs = new List<HairData>();

    [Header("右側 Player 的相機或定位")]
    public Transform playerDisplayPosition;   // 顯示 player 的位置（可在場景裡擺）

    private GameObject playerInstance;
    private HairController hairController;
    private bool menuOpen = false;

    void Start()
    {
        // 找到目前 Player（從 PlayerManager）
        playerInstance = PlayerManager.Instance?.playerInstance;
        if (playerInstance == null)
        {
            Debug.LogError("❌ 找不到 Player 實例！");
            return;
        }

        hairController = playerInstance.GetComponentInChildren<HairController>();

        // 移動 Player 到畫面右半邊顯示
        if (playerDisplayPosition != null)
            playerInstance.transform.position = playerDisplayPosition.position;

        // 關閉清單
        hairButtonContainer.gameObject.SetActive(false);

        // 綁定開關按鈕
        if (openHairMenuButton != null)
            openHairMenuButton.onClick.AddListener(ToggleHairMenu);

        // 建立所有髮型按鈕
        PopulateHairButtons();
    }

    void ToggleHairMenu()
    {
        menuOpen = !menuOpen;
        hairButtonContainer.gameObject.SetActive(menuOpen);
    }

    void PopulateHairButtons()
    {
        // 清除原本的按鈕
        foreach (Transform child in hairButtonContainer)
            Destroy(child.gameObject);

        // 動態生成每個髮型按鈕
        foreach (var hair in availableHairs)
        {
            var btnObj = Instantiate(hairButtonPrefab, hairButtonContainer);
            var txt = btnObj.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = hair.hairName;

            var img = btnObj.GetComponentInChildren<Image>();
            if (img != null && hair.hairDown != null)
                img.sprite = hair.hairDown;

            var button = btnObj.GetComponent<Button>();
            button.onClick.AddListener(() => OnSelectHair(hair));
        }
    }

    void OnSelectHair(HairData newHair)
    {
        if (hairController == null)
        {
            Debug.LogError("⚠ HairController 不存在於 Player 身上。");
            return;
        }

        // 套用新的髮型資料
        hairController.hairUp = newHair.hairUp;
        hairController.hairDown = newHair.hairDown;
        hairController.hairLeft = newHair.hairLeft;
        hairController.hairRight = newHair.hairRight;

        hairController.hairUpFrames = newHair.hairUpFrames;
        hairController.hairDownFrames = newHair.hairDownFrames;
        hairController.hairLeftFrames = newHair.hairLeftFrames;
        hairController.hairRightFrames = newHair.hairRightFrames;

        // 立即更新顯示
        hairController.UpdateHairDirection(0, -1);

        Debug.Log($"🎀 已換髮型：{newHair.hairName}");
    }
}
