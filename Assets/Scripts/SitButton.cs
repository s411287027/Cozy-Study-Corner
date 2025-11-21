using UnityEngine;

public class SitButton : MonoBehaviour
{
    [System.Serializable]
    public class SitPartData
    {
        public string partName;       // 對應 PlayerSitController 的 SitPart 名稱
        public Sprite sprite;         // 坐下用圖片
        public Transform position;    // 座位上位置
        public int sortingOrder = 0;  // 排序
        public Vector3 scale = Vector3.one; // 坐下縮放
    }

    public SitPartData[] partsData;

    private PlayerSitController playerSit;

    void Start()
    {
        // 自動尋找玩家 SitController
        playerSit = FindObjectOfType<PlayerSitController>();

        if (playerSit == null)
            Debug.LogError("[SitButton] 找不到 PlayerSitController！");
    }

    public void OnSit()
    {
        playerSit?.Sit(partsData);
    }

    public void OnStandUp()
    {
        playerSit?.StandUp();
    }
}
