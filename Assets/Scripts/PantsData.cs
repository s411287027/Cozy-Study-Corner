using UnityEngine;

[CreateAssetMenu(fileName = "PantsData", menuName = "DressScene/Pants Data", order = 0)]
public class PantsData : ScriptableObject
{
    public int pantsID;
    [Header("庫子名稱")]
    public string pantsName;

    [Header("靜態圖")]
    public Sprite pantsUp;
    public Sprite pantsDown;
    public Sprite pantsLeft;
    public Sprite pantsRight;

    [Header("動畫幀")]
    public Sprite[] pantsUpFrames;
    public Sprite[] pantsDownFrames;
    public Sprite[] pantsLeftFrames;
    public Sprite[] pantsRightFrames;
}
