using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PantsButtonUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text pantsNameText;
    public Button button;

    private PantsData data;
    private PantsSelectionUI controller;

    public void Setup(PantsData p, PantsSelectionUI c)
    {
        data = p;
        controller = c;

        pantsNameText.text = p.pantsName;
        icon.sprite = p.pantsDown;

        button.onClick.AddListener(() =>
        {
            controller.SelectPants(p);
        });
    }
}
