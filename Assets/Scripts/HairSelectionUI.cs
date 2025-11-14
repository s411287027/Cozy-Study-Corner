using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HairSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject hairButtonPrefab;       // 髮型按鈕 prefab
    public Transform content;                 // ScrollView Content

    [Header("Player 設定")]
    public Transform playerDisplayPosition;   // Player 顯示位置（右側）
    private GameObject player;
    private HairController hairController;

    [Header("所有可選髮型")]
    public List<HairData> hairList = new List<HairData>();

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;

        if (player == null)
        {
            Debug.LogError("找不到 Player !");
            return;
        }

        hairController = player.GetComponentInChildren<HairController>();

        // 移動到展示位置
        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        GenerateHairUI();
    }

    void GenerateHairUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var hair in hairList)
        {
            GameObject obj = Instantiate(hairButtonPrefab, content);
            HairButtonUI ui = obj.GetComponent<HairButtonUI>();
            ui.Setup(hair, this);
        }
    }

    public void SelectHair(HairData hair)
    {
        if (hairController == null) return;

        // 替換髮型
        hairController.hairUp = hair.hairUp;
        hairController.hairDown = hair.hairDown;
        hairController.hairLeft = hair.hairLeft;
        hairController.hairRight = hair.hairRight;

        hairController.hairUpFrames = hair.hairUpFrames;
        hairController.hairDownFrames = hair.hairDownFrames;
        hairController.hairLeftFrames = hair.hairLeftFrames;
        hairController.hairRightFrames = hair.hairRightFrames;

        hairController.UpdateHairDirection(0, -1);

        Debug.Log($"成功替換髮型：{hair.hairName}");
    }
}
