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

    private Vector3 prevPlayerPos;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // 👉 在 DressScene：Face 停住 + 面向前（Down）
        if (sceneName == "DressScene")
        {
            if (sr != null && faceDown != null)
                sr.sprite = faceDown;

            enabled = false;
            return;
        }

        playerTransform = transform.parent;

        if (playerTransform == null)
        {
            playerTransform = FindObjectOfType<player_move>()?.transform;
            if (playerTransform == null)
            {
                //Debug.LogError("FaceController: 找不到 player_move 或無 parent!");
                return;
            }
        }

        if (!initializedOffset)
        {
            baseLocalOffset = transform.localPosition;
            initializedOffset = true;
        }

        prevPlayerPos = playerTransform.position;

        // 初始面朝前
        UpdateFaceDirection(0f, -1f);
    }

    void LateUpdate()
    {
        if (playerTransform == null)
            return;

        // 保持 local offset
        transform.position = playerTransform.TransformPoint(baseLocalOffset);
        transform.localScale = Vector3.one;

        // 跟隨 sorting layer
        var playerSR = playerTransform.GetComponent<SpriteRenderer>();
        var mySR = GetComponent<SpriteRenderer>();

        if (playerSR != null && mySR != null)
        {
            mySR.sortingLayerName = playerSR.sortingLayerName;
            mySR.sortingOrder = playerSR.sortingOrder + 1;
        }
    }

    /// <summary>
    /// player 每幀呼叫：UpdateFaceDirection(input.x, input.y)
    /// </summary>
    public void UpdateFaceDirection(float dirX, float dirY)
    {
        if (sr == null) return;

        // 沒輸入 → idle，不換面
        if (Mathf.Abs(dirX) < 0.001f && Mathf.Abs(dirY) < 0.001f)
            return;

        // 修正 flip 狀態（若玩家左右翻轉）
        bool parentFlipped = playerTransform != null && playerTransform.localScale.x < 0f;
        float effectiveDirX = parentFlipped ? -dirX : dirX;

        string newDir;

        if (Mathf.Abs(effectiveDirX) > Mathf.Abs(dirY))
        {
            newDir = (effectiveDirX > 0f) ? "Right" : "Left";
        }
        else
        {
            newDir = (dirY > 0f) ? "Up" : "Down";
        }

        if (newDir == lastDir)
            return;

        lastDir = newDir;

        // 換 sprite
        switch (newDir)
        {
            case "Up": sr.sprite = faceUp; break;
            case "Down": sr.sprite = faceDown; break;
            case "Left": sr.sprite = faceLeft; break;
            case "Right": sr.sprite = faceRight; break;
        }
    }
}
