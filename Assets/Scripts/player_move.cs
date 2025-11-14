using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

[System.Serializable]
public class Pseudo3DConfig
{
    public string sceneName;
    public float baseY = 0f;     // 偽3D基準Y
    public float baseScale = 1.8f;
    public float farScale = 1.6f;
    public float nearScale = 2f;
    public float farY = 10f;
    public float nearY = -10f;
}

public class player_move : MonoBehaviour
{
    [Header("角色設定")]
    public float moveSpeed = 5f;
    public float stopThreshold = 0.05f;

    [Header("頭髮設定")]
    public HairController hairController;

    [Header("點擊指示器")]
    public GameObject clickIndicatorPrefab;
    private GameObject clickIndicatorInstance;

    private Animator ani;
    private Rigidbody2D rb;
    private Vector2 targetPosition;

    [Header("碰撞設定")]
    public LayerMask obstacleLayer;

    [Header("圖層設定")]
    public int maxSortingOrder = 100;
    public int minSortingOrder = 0;

    [Header("啟用偽3D的場景")]
    public string[] pseudo3DScenes = { "classroom", "CafeScene", "LibraryScene", "ForestScene" };

    [Header("場景偽3D配置")]
    public Pseudo3DConfig[] sceneConfigs;

    private float baseY;
    private float baseScale;
    private float farScale;
    private float nearScale;
    private float farY;
    private float nearY;

    private Vector3 originalScale;
    private bool isPseudo3D = false;
    private SpriteRenderer sr;
    private bool canMove = true;
    private bool freezeHair = false;

