using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PantsButtonUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text pantsNameText;
    public Button button;

    public PantsData data;
    private PantsSelectionUI controller;

    public void Setup(PantsData pants, PantsSelectionUI ctrl)
    {
        data = pants;
        controller = ctrl;

        pantsNameText.text = pants.pantsName;
        icon.sprite = pants.pantsDown;

        button.onClick.AddListener(() =>
        {
            controller.SelectPants(pants);
        });
    }
}
