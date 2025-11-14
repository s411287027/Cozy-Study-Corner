using UnityEngine;

[CreateAssetMenu(fileName = "HairData", menuName = "DressScene/Hair Data", order = 0)]
public class HairData : ScriptableObject
{
    [Header("髮型名稱")]
    public string hairName;

    [Header("靜態圖")]
    public Sprite hairUp;
    public Sprite hairDown;
    public Sprite hairLeft;
    public Sprite hairRight;

    [Header("動畫幀")]
    public Sprite[] hairUpFrames;
    public Sprite[] hairDownFrames;
    public Sprite[] hairLeftFrames;
    public Sprite[] hairRightFrames;
}
