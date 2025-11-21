using UnityEngine;

public class PlayerSitController : MonoBehaviour
{
    [System.Serializable]
    public class SitPart
    {
        public string name;                 // 部位名稱，例如 Body、Leg
        public SpriteRenderer renderer;     // 對應 SpriteRenderer

        [HideInInspector] public Vector3 originalPosition;
        [HideInInspector] public int originalSortingOrder;
        [HideInInspector] public Sprite originalSprite;
    }

    public SitPart[] sitParts;

    private SpriteRenderer sr;
    private Animator ani;

    private bool isSitting = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ani = GetComponent<Animator>();

        // 記錄各部位原始狀態
        foreach (var p in sitParts)
        {
            if (p.renderer != null)
            {
                p.originalPosition = p.renderer.transform.position;
                p.originalSortingOrder = p.renderer.sortingOrder;
                p.originalSprite = p.renderer.sprite;

                // 初始隱藏
                p.renderer.gameObject.SetActive(false);
            }
        }
    }

    // ===================== 坐下 =====================
    public void Sit(SitButton.SitPartData[] partsData)
    {
        if (isSitting) return;
        isSitting = true;

        if (sr != null) sr.enabled = false;
        if (ani != null) ani.enabled = false;

        foreach (var data in partsData)
        {
            var part = System.Array.Find(sitParts, p => p.name == data.partName);
            if (part == null) continue;

            // 設定位置、圖片、排序
            part.renderer.transform.position = data.position.position;
            part.renderer.sprite = data.sprite;
            part.renderer.sortingOrder = data.sortingOrder;

            // 套用坐下縮放
            part.renderer.transform.localScale = data.scale;

            part.renderer.gameObject.SetActive(true);
        }
    }

    // ===================== 站起 =====================
    public void StandUp()
    {
        if (!isSitting) return;
        isSitting = false;

        if (sr != null) sr.enabled = true;
        if (ani != null) ani.enabled = true;

        foreach (var part in sitParts)
        {
            if (part.renderer != null)
            {
                part.renderer.transform.position = part.originalPosition;
                part.renderer.sortingOrder = part.originalSortingOrder;
                part.renderer.sprite = part.originalSprite;

                // 恢復原始大小
                part.renderer.transform.localScale = Vector3.one;

                part.renderer.gameObject.SetActive(false);
            }
        }
    }
}
