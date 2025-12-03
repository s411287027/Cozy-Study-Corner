using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions; // ⭐ 為了 ContinueWithOnMainThread
using System.Collections.Generic;

public class SeatManager_Coffee : MonoBehaviour
{
    public Transform seatsParent;
    public GameObject homeButton;

    // ⭐ 房間變數
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

        // ⭐ 啟動時，連線到預設房間
        ConnectToRoom(currentRoomID);
    }

    // ⭐ 核心函式：切換房間的邏輯 (供外部呼叫)
    public void ConnectToRoom(string roomId)
    {
        // 1. 如果之前有監聽別的房間，先取消監聽
        if (roomRef != null)
        {
            roomRef.ValueChanged -= OnSeatDataChanged;
        }

        // 2. 更新房間 ID
        currentRoomID = roomId;
        Debug.Log($"[SeatManager_Coffee] 切換操作目標至：{currentRoomID}");

        // 3. 設定新的監聽路徑：Seat/Coffee/RoomX
        roomRef = FirebaseDatabase.DefaultInstance.GetReference($"Seat/Coffee/{currentRoomID}");
        roomRef.ValueChanged += OnSeatDataChanged;
    }

    private void OnDestroy()
    {
        // 移除 roomRef 的監聽
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

        // Snapshot 現在是 RoomX 的資料
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
                    // 只要我坐著，其他空位也隱藏坐下按鈕
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
        {
            Debug.Log("❌ 你已經坐在其他位置，不能再坐！");
            return;
        }

        if (homeButton != null) homeButton.SetActive(false);

        // ⭐ 寫入路徑加入 Room ID
        string seatPath = $"Seat/Coffee/{currentRoomID}/{seatId}";

        rootRef.Child(seatPath).SetValueAsync(currentUID).ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"✅ 已在 {currentRoomID} 坐下：{seatId}");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError($"❌ 坐下失敗：{task.Exception.Message}");
            }
        });
    }

    private void OnLeaveButtonClicked(string seatId)
    {
        if (seatId != currentSeat) return;

        if (homeButton != null) homeButton.SetActive(true);

        // ⭐ 寫入路徑加入 Room ID
        string seatPath = $"Seat/Coffee/{currentRoomID}/{seatId}";

        rootRef.Child(seatPath).SetValueAsync("").ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"🏃 已離開 {currentRoomID} 座位：{seatId}");
                currentSeat = null;
            }
        });
    }
}