    void Awake()
    {
        originalScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        ani = GetComponent<Animator>();
        targetPosition = rb.position;

        if (clickIndicatorPrefab != null)
        {
            clickIndicatorInstance = Instantiate(clickIndicatorPrefab, targetPosition, Quaternion.identity);
            clickIndicatorInstance.SetActive(false);
        }

        UpdateSceneFlag(); // 初始化偽3D

        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            canMove = false;
            freezeHair = true;   // 停止頭髮方向更新
            rb.linearVelocity = Vector2.zero;

            // 設定面向正面（朝下）
            ani.SetFloat("Horizontal", 0);
            ani.SetFloat("Vertical", -1);
            ani.SetFloat("Speed", 0);

            if (hairController != null)
                hairController.UpdateHairDirection(0f, -1f);
        }
    }

    void Update()
    {
        //  DressScene 永遠不能動（保險鎖）
        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            rb.linearVelocity = Vector2.zero;
            ani.SetFloat("Speed", 0);
            return;
        }

        if (!canMove) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            targetPosition = mouseWorld;

            if (clickIndicatorInstance != null)
            {
                clickIndicatorInstance.transform.position = targetPosition;
                clickIndicatorInstance.SetActive(true);
            }
        }

        Vector2 dir = targetPosition - rb.position;
        float dirX = 0f;
        float dirY = 0f;

        if (dir.magnitude > stopThreshold)
        {
            Vector2 dirNormalized = dir.normalized;
            dirX = dirNormalized.x;
            dirY = dirNormalized.y;

            ani.SetFloat("Horizontal", dirNormalized.x);
            ani.SetFloat("Vertical", dirNormalized.y);
            ani.SetFloat("Speed", dir.magnitude);
        }
        else
        {
            ani.SetFloat("Speed", 0);
            if (clickIndicatorInstance != null)
                clickIndicatorInstance.SetActive(false);
        }
        if (hairController != null && !freezeHair)
        {
            if (dir.magnitude > stopThreshold)
            {
                float hx = ani.GetFloat("Horizontal");
                float hy = ani.GetFloat("Vertical");
                hairController.UpdateHairDirection(hx, hy);
            }
            else
            {
                // 🟢 玩家停止移動時傳 (0,0) → Hair 停止動畫
                hairController.UpdateHairDirection(0f, 0f);
            }
        }

    }

    void FixedUpdate()
    {
        if (!canMove) return;

        Vector2 dir = targetPosition - rb.position;
        if (dir.magnitude <= stopThreshold)
        {
            rb.position = targetPosition;
            ApplyPseudo3DScaleAndSorting();
            return;
        }

        Vector2 moveDir = dir.normalized;
        float distance = moveSpeed * Time.fixedDeltaTime;

        RaycastHit2D[] hits = new RaycastHit2D[1];
        int hitCount = rb.Cast(moveDir, hits, distance);

        bool blocked = false;
        if (hitCount > 0 && ((1 << hits[0].collider.gameObject.layer) & obstacleLayer) != 0)
        {
            blocked = true;
        }

        if (blocked)
        {
            targetPosition = rb.position;
            if (clickIndicatorInstance != null)
                clickIndicatorInstance.SetActive(false);
            ApplyPseudo3DScaleAndSorting();
            return;
        }

        rb.MovePosition(rb.position + moveDir * distance);
        ApplyPseudo3DScaleAndSorting();
    }

    void ApplyPseudo3DScaleAndSorting()
    {
        if (isPseudo3D)
        {
            float y = rb.position.y;
            float scaleFactor;

            if (y > baseY)
            {
                float t = Mathf.InverseLerp(baseY, farY, y);
                scaleFactor = Mathf.Lerp(baseScale, farScale, t);
            }
            else
            {
                float t = Mathf.InverseLerp(baseY, nearY, y);
                scaleFactor = Mathf.Lerp(baseScale, nearScale, t);
            }

            transform.localScale = originalScale * scaleFactor;

            float tLayer = Mathf.InverseLerp(farY, nearY, y);
            sr.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(minSortingOrder, maxSortingOrder, tLayer));
        }
        else
        {
            // 沒有偽3D的場景，恢復原始圖層與大小
            transform.localScale = originalScale;
            if (sr != null)
                sr.sortingOrder = 5; // 設回預設圖層
        }
    }

    void UpdateSceneFlag()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool pseudo3D = System.Array.Exists(pseudo3DScenes, s => s.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase));

        if (pseudo3D != isPseudo3D)
        {
            isPseudo3D = pseudo3D;

            if (isPseudo3D)
            {
                // 尋找場景專屬配置
                Pseudo3DConfig config = System.Array.Find(sceneConfigs, c => c.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase));
                if (config != null)
                {
                    baseY = config.baseY;
                    baseScale = config.baseScale;
                    farScale = config.farScale;
                    nearScale = config.nearScale;
                    farY = config.farY;
                    nearY = config.nearY;
                }
                else
                {
                    baseY = transform.position.y;
                    baseScale = 1.8f;
                    farScale = 1.6f;
                    nearScale = 2f;
                    farY = 10f;
                    nearY = -10f;
                }

                originalScale = transform.localScale;
                transform.localScale = originalScale * baseScale;
                UpdateSortingOrder();
            }
            else
            {
                // 沒有偽3D場景，恢復原始大小和圖層
                transform.localScale = originalScale;
                if (sr != null)
                    sr.sortingOrder = 5;
            }
        }
    }


    void UpdateSortingOrder()
    {
        if (!isPseudo3D || sr == null) return;
        float y = rb.position.y;
        float tLayer = Mathf.InverseLerp(farY, nearY, y);
        sr.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(minSortingOrder, maxSortingOrder, tLayer));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateSceneFlag();
        SetCanMove(false);
        freezeHair = true;
        if (clickIndicatorInstance != null)
            clickIndicatorInstance.SetActive(false);

        //  如果是 Dress 場景，禁止移動
        if (scene.name == "DressScene")
        {
            canMove = false;
            Debug.Log("玩家進入 Dress 場景，停止移動");
            //  設定角色面向「正面（朝下）」
            if (ani != null)
            {
                ani.SetFloat("Horizontal", 0);
                ani.SetFloat("Vertical", -1);  // -1 代表朝下
                ani.SetFloat("Speed", 0);      // 停止移動動畫
            }

            //  同步更新髮型方向（顯示正面髮型）
            if (hairController != null)
            {
                hairController.UpdateHairDirection(0f, -1f);
            }

            return; // 不要重新啟用移動
        }
        else
        {
            freezeHair = false; //  其他場景恢復頭髮更新
        }

        Invoke(nameof(EnableMove), 0.01f);
    }

    public void SetPositionInstant(Vector3 pos)
    {
        canMove = false;
        targetPosition = pos;
        rb.position = pos;
        transform.position = pos;
        rb.linearVelocity = Vector2.zero;

        UpdateSceneFlag();
        Invoke(nameof(EnableMove), 0.01f);
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!value && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    void EnableMove()
    {
        SetCanMove(true);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


}
