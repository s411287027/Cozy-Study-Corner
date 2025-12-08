using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

[RequireComponent(typeof(Button))]
public class SeatClickArea : MonoBehaviour
{
    public string seatId;
    public SeatManager_Forest manager; // 必須要在 Inspector 拉入或動態抓取

    [Header("UI Buttons")]
    public Button addFriendButton;
    public Button stickyNoteButton;

    private void Awake()
    {
        // 初始化：隱藏按鈕
        HideButtons();

        // 綁定事件
        if (addFriendButton != null)
            addFriendButton.onClick.AddListener(OnAddFriendClicked);

        if (stickyNoteButton != null)
            stickyNoteButton.onClick.AddListener(OnStickyNoteClicked);

        // 設定 ClickArea 本身的點擊 (點擊座位顯示選單)
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClickAreaClicked);
    }

    private void OnClickAreaClicked()
    {
        // 通知 Manager 處理點擊邏輯 (Manager 會判斷是否顯示按鈕)
        manager?.OnSeatClicked(seatId, this);
    }

    // ⭐⭐⭐ 重點修改：發送好友邀請邏輯 ⭐⭐⭐
    private void OnAddFriendClicked()
    {
        Debug.Log($"[SeatClickArea] 嘗試加好友，座位: {seatId}");
        HideButtons(); // 點擊後立刻隱藏選單

        if (manager == null)
        {
            Debug.LogError("❌ SeatClickArea 找不到 SeatManager_Forest 引用！");
            return;
        }

        // 1. 取得資料庫路徑 (配合 SeatManager_Forest 的結構)
        // 路徑結構：Seat/Forest/{RoomID}/{SeatID}
        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Forest/{currentRoom}/{seatId}";

        // 2. 查詢該座位的 UID
        FirebaseDatabase.DefaultInstance.RootReference.Child(path)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ 查詢座位失敗");
                    return;
                }

                string targetUid = task.Result.Value?.ToString();

                // 3. 驗證 UID 是否有效
                if (string.IsNullOrEmpty(targetUid) || targetUid == "null")
                {
                    Debug.LogWarning("⚠️ 該座位目前沒有人 (資料庫值為空)");
                    return;
                }

                Debug.Log($"✅ 找到目標 UID: {targetUid}，準備發送邀請...");

                // 4. 呼叫好友系統控制器發送邀請
                if (FriendSystemController.Instance != null)
                {
                    FriendSystemController.Instance.SendFriendRequest(targetUid);
                }
                else
                {
                    Debug.LogError("❌ FriendSystemController 尚未初始化！請確認場景中是否有該物件。");
                }
            });
    }

    private void OnStickyNoteClicked()
    {
        Debug.Log($"[SeatClickArea] 點擊便條紙功能: {seatId}");
        HideButtons();
        // 這裡未來可以擴充便條紙功能
    }

    public void ShowButtons()
    {
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(true);
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(true);
    }

    public void HideButtons()
    {
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(false);
    }
}