using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShoesButtonUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text shoesNameText;
    public Button button;

    public ShoesData data;
    private ShoesSelectionUI controller;

    public void Setup(ShoesData shoes, ShoesSelectionUI ctrl)
    {
        data = shoes;
        controller = ctrl;

        shoesNameText.text = shoes.shoesName;
        icon.sprite = shoes.shoesDown;

        button.onClick.AddListener(() =>
        {
            controller.SelectShoes(shoes);
        });
    }
}
