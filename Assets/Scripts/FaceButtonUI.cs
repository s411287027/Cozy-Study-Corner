using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FaceButtonUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text faceNameText;
    public Button button;

    public FaceData data;
    private FaceSelectionUI controller;

    public void Setup(FaceData face, FaceSelectionUI ctrl)
    {
        data = face;
        controller = ctrl;

        faceNameText.text = face.faceName;
        icon.sprite = face.faceDown;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            controller.SelectFace(face);
        });
    }
}
