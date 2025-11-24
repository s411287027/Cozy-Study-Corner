using UnityEngine;

[CreateAssetMenu(fileName = "PantsData", menuName = "DressScene/Pants Data", order = 0)]
public class PantsData : ScriptableObject
{
    public int pantsID;
    public string pantsName;

    public Sprite pantsUp;
    public Sprite pantsDown;
    public Sprite pantsLeft;
    public Sprite pantsRight;

    public Sprite[] pantsUpFrames;
    public Sprite[] pantsDownFrames;
    public Sprite[] pantsLeftFrames;
    public Sprite[] pantsRightFrames;
}
