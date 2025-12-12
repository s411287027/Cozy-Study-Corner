using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

[RequireComponent(typeof(Button))]
public class SeatClickArea_Forest : MonoBehaviour
{
    public string seatId;
    public SeatManager_Forest manager;

    [Header("UI Buttons")]
    public StickyNoteSystemController stickyNoteUI;
    public Button addFriendButton;
    public Button stickyNoteButton;

    private void Awake()
    {
        HideButtons();

        if (addFriendButton != null)
            addFriendButton.onClick.AddListener(OnAddFriendClicked);

        if (stickyNoteButton != null)
            stickyNoteButton.onClick.AddListener(OnStickyNoteClicked);

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClickAreaClicked);
    }

    private void OnClickAreaClicked()
    {
        manager?.OnSeatClicked(seatId, this);
    }

    // ⭐⭐⭐ [還原] 這邊完全恢復成你原本的邏輯，確保功能正常 ⭐⭐⭐
    private void OnAddFriendClicked()
    {
        Debug.Log($"[SeatClickArea_Forest] 嘗試加好友，座位: {seatId}");
        HideButtons();

        if (manager == null)
        {
            Debug.LogError("❌ SeatClickArea_Forest 找不到 SeatManager_Forest 引用！");
            return;
        }

        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Forest/{currentRoom}/{seatId}";

        FirebaseDatabase.DefaultInstance.RootReference.Child(path)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ 查詢座位失敗");
                    return;
                }

                string targetUid = task.Result.Value?.ToString();

                if (string.IsNullOrEmpty(targetUid) || targetUid == "null")
                {
                    Debug.LogWarning("⚠️ 該座位目前沒有人");
                    return;
                }

                Debug.Log($"✅ 找到目標 UID: {targetUid}，準備發送邀請...");

                if (FriendSystemController.Instance != null)
                {
                    FriendSystemController.Instance.SendFriendRequest(targetUid);
                }
                else
                {
                    Debug.LogError("❌ FriendSystemController 尚未初始化！");
                }
            });
    }

    private void OnStickyNoteClicked()
    {
        Debug.Log($"[SeatClickArea_Forest] 點擊便條紙功能: {seatId}");
        HideButtons();

        if (manager == null)
        {
            Debug.LogError("❌ SeatClickArea_Forest 找不到 SeatManager_Forest 引用！");
            return;
        }
        if (stickyNoteUI == null)
        {
            Debug.LogError("❌ stickyNoteUI 沒有指派！請在 Inspector 拖 StickyNoteSystemController 進來");
            return;
        }

        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Forest/{currentRoom}/{seatId}";

        FirebaseDatabase.DefaultInstance.RootReference.Child(path)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ 查詢座位失敗");
                    return;
                }

                string targetUid = task.Result.Value?.ToString();

                if (string.IsNullOrEmpty(targetUid) || targetUid == "null")
                {
                    Debug.LogWarning("⚠️ 該座位目前沒有人");
                    return;
                }

                // ✅ 打開「傳便利貼」UI，並設定目標 UID
                stickyNoteUI.OpenSendPanel(targetUid);
            });
    }


    // ⭐⭐⭐ [修改] 顯示按鈕時，額外去檢查狀態來決定是否變灰 ⭐⭐⭐
    public void ShowButtons()
    {
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(true);

        if (addFriendButton != null)
        {
            addFriendButton.gameObject.SetActive(true);
            // 預設先開啟，等檢查結果回來再決定要不要關掉
            addFriendButton.interactable = true;

            // 呼叫檢查狀態的方法 (純視覺更新，不影響點擊邏輯)
            CheckFriendStatusForUI();
        }
    }

    // 這是新增的輔助方法，專門用來控制 UI 變灰
    private void CheckFriendStatusForUI()
    {
        if (manager == null) return;
        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Forest/{currentRoom}/{seatId}";

        FirebaseDatabase.DefaultInstance.RootReference.Child(path)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled) return;

                string uidOnSeat = task.Result.Value?.ToString();

                // 如果座位沒人，或是資料錯誤，或是 FriendSystemController 沒準備好
                if (string.IsNullOrEmpty(uidOnSeat) || uidOnSeat == "null" || FriendSystemController.Instance == null)
                {
                    return;
                }

                // 使用剛剛在 FriendSystemController 新增的方法檢查
                bool shouldDisable = FriendSystemController.Instance.CheckIsFriendOrRequested(uidOnSeat);

                if (shouldDisable)
                {
                    if (addFriendButton != null)
                    {
                        addFriendButton.interactable = false; // 變灰且不能點
                        // 如果你有 Text 組件想改文字，也可以在這裡改
                        // var text = addFriendButton.GetComponentInChildren<TMPro.TMP_Text>();
                        // if(text) text.text = "已添加";
                    }
                }
            });
    }

    public void HideButtons()
    {
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(false);
    }
}