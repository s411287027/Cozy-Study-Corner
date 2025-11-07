using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class player_move : MonoBehaviour
{
    [Header("角色設定")]
    public float moveSpeed = 5f;
    public float stopThreshold = 0.05f;

    [Header("點擊指示器")]
    public GameObject clickIndicatorPrefab;
    private GameObject clickIndicatorInstance;

    private Animator ani;
    private Rigidbody2D rb;
    private Vector2 targetPosition;

    [Header("碰撞設定")]
    public LayerMask obstacleLayer;

    [Header("Classroom 偽3D縮放設定")]
    public float farY = 10f;       // 最遠（往後）
    public float nearY = -10f;     // 最近（往前）
    public float baseScale = 1.8f; // Classroom 生成點比例
    public float farScale = 1.6f;  // 往後最小
    public float nearScale = 2f;   // 往前最大

    [Header("Classroom 圖層設定")]
    public int maxSortingOrder = 100; // spawnPoint往前最大
    public int minSortingOrder = 0;   // spawnPoint往後最小

    public Transform spawnPoint; // Classroom 生成點

    private float baseY;            // Classroom 基準點 y
    private Vector3 originalScale;  // prefab 原始 scale
    private bool isClassroom = false;
    private SpriteRenderer sr;

    void Awake()
    {
        originalScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateSceneFlag();
    }

    void Start()
    {
        ani = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        targetPosition = rb.position;

        if (clickIndicatorPrefab != null)
        {
            clickIndicatorInstance = Instantiate(clickIndicatorPrefab, rb.position, Quaternion.identity);
            clickIndicatorInstance.SetActive(false);
        }

        if (isClassroom)
        {
            baseY = spawnPoint != null ? spawnPoint.position.y : transform.position.y;
            transform.localScale = originalScale * baseScale; // 初始 Classroom scale
            UpdateSortingOrder();
        }
        else
        {
            transform.localScale = originalScale; // 其他場景
        }
    }

    void Update()
    {
        UpdateSceneFlag();

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
        if (dir.magnitude > stopThreshold)
        {
            Vector2 dirNormalized = dir.normalized;
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
    }

    void FixedUpdate()
    {
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
        if (hitCount > 0)
        {
            if (((1 << hits[0].collider.gameObject.layer) & obstacleLayer) != 0)
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
        if (!isClassroom) return;

        float y = rb.position.y;
        float scaleFactor;

        // 偽3D縮放
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

        // 圖層排序
        float tLayer = Mathf.InverseLerp(farY, nearY, y); // 0~1 往前越大
        sr.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(minSortingOrder, maxSortingOrder, tLayer));
    }

    void UpdateSceneFlag()
    {
        bool currentClassroom = SceneManager.GetActiveScene().name == "classroom";
        if (currentClassroom != isClassroom)
        {
            isClassroom = currentClassroom;
            if (isClassroom)
            {
                baseY = spawnPoint != null ? spawnPoint.position.y : transform.position.y;
                transform.localScale = originalScale * baseScale;
                UpdateSortingOrder();
            }
            else
            {
                transform.localScale = originalScale;
                if (sr != null)
                    sr.sortingOrder = 0;
            }
        }
    }

    void UpdateSortingOrder()
    {
        if (!isClassroom || sr == null) return;
        float y = rb.position.y;
        float tLayer = Mathf.InverseLerp(farY, nearY, y);
        sr.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(minSortingOrder, maxSortingOrder, tLayer));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateSceneFlag();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
