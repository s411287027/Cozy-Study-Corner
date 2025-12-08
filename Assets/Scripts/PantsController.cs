using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class PantsController : MonoBehaviour
{
    [Header("不同方向的靜態褲子圖片（預設用）")]
    public Sprite pantsUp;
    public Sprite pantsDown;
    public Sprite pantsLeft;
    public Sprite pantsRight;

    [Header("不同方向的動態動畫幀（可多張）")]
    public Sprite[] pantsUpFrames;
    public Sprite[] pantsDownFrames;
    public Sprite[] pantsLeftFrames;
    public Sprite[] pantsRightFrames;

    [Header("動畫設定")]
    public float animationInterval = 0.15f;

    [Header("位置 offset（相對 parent 的 localPosition）")]
    public Vector3 baseLocalOffset = Vector3.zero;

    [Header("移動偵測設定")]
    public float movementThreshold = 0.001f;

    [Header("除錯")]
    public bool enableDebug = true;

    private SpriteRenderer sr;
    private Transform playerTransform;
    private string lastDir = "";
    private bool initializedOffset = false;

    private float animTimer = 0f;
    private int animIndex = 0;
    private string currentMoveDir = "";
    private bool isMoving = false;
    private Vector3 prevPlayerPos;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        /*if (sceneName == "DressScene")
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sprite = pantsDown;
            enabled = false;
            return;
        }*/

        playerTransform = transform.parent;

        /*if (playerTransform == null)
        {
            if (enableDebug)
                Debug.LogWarning("⚠ PantsController: Pants 不是 Player 的子物件，將自動搜尋 player_move。");

            playerTransform = FindObjectOfType<player_move>()?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("❌ 找不到 player_move！請確保 Pants 是 Player 的子物件。");
                return;
            }
        }*/
        // DressScene 只顯示正面，不播放走路動畫
        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            // 強制顯示正面
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sprite = pantsDown;
            enabled = false;
            return;
        }
        if (!initializedOffset)
        {
            baseLocalOffset = transform.localPosition;
            initializedOffset = true;

            if (enableDebug)
                Debug.Log($"PantsController: 初次設定 baseLocalOffset = {baseLocalOffset}");
        }

        if (sr != null && sr.sprite == null && pantsDown != null)
            sr.sprite = pantsDown;

        prevPlayerPos = playerTransform.position;
        UpdatePantsDirection(0f, -1f);
    }
    
    void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            return; 
        }
        if (playerTransform == null)
            return;

        transform.position = playerTransform.TransformPoint(baseLocalOffset);
        transform.localScale = Vector3.one;

        /*var playerSR = playerTransform.GetComponent<SpriteRenderer>();
        var pantsSR = GetComponent<SpriteRenderer>();
        if (playerSR != null && pantsSR != null)
        {
            pantsSR.sortingLayerName = playerSR.sortingLayerName;
            pantsSR.sortingOrder = playerSR.sortingOrder + 2;
        }*/

        Vector3 currPos = playerTransform.position;
        float moved = (currPos - prevPlayerPos).sqrMagnitude;
        prevPlayerPos = currPos;

        if (enableDebug && moved < (movementThreshold * movementThreshold))
        {
            //Debug.Log("PantsController: Player idle (detected by movement).");
        }

        if (isMoving)
            AnimatePants();
    }

    public void UpdatePantsDirection(float dirX, float dirY)
    {
        if (SceneManager.GetActiveScene().name == "DressScene")
        {
            isMoving = false; // 確保動畫停止
            ShowStaticPants("Down"); // 強制顯示 Down（正面）靜態圖
            // 且因為 Start 已經 disabled 整個 Component， LateUpdate 不會執行 AnimateHair
            return; 
        }
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (Mathf.Abs(dirX) < 0.001f && Mathf.Abs(dirY) < 0.001f)
        {
            if (isMoving) // 只有當它剛從移動轉為停止時才執行一次
            {
                // 保留 currentMoveDir，並顯示該方向的第一幀（或靜態圖）
                animIndex = 0;
                animTimer = 0f;
                
                Sprite[] frames = GetFramesForDirection(currentMoveDir);
                if (frames != null && frames.Length > 0)
                    sr.sprite = frames[animIndex]; // 顯示動畫第一幀
                else
                    ShowStaticPants(currentMoveDir); // 顯示靜態圖

                if (enableDebug)
                    Debug.Log($"Pants: Idle. Displaying first frame for direction {currentMoveDir}");
            }
            
            //if (enabled)
            //    enabled = false;
            isMoving = false;
            return;
        }
        //if (!enabled)
        //    enabled = true;
        isMoving = true;

        bool parentFlipped = playerTransform != null && playerTransform.localScale.x < 0f;
        float effectiveDirX = parentFlipped ? -dirX : dirX;

        string newDir = "";

        if (Mathf.Abs(effectiveDirX) > Mathf.Abs(dirY))
            newDir = (effectiveDirX > 0f) ? "Right" : "Left";
        else
            newDir = (dirY > 0f) ? "Up" : "Down";

        if (newDir != currentMoveDir)
        {
            currentMoveDir = newDir;
            animIndex = 0;
            animTimer = 0f;

            Sprite[] frames = GetFramesForDirection(currentMoveDir);
            if (frames != null && frames.Length > 0)
                sr.sprite = frames[animIndex];
            else
                ShowStaticPants(currentMoveDir);
        }

        if (enableDebug && newDir != lastDir)
        {
            //Debug.Log($"PantsController: dirX={dirX:F2}, dirY={dirY:F2}, flipped={parentFlipped} => {newDir}, sprite={sr.sprite?.name}");
            lastDir = newDir;
        }
    }

    private Sprite[] GetFramesForDirection(string dir)
    {
        switch (dir)
        {
            case "Up": return pantsUpFrames;
            case "Down": return pantsDownFrames;
            case "Left": return pantsLeftFrames;
            case "Right": return pantsRightFrames;
            default: return null;
        }
    }

    private void AnimatePants()
    {
        animTimer += Time.deltaTime;

        if (animTimer >= animationInterval)
        {
            animTimer = 0f;
            Sprite[] frames = GetFramesForDirection(currentMoveDir);

            if (frames == null || frames.Length == 0)
                return;

            animIndex = (animIndex + 1) % frames.Length;
            sr.sprite = frames[animIndex];
        }
    }

    private void ShowStaticPants(string dir)
    {
        switch (dir)
        {
            case "Up": sr.sprite = pantsUp; break;
            case "Down": sr.sprite = pantsDown; break;
            case "Left": sr.sprite = pantsLeft; break;
            case "Right": sr.sprite = pantsRight; break;
            default: sr.sprite = pantsDown; break;
        }
    }
    public void ForceUpdatePantsSprite(float dirX, float dirY)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        isMoving = false;

        // 沿用 UpdatePantsDirection 的方向判斷邏輯
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
            ShowStaticPants(currentDir);
        }
        enabled = true;
    }
    public void UpdateSortingOrder(int newOrder)
    {
        if (sr != null)
        {
            sr.sortingOrder = newOrder;
        }
    }
}
