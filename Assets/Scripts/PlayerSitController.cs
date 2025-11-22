using UnityEngine;

public class PlayerSitController : MonoBehaviour
{
    [System.Serializable]
    public class SitPart
    {
        public string name;                 // ����W�١A�Ҧp Body�BLeg
        public SpriteRenderer renderer;     // ���� SpriteRenderer

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

        // �O���U�����l���A
        foreach (var p in sitParts)
        {
            if (p.renderer != null)
            {
                p.originalPosition = p.renderer.transform.position;
                p.originalSortingOrder = p.renderer.sortingOrder;
                p.originalSprite = p.renderer.sprite;

                // ��l����
                p.renderer.gameObject.SetActive(false);
            }
        }
    }

    // ===================== ���U =====================
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

            // �]�w��m�B�Ϥ��B�Ƨ�
            part.renderer.transform.position = data.position.position;
            part.renderer.sprite = data.sprite;
            part.renderer.sortingOrder = data.sortingOrder;

            // �M�Χ��U�Y��
            part.renderer.transform.localScale = data.scale;

            part.renderer.gameObject.SetActive(true);
        }
    }

    // ===================== ���_ =====================
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

                // ��_��l�j�p
                part.renderer.transform.localScale = Vector3.one;

                part.renderer.gameObject.SetActive(false);
            }
        }
    }
}
