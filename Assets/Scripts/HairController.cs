using UnityEngine;

[ExecuteAlways]
public class HairController : MonoBehaviour
{
    [Header("不同方向的靜態頭髮圖片（預設用）")]
    public Sprite hairUp;
    public Sprite hairDown;
    public Sprite hairLeft;
    public Sprite hairRight;

    [Header("不同方向的動態動畫幀（可多張）")]
    public Sprite[] hairUpFrames;
    public Sprite[] hairDownFrames;
    public Sprite[] hairLeftFrames;
    public Sprite[] hairRightFrames;

    [Header("動畫設定")]
    [Tooltip("當持續朝同方向移動時，幀與幀之間的時間（秒）")]
    public float animationInterval = 0.15f;

    [Header("位置 offset（相對 parent 的 localPosition）")]
    public Vector3 baseLocalOffset = Vector3.zero;

    [Header("移動偵測設定")]
    [Tooltip("當 parent 每幀位移小於此值時視為停止（單位：世界座標）")]
    public float movementThreshold = 0.001f;

    [Header("除錯")]
    public bool enableDebug = true;

    private SpriteRenderer sr;
    private Transform playerTransform;
    private string lastDir = "";
    private bool initializedOffset = false;

    // 🔹 動畫控制變數
    private float animTimer = 0f;
    private int animIndex = 0;
    private string currentMoveDir = "";
    private bool isMoving = false; // 是否正在移動（會播放動畫）
    private Vector3 prevPlayerPos; // 用來偵測 parent 是否移動

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        playerTransform = transform.parent;

        if (playerTransform == null)
        {
            if (enableDebug)
                Debug.LogWarning("⚠ HairController: Hair 不是 Player 的子物件，將自動搜尋 player_move。");

            playerTransform = FindObjectOfType<player_move>()?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("❌ 找不到 player_move！請確保 Hair 是 Player 的子物件。");
                return;
            }
        }

        if (!initializedOffset)
        {
            baseLocalOffset = transform.localPosition;
            initializedOffset = true;

            if (enableDebug)
                Debug.Log($"HairController: 初次設定 baseLocalOffset = {baseLocalOffset}");
        }

        if (sr != null && sr.sprite == null && hairDown != null)
            sr.sprite = hairDown;

        prevPlayerPos = playerTransform.position;
        UpdateHairDirection(1f, 0f);
    }

    void LateUpdate()
    {
        if (playerTransform == null)
            return;

        // 保持在 parent 的 local 相對位置
        transform.position = playerTransform.TransformPoint(baseLocalOffset);
        transform.localScale = Vector3.one;

        // 固定在 parent 的上層 sorting
        var playerSR = playerTransform.GetComponent<SpriteRenderer>();
        var hairSR = GetComponent<SpriteRenderer>();
        if (playerSR != null && hairSR != null)
        {
            hairSR.sortingLayerName = playerSR.sortingLayerName;
            hairSR.sortingOrder = playerSR.sortingOrder + 1;
        }

        // ===== 自行偵測 parent 是否移動（若 player_move 忘了每幀傳 0,0，這能保障） =====
        Vector3 currPos = playerTransform.position;
        float moved = (currPos - prevPlayerPos).sqrMagnitude;
        prevPlayerPos = currPos;

        if (enableDebug && moved < (movementThreshold * movementThreshold))
        {
            Debug.Log("HairController: Player idle (detected by movement).");
        }
        // else: 如果 parent 有動，isMoving 應該在 UpdateHairDirection 被設為 true（或也可在此強制）
        // 若你想完全以位移為準，可以改成 isMoving = parentIsMoving;

        // 播放動畫（只有在 isMoving = true）
        if (isMoving)
            AnimateHair();
    }

    /// <summary>
    /// 建議由 player_move 每幀呼叫：UpdateHairDirection(input.x, input.y)
    /// dirX, dirY 為輸入方向（可為 -1..1）
    /// </summary>
    public void UpdateHairDirection(float dirX, float dirY)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // 外部有明確傳入方向：用輸入判斷是否在移動（優先於 position 偵測）
        if (Mathf.Abs(dirX) < 0.001f && Mathf.Abs(dirY) < 0.001f)
        {
            // player_move 有傳 (0,0) -> 表示停止：保留最後一幀（不要改 sprite）
            isMoving = false;
            return;
        }

        // 有輸入方向 -> 表示正在移動（要播放動畫）
        isMoving = true;

        bool parentFlipped = playerTransform != null && playerTransform.localScale.x < 0f;
        float effectiveDirX = parentFlipped ? -dirX : dirX;

        string newDir = "";

        if (Mathf.Abs(effectiveDirX) > Mathf.Abs(dirY))
        {
            newDir = (effectiveDirX > 0f) ? "Right" : "Left";
        }
        else
        {
            newDir = (dirY > 0f) ? "Up" : "Down";
        }

        // 若方向改變，重置動畫起始幀（並顯示第一幀）
        if (newDir != currentMoveDir)
        {
            currentMoveDir = newDir;
            animIndex = 0;
            animTimer = 0f;

            // 顯示第一張（如果有幀陣列就顯示第一張，否則顯示靜態圖）
            Sprite[] frames = GetFramesForDirection(currentMoveDir);
            if (frames != null && frames.Length > 0)
                sr.sprite = frames[animIndex];
            else
                ShowStaticHair(currentMoveDir);
        }

        if (enableDebug && newDir != lastDir)
        {
            Debug.Log($"HairController: dirX={dirX:F2}, dirY={dirY:F2}, flipped={parentFlipped} => {newDir}, sprite={sr.sprite?.name}");
            lastDir = newDir;
        }
    }

    private Sprite[] GetFramesForDirection(string dir)
    {
        switch (dir)
        {
            case "Up": return hairUpFrames;
            case "Down": return hairDownFrames;
            case "Left": return hairLeftFrames;
            case "Right": return hairRightFrames;
            default: return null;
        }
    }

    private void AnimateHair()
    {
        animTimer += Time.deltaTime;

        if (animTimer >= animationInterval)
        {
            animTimer = 0f;
            Sprite[] frames = GetFramesForDirection(currentMoveDir);

            // 若該方向沒動畫幀，保留目前 sprite（不改為靜態）
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            animIndex = (animIndex + 1) % frames.Length;
            sr.sprite = frames[animIndex];
        }
    }

    private void ShowStaticHair(string dir)
    {
        switch (dir)
        {
            case "Up": sr.sprite = hairUp; break;
            case "Down": sr.sprite = hairDown; break;
            case "Left": sr.sprite = hairLeft; break;
            case "Right": sr.sprite = hairRight; break;
            default: sr.sprite = hairDown; break;
        }
    }
}
