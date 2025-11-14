using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HairButtonUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text hairNameText;
    public Button button;

    public HairData data;
    private HairSelectionUI controller;

    public void Setup(HairData hair, HairSelectionUI ctrl)
    {
        data = hair;
        controller = ctrl;

        hairNameText.text = hair.hairName;
        icon.sprite = hair.hairDown;  // 縮圖

        button.onClick.AddListener(() =>
        {
            controller.SelectHair(hair);
        });
    }
}
