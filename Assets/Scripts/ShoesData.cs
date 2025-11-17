using UnityEngine;

[CreateAssetMenu(fileName = "ShoesData", menuName = "DressScene/Shoes Data", order = 0)]
public class ShoesData : ScriptableObject
{
    [Header("鞋子名稱")]
    public string shoesName;

    [Header("不同方向的鞋子圖片（無動畫）")]
    public Sprite shoesUp;
    public Sprite shoesDown;
    public Sprite shoesLeft;
    public Sprite shoesRight;

    [Header("動畫幀")]
    public Sprite[] shoesUpFrames;
    public Sprite[] shoesDownFrames;
    public Sprite[] shoesLeftFrames;
    public Sprite[] shoesRightFrames;
}
