using UnityEngine;

[CreateAssetMenu(fileName = "FaceData", menuName = "DressScene/Face Data", order = 0)]
public class FaceData : ScriptableObject
{
    public int faceID;
    [Header("臉部名稱")]
    public string faceName;

    [Header("不同方向的靜態圖片（無動畫）")]
    public Sprite faceUp;
    public Sprite faceDown;
    public Sprite faceLeft;
    public Sprite faceRight;

    [Header("動畫幀")]
    public Sprite[] faceUpFrames;
    public Sprite[] faceDownFrames;
    public Sprite[] faceLeftFrames;
    public Sprite[] faceRightFrames;
}
