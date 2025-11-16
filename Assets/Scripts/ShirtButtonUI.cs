using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShirtButtonUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text shirtNameText;
    public Button button;

    public ShirtData data;
    private ShirtSelectionUI controller;

    public void Setup(ShirtData shirt, ShirtSelectionUI ctrl)
    {
        data = shirt;
        controller = ctrl;

        shirtNameText.text = shirt.shirtName;
        icon.sprite = shirt.shirtDown;

        button.onClick.AddListener(() =>
        {
            controller.SelectShirt(shirt);
        });
    }
}
