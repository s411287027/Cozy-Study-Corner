using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class ShoesController : MonoBehaviour
{
    [Header("不同方向的鞋子圖片")]
    public Sprite shoesUp;
    public Sprite shoesDown;
    public Sprite shoesLeft;
    public Sprite shoesRight;

    [Header("不同方向的動態動畫幀（可多張）")]
    public Sprite[] shoesUpFrames;
    public Sprite[] shoesDownFrames;
    public Sprite[] shoesLeftFrames;
    public Sprite[] shoesRightFrames;

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
        if (sceneName == "DressScene")
        {
            enabled = false;
            return;
        }

        playerTransform = transform.parent;

        if (playerTransform == null)
        {
            if (enableDebug)
                Debug.LogWarning("⚠ ShoesController: Shoes 不是 Player 的子物件，將自動搜尋 player_move。");

            playerTransform = FindObjectOfType<player_move>()?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("❌ 找不到 player_move！請確保 Shoes 是 Player 的子物件。");
                return;
            }
        }

        if (!initializedOffset)
        {
            baseLocalOffset = transform.localPosition;
            initializedOffset = true;

            if (enableDebug)
                Debug.Log($"ShoesController: 初次設定 baseLocalOffset = {baseLocalOffset}");
        }

        if (sr != null && sr.sprite == null && shoesDown != null)
            sr.sprite = shoesDown;

        prevPlayerPos = playerTransform.position;
        UpdateShoesDirection(1f, 0f);
    }

    void LateUpdate()
    {
        if (playerTransform == null)
            return;

        transform.position = playerTransform.TransformPoint(baseLocalOffset);
        transform.localScale = Vector3.one;

        var playerSR = playerTransform.GetComponent<SpriteRenderer>();
        var shoesSR = GetComponent<SpriteRenderer>();
        if (playerSR != null && shoesSR != null)
        {
            shoesSR.sortingLayerName = playerSR.sortingLayerName;
            shoesSR.sortingOrder = playerSR.sortingOrder + 1;
        }

        Vector3 currPos = playerTransform.position;
        float moved = (currPos - prevPlayerPos).sqrMagnitude;
        prevPlayerPos = currPos;

        if (enableDebug && moved < (movementThreshold * movementThreshold))
        {
            //Debug.Log("ShoesController: Player idle (detected by movement).");
        }

        if (isMoving)
            AnimateShoes();
    }

    public void UpdateShoesDirection(float dirX, float dirY)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (Mathf.Abs(dirX) < 0.001f && Mathf.Abs(dirY) < 0.001f)
        {
            isMoving = false;
            return;
        }

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
                ShowStaticShoes(currentMoveDir);
        }

        //if (enableDebug && newDir != lastDir)
        //{
        //Debug.Log($"ShoesController: dirX={dirX:F2}, dirY={dirY:F2}, flipped={parentFlipped} => {newDir}, sprite={sr.sprite?.name}");
        lastDir = newDir;
        //}
    }

    private Sprite[] GetFramesForDirection(string dir)
    {
        switch (dir)
        {
            case "Up": return shoesUpFrames;
            case "Down": return shoesDownFrames;
            case "Left": return shoesLeftFrames;
            case "Right": return shoesRightFrames;
            default: return null;
        }
    }

    private void AnimateShoes()
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

    private void ShowStaticShoes(string dir)
    {
        switch (dir)
        {
            case "Up": sr.sprite = shoesUp; break;
            case "Down": sr.sprite = shoesDown; break;
            case "Left": sr.sprite = shoesLeft; break;
            case "Right": sr.sprite = shoesRight; break;
            default: sr.sprite = shoesDown; break;
        }
    }
}