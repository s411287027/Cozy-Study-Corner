using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShoesSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject shoesButtonPrefab;
    public Transform content;

    [Header("Player 設定")]
    public Transform playerDisplayPosition;
    private GameObject player;
    private ShoesController shoesController;

    [Header("所有可選鞋子")]
    public List<ShoesData> shoesList = new List<ShoesData>();

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;

        shoesController = player.GetComponentInChildren<ShoesController>();

        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        GenerateShoesUI();
    }

    void GenerateShoesUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var shoes in shoesList)
        {
            GameObject obj = Instantiate(shoesButtonPrefab, content);
            ShoesButtonUI ui = obj.GetComponent<ShoesButtonUI>();
            ui.Setup(shoes, this);
        }
    }

    public void SelectShoes(ShoesData shoes)
    {
        shoesController.shoesUp = shoes.shoesUp;
        shoesController.shoesDown = shoes.shoesDown;
        shoesController.shoesLeft = shoes.shoesLeft;
        shoesController.shoesRight = shoes.shoesRight;

        shoesController.UpdateShoesDirection(0, -1);

        Debug.Log($"成功替換鞋子：{shoes.shoesName}");
    }
}
