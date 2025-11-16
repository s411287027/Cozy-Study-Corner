using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShirtSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject shirtButtonPrefab;
    public Transform content;

    [Header("Player 設定")]
    public Transform playerDisplayPosition;
    private GameObject player;
    private ShirtController shirtController;

    [Header("所有可選衣服")]
    public List<ShirtData> shirtList = new List<ShirtData>();

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;

        if (player == null)
        {
            Debug.LogError("找不到 Player !");
            return;
        }

        shirtController = player.GetComponentInChildren<ShirtController>();

        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        GenerateShirtUI();
    }

    void GenerateShirtUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var shirt in shirtList)
        {
            GameObject obj = Instantiate(shirtButtonPrefab, content);
            ShirtButtonUI ui = obj.GetComponent<ShirtButtonUI>();
            ui.Setup(shirt, this);
        }
    }

    public void SelectShirt(ShirtData shirt)
    {
        if (shirtController == null) return;

        shirtController.shirtUp = shirt.shirtUp;
        shirtController.shirtDown = shirt.shirtDown;
        shirtController.shirtLeft = shirt.shirtLeft;
        shirtController.shirtRight = shirt.shirtRight;

        shirtController.shirtUpFrames = shirt.shirtUpFrames;
        shirtController.shirtDownFrames = shirt.shirtDownFrames;
        shirtController.shirtLeftFrames = shirt.shirtLeftFrames;
        shirtController.shirtRightFrames = shirt.shirtRightFrames;

        shirtController.UpdateShirtDirection(0, -1);

        Debug.Log($"成功替換衣服：{shirt.shirtName}");
    }
}
