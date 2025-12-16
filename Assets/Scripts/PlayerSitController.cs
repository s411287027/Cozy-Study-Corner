using System.Collections;
using UnityEngine;

public class PlayerSitController : MonoBehaviour
{
    [System.Serializable]
    public class SitPart
    {
        // ... (SitPart 類別不變) ...
        public string name;
        public SpriteRenderer renderer;

        [HideInInspector] public Vector3 originalPosition;
        [HideInInspector] public int originalSortingOrder;
        [HideInInspector] public Sprite originalSprite;
    }

    public SitPart[] sitParts;

    [Tooltip("連結 Avatar 站姿的主體 GameObject (如 Naked down_0000)。若為空，將嘗試使用 GetComponent<SpriteRenderer>()。")]
    public GameObject originalBodyGameObject; // ⭐ 修正點 1: 保持 GameObject 供外部連結

    private SpriteRenderer sr; // ⭐ 修正點 2: 重新引入 sr 變數
    private Animator ani;

    private bool isSitting = false;

    // ⭐ 存人物方向（不是衣服）
    private float savedH;
    private float savedV;

    void Awake()
    {
        // ⭐ 修正點 3: 重新加入 GetComponent 邏輯，作為 fallback
        sr = GetComponent<SpriteRenderer>();
        ani = GetComponent<Animator>();

        // 儲存坐姿部件的原始狀態
        foreach (var p in sitParts)
        {
            if (p.renderer != null)
            {
                p.originalPosition = p.renderer.transform.position;
                p.originalSortingOrder = p.renderer.sortingOrder;
                p.originalSprite = p.renderer.sprite;

                p.renderer.gameObject.SetActive(false);
            }
        }
    }

    // ===================== 坐下 =====================
    public void Sit(SitButton.SitPartData[] partsData)
    {
        if (isSitting) return;
        isSitting = true;

        // ⭐ 存 Animator 當下方向
        if (ani != null)
        {
            ani.keepAnimatorControllerStateOnDisable = true;
            ani.enabled = false;
        }

        // ⭐ 修正點 4: 優先使用連結的 GameObject，如果 GameObject 為空，則嘗試使用 GetComponent 獲取的 sr。
        if (originalBodyGameObject != null)
        {
            originalBodyGameObject.SetActive(false);
        }
        else if (sr != null) // 如果沒有連結父物件，但主物件上有 Renderer (適用於您自己的玩家)
        {
            sr.enabled = false;
        }

        if (ani != null) ani.enabled = false;

        foreach (var data in partsData)
        {
            var part = System.Array.Find(sitParts, p => p.name == data.partName);
            // ... (其餘 Sit 邏輯不變) ...
            if (part == null) continue;

            part.renderer.transform.position = data.position.position;
            part.renderer.sprite = data.sprite;
            part.renderer.sortingOrder = data.sortingOrder;
            part.renderer.transform.localScale = data.scale;
            part.renderer.gameObject.SetActive(true);
        }
    }

    // ===================== 站立 =====================
    public void StandUp()
    {
        if (!isSitting) return;
        isSitting = false;

        // ⭐ 修正點 5: 恢復站姿時，使用相同的優先級邏輯
        if (originalBodyGameObject != null)
        {
            originalBodyGameObject.SetActive(true);
        }
        else if (sr != null)
        {
            sr.enabled = true;
        }

        if (ani != null) ani.enabled = true;

        foreach (var part in sitParts)
        {
            // ... (其餘 StandUp 邏輯不變) ...
            if (part.renderer != null)
            {
                part.renderer.transform.position = part.originalPosition;
                part.renderer.sortingOrder = part.originalSortingOrder;
                part.renderer.sprite = part.originalSprite;
                part.renderer.transform.localScale = Vector3.one;
                part.renderer.gameObject.SetActive(false);
            }
        }


        // ⭐⭐ 關鍵：等 Animator 第一幀跑完，再設方向
        StartCoroutine(RestoreDirectionAfterAnimator());
    }

    IEnumerator RestoreDirectionAfterAnimator()
    {
        yield return null; // ⭐ 一定要這一行

        ani.SetFloat("Horizontal", savedH);
        ani.SetFloat("Vertical", savedV);
    }
}