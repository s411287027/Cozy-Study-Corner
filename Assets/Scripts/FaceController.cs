using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class FaceController : MonoBehaviour
{
    [Header("不同方向的臉部圖片")]
    public Sprite faceUp;
    public Sprite faceDown;
    public Sprite faceLeft;
    public Sprite faceRight;

    [Header("不同方向的動態動畫幀（可多張）")]
    public Sprite[] faceUpFrames;
    public Sprite[] faceDownFrames;
    public Sprite[] faceLeftFrames;
    public Sprite[] faceRightFrames;

    [Header("動畫設定")]
    [Tooltip("當持續朝同方向移動時，幀與幀之間的時間（秒）")]
    public float animationInterval = 0.15f;

    [Header("位置 offset（相對 parent 的 localPosition）")]
    public Vector3 baseLocalOffset = Vector3.zero;

    [Header("移動偵測設定")]
    public float movementThreshold = 0.001f;

    [Header("除錯")]
    public bool enableDebug = false;

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
        string sceneName = SceneManager.GetActiveScene().name;

        //  在 DressScene：Face 停住 + 面向前（Down）
        if (sceneName == "DressScene")
        {
            if (sr != null && faceDown != null)
                sr.sprite = faceDown;

            enabled = false;
            return;
        }

        playerTransform = transform.parent;

        /*if (playerTransform == null)
        {
            playerTransform = FindObjectOfType<player_move>()?.transform;
            if (playerTransform == null)
            {
                //Debug.LogError("FaceController: 找不到 player_move 或無 parent!");
                return;
            }
        }*/
        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            // 強制顯示正面
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sprite = faceDown;
            enabled = false;
            return;
        }
        
        if (!initializedOffset)
        {
            baseLocalOffset = transform.localPosition;
            initializedOffset = true;
        }

        if (sr != null && sr.sprite == null && faceDown != null)
            sr.sprite = faceDown;

        prevPlayerPos = playerTransform.position;

        // 初始面朝前
        UpdateFaceDirection(0f, -1f);
    }
    
    void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            return; 
        }
        if (playerTransform == null)
            return;

        // 保持 local offset
        transform.position = playerTransform.TransformPoint(baseLocalOffset);
        transform.localScale = Vector3.one;

        /*// 跟隨 sorting layer
        var playerSR = playerTransform.GetComponent<SpriteRenderer>();
        var faceSR = GetComponent<SpriteRenderer>();

        if (playerSR != null && faceSR != null)
        {
            faceSR.sortingLayerName = playerSR.sortingLayerName;
            faceSR.sortingOrder = playerSR.sortingOrder + 4;
        }*/

        // ===== 自行偵測 parent 是否移動（若 player_move 忘了每幀傳 0,0，這能保障） =====
        Vector3 currPos = playerTransform.position;
        float moved = (currPos - prevPlayerPos).sqrMagnitude;
        prevPlayerPos = currPos;

        if (isMoving)
            AnimateFace();
    }

    /// <summary>
    /// player 每幀呼叫：UpdateFaceDirection(input.x, input.y)
    /// </summary>
    public void UpdateFaceDirection(float dirX, float dirY)
    {
        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            isMoving = false; // 確保動畫停止
            ShowStaticFace("Down"); // 強制顯示 Down（正面）靜態圖
            // 且因為 Start 已經 disabled 整個 Component， LateUpdate 不會執行 AnimateHair
            return; 
        }

        if (sr == null) return;

        // 沒輸入 → idle，不換面
        if (Mathf.Abs(dirX) < 0.001f && Mathf.Abs(dirY) < 0.001f)
        {
            if (enabled)
                enabled = false;
            isMoving = false;
            return;
        }
        // 有輸入方向 -> 表示正在移動（要播放動畫）
        if (enabled)
            enabled = true;
        isMoving = true;

        // 修正 flip 狀態（若玩家左右翻轉）
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
                ShowStaticFace(currentMoveDir);
        }

        if (enableDebug && newDir != lastDir)
        {
            //Debug.Log($"FaceController: dirX={dirX:F2}, dirY={dirY:F2}, flipped={parentFlipped} => {newDir}, sprite={sr.sprite?.name}");
            lastDir = newDir;
        }
    }

    private Sprite[] GetFramesForDirection(string dir)
    {
        switch (dir)
        {
            case "Up": return faceUpFrames;
            case "Down": return faceDownFrames;
            case "Left": return faceLeftFrames;
            case "Right": return faceRightFrames;
            default: return null;
        }
    }

    private void AnimateFace()
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

    private void ShowStaticFace(string dir)
    {
        switch (dir)
        {
            case "Up": sr.sprite = faceUp; break;
            case "Down": sr.sprite = faceDown; break;
            case "Left": sr.sprite = faceLeft; break;
            case "Right": sr.sprite = faceRight; break;
            default: sr.sprite = faceDown; break;
        }
    }
    public void ForceUpdateFaceSprite(float dirX, float dirY)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // 沿用 UpdateFaceDirection 的方向判斷邏輯
        string currentDir = "";
        if (Mathf.Abs(dirX) > Mathf.Abs(dirY))
        {
            currentDir = (dirX > 0f) ? "Right" : "Left";
        }
        else
        {
            currentDir = (dirY > 0f) ? "Up" : "Down";
        }

        // 💡 保持內部狀態同步
        currentMoveDir = currentDir;
        animIndex = 0;
        animTimer = 0f;

        // 顯示第一張（如果有幀陣列就顯示第一張，否則顯示靜態圖）
        Sprite[] frames = GetFramesForDirection(currentDir);
        if (frames != null && frames.Length > 0)
        {
            sr.sprite = frames[animIndex];
        }
        else
        {
            ShowStaticFace(currentDir);
        }
    }
    public void UpdateSortingOrder(int newOrder)
    {
        if (sr != null)
        {
            sr.sortingOrder = newOrder;
        }
    }
}
