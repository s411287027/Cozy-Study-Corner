using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Auth;
using System.Linq;

[System.Serializable]
public class Pseudo3DConfig
{
    public string sceneName;
    public float baseY = 0f;
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

    [Header("裝備資料庫 (請確認這裡有東西！)")]
    public List<HairData> allHairList;
    public List<ShirtData> allShirtList;
    public List<PantsData> allPantsList;
    public List<ShoesData> allShoesList;
    public List<FaceData> allFaceList;

    [Header("控制器")]
    public HairController hairController;
    public ShirtController shirtController;
    public PantsController pantsController;
    public ShoesController shoesController;
    public FaceController faceController;

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
    private bool freezeShirt = false;
    private bool freezePants = false;
    private bool freezeShoes = false;
    private bool freezeFace = false;

    private DatabaseReference dbRef;

    void Awake()
    {
        originalScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (hairController == null) hairController = GetComponentInChildren<HairController>();
        if (shirtController == null) shirtController = GetComponentInChildren<ShirtController>();
        if (pantsController == null) pantsController = GetComponentInChildren<PantsController>();
        if (shoesController == null) shoesController = GetComponentInChildren<ShoesController>();
        if (faceController == null) faceController = GetComponentInChildren<FaceController>();
    }

    void Start()
    {
        ani = GetComponent<Animator>();
        targetPosition = rb.position;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        if (clickIndicatorPrefab != null)
        {
            clickIndicatorInstance = Instantiate(clickIndicatorPrefab, targetPosition, Quaternion.identity);
            clickIndicatorInstance.SetActive(false);
        }

        UpdateSceneFlag();

        // ================================================================
        // 🔥 [修正重點] 這裡要把強制轉正面的程式碼「搬出來」！
        // 讓它在任何場景（不只是 DressScene）一開始都先面向正面 (0, -1)
        // ================================================================
        if (ani != null)
        {
            ani.SetFloat("Horizontal", 0);
            ani.SetFloat("Vertical", -1); // 設定動畫參數為正面
        }

        // 強制所有控制器立刻刷新成正面圖片
        // 這樣在等待 Firebase 下載的那幾秒，玩家也會是正面的
        if (hairController != null) hairController.ForceUpdateHairSprite(0f, -1f);
        if (shirtController != null) shirtController.ForceUpdateShirtSprite(0f, -1f);
        if (pantsController != null) pantsController.ForceUpdatePantsSprite(0f, -1f);
        if (shoesController != null) shoesController.ForceUpdateShoesSprite(0f, -1f);
        if (faceController != null) faceController.ForceUpdateFaceSprite(0f, -1f);

        // 🔥 設定好方向後，再開始下載裝備
        StartCoroutine(LoadAndEquipFromFirebase());

        // 原本的 DressScene 邏輯 (只剩下鎖定移動的功能)
        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            canMove = false;
            freezeHair = true;
            freezeShirt = true;
            freezePants = true;
            freezeShoes = true;
            //freezeFace = true;
            rb.linearVelocity = Vector2.zero;
            ani.SetFloat("Speed", 0);

            // 上面已經轉過正面了，這裡不用再寫一次
        }
    }

    // ================================================================
    // 🔥 偵錯版：讀取 Firebase
    // ================================================================
    IEnumerator LoadAndEquipFromFirebase()
    {
        // 1. 檢查登入
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogError("⛔【失敗】檢測不到使用者登入！請先執行 Login 場景，或確認 Firebase 初始化。");
            yield break;
        }

        string uid = currentUser.UserId;
        Debug.Log($"🔍 開始讀取玩家 {uid} 的裝備...");

        // 2. 讀取資料
        var task = dbRef.Child("users").Child(uid).Child("currentEquip").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("❌ Firebase 連線錯誤：" + task.Exception);
            yield break;
        }

        DataSnapshot snapshot = task.Result;
        if (snapshot.Exists)
        {
            Debug.Log($"✅ 讀取成功！找到 {snapshot.ChildrenCount} 筆裝備資料。");

            if (snapshot.HasChild("hair")) EquipHair(int.Parse(snapshot.Child("hair").Value.ToString()));
            else Debug.LogWarning("⚠️ Firebase 中沒有 'hair' 的紀錄");

            if (snapshot.HasChild("face")) EquipFace(int.Parse(snapshot.Child("face").Value.ToString()));
            if (snapshot.HasChild("shirt")) EquipShirt(int.Parse(snapshot.Child("shirt").Value.ToString()));
            if (snapshot.HasChild("pants")) EquipPants(int.Parse(snapshot.Child("pants").Value.ToString()));
            if (snapshot.HasChild("shoes")) EquipShoes(int.Parse(snapshot.Child("shoes").Value.ToString()));
        }
        else
        {
            Debug.LogWarning($"⚠️ 路徑 users/{uid}/currentEquip 不存在！請確認資料庫是否為空。");
        }
    }

    // ================================================================
    // 🔥 偵錯版：裝備邏輯
    // ================================================================
    void EquipHair(int id)
    {
        Debug.Log($"📥 嘗試穿上髮型 ID: {id}");

        if (hairController == null)
        {
            Debug.LogError("❌ HairController 遺失！");
            return;
        }
        if (allHairList == null || allHairList.Count == 0)
        {
            Debug.LogError("❌ Inspector 中的 AllHairList 是空的！請拖入 Data 檔案！");
            return;
        }

        HairData data = allHairList.FirstOrDefault(x => x.hairID == id);

        if (data != null)
        {
            hairController.hairUp = data.hairUp;
            hairController.hairDown = data.hairDown;
            hairController.hairLeft = data.hairLeft;
            hairController.hairRight = data.hairRight;
            hairController.hairUpFrames = data.hairUpFrames;
            hairController.hairDownFrames = data.hairDownFrames;
            hairController.hairLeftFrames = data.hairLeftFrames;
            hairController.hairRightFrames = data.hairRightFrames;

            hairController.ForceUpdateHairSprite(0f, -1f);
            Debug.Log($"✅ 成功穿上髮型：{data.name}");
        }
        else
        {
            Debug.LogError($"❌ 在 AllHairList 中找不到 ID 為 {id} 的資料。請檢查 ScriptableObject 內的 ID 數值。");
        }
    }

    // (其他裝備函式邏輯相同，省略重複 Log 以保持簡潔，但建議你也檢查其他部位)
    void EquipFace(int id)
    {
        if (faceController == null) return;
        FaceData data = allFaceList.FirstOrDefault(x => x.faceID == id);
        if (data != null)
        {
            faceController.faceUp = data.faceUp;
            faceController.faceDown = data.faceDown;
            faceController.faceLeft = data.faceLeft;
            faceController.faceRight = data.faceRight;
            faceController.faceUpFrames = data.faceUpFrames;
            faceController.faceDownFrames = data.faceDownFrames;
            faceController.faceLeftFrames = data.faceLeftFrames;
            faceController.faceRightFrames = data.faceRightFrames;
            faceController.ForceUpdateFaceSprite(0f, -1f);
        }
    }

    void EquipShirt(int id)
    {
        if (shirtController == null) return;
        ShirtData data = allShirtList.FirstOrDefault(x => x.shirtID == id);
        if (data != null)
        {
            shirtController.shirtUp = data.shirtUp;
            shirtController.shirtDown = data.shirtDown;
            shirtController.shirtLeft = data.shirtLeft;
            shirtController.shirtRight = data.shirtRight;
            shirtController.shirtUpFrames = data.shirtUpFrames;
            shirtController.shirtDownFrames = data.shirtDownFrames;
            shirtController.shirtLeftFrames = data.shirtLeftFrames;
            shirtController.shirtRightFrames = data.shirtRightFrames;
            shirtController.ForceUpdateShirtSprite(0f, -1f);
        }
    }

    void EquipPants(int id)
    {
        if (pantsController == null) return;
        PantsData data = allPantsList.FirstOrDefault(x => x.pantsID == id);
        if (data != null)
        {
            pantsController.pantsUp = data.pantsUp;
            pantsController.pantsDown = data.pantsDown;
            pantsController.pantsLeft = data.pantsLeft;
            pantsController.pantsRight = data.pantsRight;
            pantsController.pantsUpFrames = data.pantsUpFrames;
            pantsController.pantsDownFrames = data.pantsDownFrames;
            pantsController.pantsLeftFrames = data.pantsLeftFrames;
            pantsController.pantsRightFrames = data.pantsRightFrames;
            pantsController.ForceUpdatePantsSprite(0f, -1f);
        }
    }

    void EquipShoes(int id)
    {
        if (shoesController == null) return;
        ShoesData data = allShoesList.FirstOrDefault(x => x.shoesID == id);
        if (data != null)
        {
            shoesController.shoesUp = data.shoesUp;
            shoesController.shoesDown = data.shoesDown;
            shoesController.shoesLeft = data.shoesLeft;
            shoesController.shoesRight = data.shoesRight;
            shoesController.shoesUpFrames = data.shoesUpFrames;
            shoesController.shoesDownFrames = data.shoesDownFrames;
            shoesController.shoesLeftFrames = data.shoesLeftFrames;
            shoesController.shoesRightFrames = data.shoesRightFrames;
            shoesController.ForceUpdateShoesSprite(0f, -1f);
        }
    }

    // ================================================================
    // 原有 Update
    // ================================================================
    void Update()
    {
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
            if (clickIndicatorInstance != null) clickIndicatorInstance.SetActive(false);
        }

        float hx = ani.GetFloat("Horizontal");
        float hy = ani.GetFloat("Vertical");

        if (hairController != null && !freezeHair)
            hairController.UpdateHairDirection(dir.magnitude > stopThreshold ? hx : 0f, dir.magnitude > stopThreshold ? hy : 0f);
        if (shirtController != null && !freezeShirt)
            shirtController.UpdateShirtDirection(dir.magnitude > stopThreshold ? hx : 0f, dir.magnitude > stopThreshold ? hy : 0f);
        if (pantsController != null && !freezePants)
            pantsController.UpdatePantsDirection(dir.magnitude > stopThreshold ? hx : 0f, dir.magnitude > stopThreshold ? hy : 0f);
        if (shoesController != null && !freezeShoes)
            shoesController.UpdateShoesDirection(dir.magnitude > stopThreshold ? hx : 0f, dir.magnitude > stopThreshold ? hy : 0f);
        if (faceController != null && !freezeFace)
            faceController.UpdateFaceDirection(dir.magnitude > stopThreshold ? hx : 0f, dir.magnitude > stopThreshold ? hy : 0f);
    }

    void FixedUpdate()
    {
        if (!canMove) return;

        Vector2 dir = targetPosition - rb.position;

        // 如果已經到達目標點，直接套用偽3D縮放和排序
        if (dir.magnitude <= stopThreshold)
        {
            rb.position = targetPosition;
            ApplyPseudo3DScaleAndSorting();
            return;
        }

        Vector2 moveDir = dir.normalized;

        // 避免移動過頭
        float distance = Mathf.Min(moveSpeed * Time.fixedDeltaTime, dir.magnitude);

        // 檢測前方障礙物
        RaycastHit2D[] hits = new RaycastHit2D[5];
        int hitCount = rb.Cast(moveDir, hits, distance);
        bool blocked = false;
        Vector2 pushBack = Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            if (((1 << hits[i].collider.gameObject.layer) & obstacleLayer) != 0)
            {
                blocked = true;
                pushBack += hits[i].normal * 0.01f;
            }
        }

        if (blocked)
        {
            // 推開角色
            rb.position += pushBack;

            // 停止移動
            targetPosition = rb.position;

            // 停止點擊指示器
            if (clickIndicatorInstance != null) clickIndicatorInstance.SetActive(false);

            ApplyPseudo3DScaleAndSorting();
            return;
        }

        // 正常移動
        rb.MovePosition(rb.position + moveDir * distance);

        ApplyPseudo3DScaleAndSorting();
    }



    void ApplyPseudo3DScaleAndSorting()
    {
        float y = rb.position.y;
        int currentOrder = 5;
        if (isPseudo3D)
        {
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
            currentOrder = Mathf.RoundToInt(Mathf.Lerp(minSortingOrder, maxSortingOrder, tLayer));
            sr.sortingOrder = currentOrder;
        }
        else
        {
            transform.localScale = originalScale;
            if (sr != null) sr.sortingOrder = 5;
            currentOrder = sr.sortingOrder;
        }
        UpdateAllAccessoriesSorting(currentOrder);
    }

    void UpdateAllAccessoriesSorting(int playerSortingOrder)
    {
        if (hairController != null) hairController.UpdateSortingOrder(playerSortingOrder + 5);
        if (faceController != null) faceController.UpdateSortingOrder(playerSortingOrder + 4);
        if (shirtController != null) shirtController.UpdateSortingOrder(playerSortingOrder + 3);
        if (pantsController != null) pantsController.UpdateSortingOrder(playerSortingOrder + 2);
        if (shoesController != null) shoesController.UpdateSortingOrder(playerSortingOrder + 1);
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
                Pseudo3DConfig config = System.Array.Find(sceneConfigs, c => c.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase));
                if (config != null)
                {
                    baseY = config.baseY; baseScale = config.baseScale; farScale = config.farScale; nearScale = config.nearScale; farY = config.farY; nearY = config.nearY;
                }
                else
                {
                    baseY = transform.position.y; baseScale = 1.8f; farScale = 1.6f; nearScale = 2f; farY = 10f; nearY = -10f;
                }
                originalScale = transform.localScale;
                transform.localScale = originalScale * baseScale;
                UpdateSortingOrder();
            }
            else
            {
                transform.localScale = originalScale;
                if (sr != null) sr.sortingOrder = 5;
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
        freezeHair = true; freezeShirt = true; freezePants = true; freezeShoes = true; freezeFace = true;
        if (clickIndicatorInstance != null) clickIndicatorInstance.SetActive(false);
        if (scene.name == "DressScene")
        {
            canMove = false;
            if (sr != null) sr.enabled = false;
            if (hairController != null) hairController.ForceUpdateHairSprite(0f, -1f);
            if (shirtController != null) shirtController.ForceUpdateShirtSprite(0f, -1f);
            if (pantsController != null) pantsController.ForceUpdatePantsSprite(0f, -1f);
            if (shoesController != null) shoesController.ForceUpdateShoesSprite(0f, -1f);
            if (faceController != null) faceController.ForceUpdateFaceSprite(0f, -1f);
            return;
        }
        else
        {
            if (sr != null) sr.enabled = true;
            freezeHair = false; freezeShirt = false; freezePants = false; freezeShoes = false; freezeFace = false;
        }
        Invoke(nameof(EnableMove), 0.01f);
    }

    public void SetPositionInstant(Vector3 pos)
    {
        canMove = false; targetPosition = pos; rb.position = pos; transform.position = pos; rb.linearVelocity = Vector2.zero;
        UpdateSceneFlag();
        Invoke(nameof(EnableMove), 0.01f);
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!value && rb != null) rb.linearVelocity = Vector2.zero;
    }

    void EnableMove() { SetCanMove(true); }
    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
}