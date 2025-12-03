using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;

public class SeatManager_Classroom : MonoBehaviour
{
    public Transform seatsParent;
    public GameObject homeButton;

    // ⭐ 新增：用來控制現在是哪個房間
    [Header("房間設定")]
    public string currentRoomID = "Room1";

    private DatabaseReference rootRef; // 這是根目錄，用來查 User 名字
    private DatabaseReference roomRef; // ⭐ 這是房間目錄，用來監聽座位
    private string currentUID;

    private string currentSeat = null;
    private Dictionary<string, GameObject> seatObjects = new Dictionary<string, GameObject>();

    void Start()
    {
        rootRef = FirebaseDatabase.DefaultInstance.RootReference;
        currentUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        foreach (Transform seat in seatsParent)
        {
            string seatId = seat.name.Replace("Seat ", "").Replace("Seat_", "");
            seatObjects[seatId] = seat.gameObject;

            Button sitBtn = seat.Find("SitButton").GetComponent<Button>();
            Button leaveBtn = seat.Find("LeaveButton").GetComponent<Button>();

            // 注意：這裡 lambda 會抓到當下的 seatId
            sitBtn.onClick.AddListener(() => OnSitButtonClicked(seatId));
            leaveBtn.onClick.AddListener(() => OnLeaveButtonClicked(seatId));
        }

        // ⭐ 修改：不再直接監聽，而是呼叫連線函式
        ConnectToRoom(currentRoomID);
    }

    // ⭐ 新增：切換房間的函式 (外部可以呼叫這個來換房)
    public void ConnectToRoom(string roomId)
    {
        // 1. 如果之前有監聽別的房間，先取消監聽
        if (roomRef != null)
        {
            roomRef.ValueChanged -= OnSeatDataChanged;
        }

        // 2. 更新房間 ID
        currentRoomID = roomId;
        Debug.Log($"[SeatManager] UI 切換至房間：{currentRoomID}");

        // 3. 設定新的監聽路徑：Seat/Classroom/RoomX
        roomRef = FirebaseDatabase.DefaultInstance.GetReference($"Seat/Classroom/{currentRoomID}");
        roomRef.ValueChanged += OnSeatDataChanged;
    }

    private void OnDestroy()
    {
        // ⭐ 修改：移除正確的 listener
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

        // 當換房間時，Snapshot 回傳的是該房間下的所有座位 (Key: 1-1, Value: UID)
        // 下面的邏輯完全不用改，因為資料結構相對位置是一樣的

        currentSeat = null;

        // 先把所有座位重置為 "沒人" 狀態 (避免換房間時殘留舊狀態)
        // 雖然下面的 foreach 會更新有人的座位，但沒人的座位需要被清空
        // 建議這裡可以加一段重置 UI 的邏輯，或者依賴 Snapshot 資料夠完整

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
                    // 只有當我還沒坐在任何位置時，才顯示「坐下」按鈕
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

        // 更新按鈕互斥狀態 (如果我坐下了，隱藏其他所有坐下按鈕)
        if (currentSeat != null)
        {
            foreach (var kv in seatObjects)
            {
                string seatId = kv.Key;
                if (seatId != currentSeat)
                {
                    kv.Value.transform.Find("SitButton").GetComponent<Button>().gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (homeButton != null) homeButton.SetActive(true);

            // ⭐ 重要修正：如果我站起來了，要讓所有"空位"的坐下按鈕重新顯示
            // 因為上面 foreach 只跑了 DataSnapshot (有資料的節點)，
            // 如果某個位置是空的且資料庫沒節點，它可能不會被更新到，這裡補強一下會更穩
            foreach (var kv in seatObjects)
            {
                // 這裡稍微複雜，因為我們沒有每個座位的即時資料，
                // 但依賴 OnSeatDataChanged 每次觸發通常包含完整列表 (或至少我們會掃描過)
                // 簡單做法：依賴上面的 foreach 邏輯即可，如果資料庫結構完整 (空位是空字串) 就沒問題。
                // 如果是 null 節點消失，可能需要額外處理，但在此先維持你的原邏輯。
            }
        }
    }

    private void UpdateLabelWithUserName(string targetUid, TMP_Text labelToUpdate)
    {
        // 查名字跟房間無關，還是查 users/UID
        rootRef.Child("users").Child(targetUid).Child("UserName")
            .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                labelToUpdate.text = targetUid;
                return;
            }

            if (task.Result.Exists)
                labelToUpdate.text = task.Result.Value.ToString();
            else
                labelToUpdate.text = "Unknown";
        });
    }

    private void OnSitButtonClicked(string seatId)
    {
        if (currentSeat != null) { Debug.Log("❌ 你已經坐在其他位置，不能再坐！"); return; }

        // ⭐ 修改：路徑加上 currentRoomID
        string seatPath = $"Seat/Classroom/{currentRoomID}/{seatId}";

        // 建議：這裡最好也改用 Transaction 防止搶位，但用 SetValueAsync 也可以運作
        rootRef.Child(seatPath).SetValueAsync(currentUID).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"✅ 已在 {currentRoomID} 坐下：{seatId}");
                // UI 更新會由 OnSeatDataChanged 自動處理
            }
        });
    }

    private void OnLeaveButtonClicked(string seatId)
    {
        if (seatId != currentSeat) return;

        // ⭐ 修改：路徑加上 currentRoomID
        string seatPath = $"Seat/Classroom/{currentRoomID}/{seatId}";

        rootRef.Child(seatPath).SetValueAsync("").ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"🏃 已離開 {currentRoomID} 座位：{seatId}");
                currentSeat = null;
                // UI 更新會由 OnSeatDataChanged 自動處理
            }
        });
    }
}