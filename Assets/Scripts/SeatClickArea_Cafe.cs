using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

[RequireComponent(typeof(Button))]
public class SeatClickArea_Cafe : MonoBehaviour
{
    public string seatId;
    public SeatManager_Coffee manager;

    [Header("UI Buttons")]
    public StickyNoteSystemController stickyNoteUI;
    public Button addFriendButton;
    public Button stickyNoteButton;

    private void Awake()
    {
        HideButtons();

        if (addFriendButton)
            addFriendButton.onClick.AddListener(OnAddFriendClicked);

        if (stickyNoteButton)
            stickyNoteButton.onClick.AddListener(OnStickyNoteClicked);

        var btn = GetComponent<Button>();
        if (btn)
            btn.onClick.AddListener(OnClickAreaClicked);
    }

    private void OnClickAreaClicked()
    {
        manager?.OnSeatClicked(seatId, this);
    }

    private void OnAddFriendClicked()
    {
        Debug.Log($"[SeatClickArea_Cafe] 嘗試加好友，座位: {seatId}");
        HideButtons();

        if (manager == null)
        {
            Debug.LogError("❌ SeatClickArea_Cafe 找不到 SeatManager_Coffee");
            return;
        }

        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Coffee/{currentRoom}/{seatId}"; // ✅ 關鍵在這

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

                FriendSystemController.Instance?.SendFriendRequest(targetUid);
            });
    }

    private void OnStickyNoteClicked()
    {
        Debug.Log($"[SeatClickArea_Cafe] 點擊便條紙功能: {seatId}");
        HideButtons();

        if (manager == null)
        {
            Debug.LogError("❌ SeatClickArea_Cafe 找不到 SeatManager_Coffee");
            return;
        }

        if (stickyNoteUI == null)
        {
            Debug.LogError("❌ stickyNoteUI 沒有指派");
            return;
        }

        string currentRoom = manager.currentRoomID;
        string path = $"Seat/Coffee/{currentRoom}/{seatId}"; // ✅ 關鍵在這

        Debug.Log($"[SeatClickArea_Cafe] query seat path = {path}");

        FirebaseDatabase.DefaultInstance.RootReference.Child(path)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ 查詢座位失敗");
                    return;
                }

                string targetUid = task.Result.Value?.ToString();
                Debug.Log($"[SeatClickArea_Cafe] seat value (Sticky) = {targetUid}");

                if (string.IsNullOrEmpty(targetUid) || targetUid == "null")
                {
                    Debug.LogWarning("⚠️ 該座位目前沒有人");
                    return;
                }

                // ✅ 一定會進來
                stickyNoteUI.OpenSendPanel(targetUid, "Cafe");
            });
    }

    public void ShowButtons()
    {
        if (stickyNoteButton) stickyNoteButton.gameObject.SetActive(true);

        if (addFriendButton)
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
        string path = $"Seat/Coffee/{currentRoom}/{seatId}";

        FirebaseDatabase.DefaultInstance.RootReference.Child(path)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled) return;

                string uidOnSeat = task.Result.Value?.ToString();
                if (string.IsNullOrEmpty(uidOnSeat) || uidOnSeat == "null") return;

                if (FriendSystemController.Instance != null &&
                    FriendSystemController.Instance.CheckIsFriendOrRequested(uidOnSeat))
                {
                    addFriendButton.interactable = false;
                }
            });
    }

    public void HideButtons()
    {
        if (addFriendButton) addFriendButton.gameObject.SetActive(false);
        if (stickyNoteButton) stickyNoteButton.gameObject.SetActive(false);
    }
}
