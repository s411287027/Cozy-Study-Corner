using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions; // 必須新增這個命名空間
using System.Collections.Generic;

public class SeatManager_Library : MonoBehaviour
{
    public Transform seatsParent;
    public GameObject homeButton;

    // ⭐ 修正 1: 新增房間變數
    [Header("房間設定")]
    public string currentRoomID = "Room1";

    private DatabaseReference rootRef; // 根目錄 (用來查名字和寫入資料)
    private DatabaseReference roomRef; // ⭐ 監聽座位用的 Reference

    private string currentUID;
    private string currentSeat = null;
    private Dictionary<string, GameObject> seatObjects = new Dictionary<string, GameObject>();

    void Start()
    {
        rootRef = FirebaseDatabase.DefaultInstance.RootReference;
        currentUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        // 收集所有座位
        foreach (Transform seat in seatsParent)
        {
            string seatId = seat.name.Replace("Seat ", "").Replace("Seat_", "");
            seatObjects[seatId] = seat.gameObject;

            Button sitBtn = seat.Find("SitButton").GetComponent<Button>();
            Button leaveBtn = seat.Find("LeaveButton").GetComponent<Button>();

            sitBtn.onClick.AddListener(() => OnSitButtonClicked(seatId));
            leaveBtn.onClick.AddListener(() => OnLeaveButtonClicked(seatId));
        }

        // ⭐ 修正 2: 啟動時連線到預設房間
        ConnectToRoom(currentRoomID);
    }

    // ⭐ 修正 3: 核心函式：切換房間的邏輯 (供外部 RoomManager 呼叫)
    public void ConnectToRoom(string roomId)
    {
        // 1. 如果之前有監聽別的房間，先取消監聽
        if (roomRef != null)
        {
            roomRef.ValueChanged -= OnSeatDataChanged;
        }

        // 2. 更新房間 ID
        currentRoomID = roomId;
        Debug.Log($"[SeatManager_Library] 切換操作目標至：{currentRoomID}");

        // 3. 設定新的監聽路徑：Seat/Library/RoomX
        roomRef = FirebaseDatabase.DefaultInstance.GetReference($"Seat/Library/{currentRoomID}");
        roomRef.ValueChanged += OnSeatDataChanged;
    }

    private void OnDestroy()
    {
        // ⭐ 修正 4: 移除 roomRef 的監聽
        if (roomRef != null)
            roomRef.ValueChanged -= OnSeatDataChanged;
    }

    private void OnSeatDataChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        currentSeat = null;

        foreach (var seatData in args.Snapshot.Children)
        {
            string seatId = seatData.Key;
            string uid = seatData.Value?.ToString();

            if (seatObjects.TryGetValue(seatId, out GameObject seat))
            {
                Button sitBtn = seat.transform.Find("SitButton").GetComponent<Button>();
                Button leaveBtn = seat.transform.Find("LeaveButton").GetComponent<Button>();
                TMP_Text label = seat.transform.Find("Label").GetComponent<TMP_Text>();

                bool isEmpty = string.IsNullOrEmpty(uid) || uid == "null";

                if (isEmpty)
                {
                    label.text = "No person";
                    leaveBtn.gameObject.SetActive(false);
                    sitBtn.gameObject.SetActive(currentSeat == null);
                }
                else
                {
                    // ⭐ 讀取名字
                    label.text = "Loading...";
                    UpdateLabelWithUserName(uid, label);

                    if (uid == currentUID)
                    {
                        currentSeat = seatId;
                        sitBtn.gameObject.SetActive(false);
                        leaveBtn.gameObject.SetActive(true);
                        if (homeButton != null) homeButton.SetActive(false);
                    }
                    else
                    {
                        sitBtn.gameObject.SetActive(false);
                        leaveBtn.gameObject.SetActive(false);
                    }
                }
            }
        }

        if (currentSeat != null)
        {
            foreach (var kv in seatObjects)
            {
                if (kv.Key != currentSeat)
                {
                    Button sitBtn = kv.Value.transform.Find("SitButton").GetComponent<Button>();
                    sitBtn.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (homeButton != null) homeButton.SetActive(true);
        }
    }

    // 讀取名字的函式 (使用 ContinueWithOnMainThread)
    private void UpdateLabelWithUserName(string targetUid, TMP_Text labelToUpdate)
    {
        rootRef.Child("users").Child(targetUid).Child("UserName")
            .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                labelToUpdate.text = targetUid;
                return;
            }

            if (task.Result.Exists)
            {
                labelToUpdate.text = task.Result.Value.ToString();
            }
            else
            {
                labelToUpdate.text = "Unknown";
            }
        });
    }

    private void OnSitButtonClicked(string seatId)
    {
        if (currentSeat != null)
            return;

        if (homeButton != null) homeButton.SetActive(false);

        // ⭐ 修正 5: 寫入路徑加入 Room ID
        string seatPath = $"Seat/Library/{currentRoomID}/{seatId}";

        rootRef.Child(seatPath).SetValueAsync(currentUID);
    }

    private void OnLeaveButtonClicked(string seatId)
    {
        if (seatId != currentSeat)
            return;

        if (homeButton != null) homeButton.SetActive(true);

        // ⭐ 修正 6: 寫入路徑加入 Room ID
        string seatPath = $"Seat/Library/{currentRoomID}/{seatId}";

        rootRef.Child(seatPath).SetValueAsync("");
        currentSeat = null;
    }
}