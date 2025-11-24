using UnityEngine;

[CreateAssetMenu(fileName = "ShirtData", menuName = "DressScene/Shirt Data", order = 0)]
public class ShirtData : ScriptableObject
{
    public int shirtID;
    [Header("衣服名稱")]
    public string shirtName;

    [Header("靜態圖")]
    public Sprite shirtUp;
    public Sprite shirtDown;
    public Sprite shirtLeft;
    public Sprite shirtRight;

    [Header("動畫幀")]
    public Sprite[] shirtUpFrames;
    public Sprite[] shirtDownFrames;
    public Sprite[] shirtLeftFrames;
    public Sprite[] shirtRightFrames;
}
