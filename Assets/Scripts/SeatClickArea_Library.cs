using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

[RequireComponent(typeof(Button))]
public class SeatClickArea_Library : MonoBehaviour
{
    public string seatId;
    public SeatManager_Library manager;

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

    private void OnAddFriendClicked()
    {
        Debug.Log($"[SeatClickArea_Library] 嘗試加好友，座位: {seatId}");
        HideButtons();

        if (manager == null)
        {
            Debug.LogError("❌ SeatClickArea_Library 找不到 SeatManager_Library 引用！");
            return;
        }

        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Library/{currentRoom}/{seatId}";

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

                if (FriendSystemController.Instance != null)
                    FriendSystemController.Instance.SendFriendRequest(targetUid);
                else
                    Debug.LogError("❌ FriendSystemController 尚未初始化！");
            });
    }

    private void OnStickyNoteClicked()
    {
        Debug.Log($"[SeatClickArea_Library] 點擊便條紙功能: {seatId}");
        HideButtons();

        if (manager == null)
        {
            Debug.LogError("❌ SeatClickArea_Library 找不到 SeatManager_Library 引用！");
            return;
        }
        if (stickyNoteUI == null)
        {
            Debug.LogError("❌ stickyNoteUI 沒有指派！請在 Inspector 拖 StickyNoteSystemController 進來");
            return;
        }

        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Library/{currentRoom}/{seatId}";

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

                stickyNoteUI.OpenSendPanel(targetUid, "Library");
            });
    }

    public void ShowButtons()
    {
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(true);

        if (addFriendButton != null)
        {
            addFriendButton.gameObject.SetActive(true);
            addFriendButton.interactable = true;
            CheckFriendStatusForUI();
        }
    }

    private void CheckFriendStatusForUI()
    {
        if (manager == null) return;
        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Library/{currentRoom}/{seatId}";

        FirebaseDatabase.DefaultInstance.RootReference.Child(path)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled) return;

                string uidOnSeat = task.Result.Value?.ToString();

                if (string.IsNullOrEmpty(uidOnSeat) || uidOnSeat == "null" || FriendSystemController.Instance == null)
                    return;

                bool shouldDisable = FriendSystemController.Instance.CheckIsFriendOrRequested(uidOnSeat);
                if (shouldDisable && addFriendButton != null)
                    addFriendButton.interactable = false;
            });
    }

    public void HideButtons()
    {
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(false);
    }
}
