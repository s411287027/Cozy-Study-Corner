using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions; // 必須新增這個命名空間
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
            // =========== ⭐ 防止閃爍：初始化時先全部隱藏 ===========
            // 這樣在 Firebase 資料回來之前，畫面上就不會有殘留的圖片
            Transform imageObj = seat.Find("Image");
            Transform labelObj = seat.Find("Label");

            if (imageObj != null) imageObj.gameObject.SetActive(false);
            if (labelObj != null) labelObj.gameObject.SetActive(false);

            // LeaveButton 也應該預設隱藏
            leaveBtn.gameObject.SetActive(false);
            // ===================================================
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
                Image seatImage = seat.transform.Find("Image").GetComponent<Image>();
                bool isEmpty = string.IsNullOrEmpty(uid) || uid == "null";

                if (isEmpty)
                {
                    if (seatImage != null) seatImage.gameObject.SetActive(false);
                    if (label != null) label.gameObject.SetActive(false);
                    label.text = "No person";
                    leaveBtn.gameObject.SetActive(false);
                    sitBtn.gameObject.SetActive(currentSeat == null);
                }
                else
                {
                    if (seatImage != null) seatImage.gameObject.SetActive(true);
                    if (label != null) label.gameObject.SetActive(true);
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


    // 保存目前顯示按鈕的 ClickArea
    private SeatClickArea_Library activeClickArea = null;

    // ⭐ 點擊座位顯示按鈕
    public void OnSeatClicked(string seatId, SeatClickArea_Library clickArea)
    {
        string path = $"Seat/Library/{currentRoomID}/{seatId}";

        rootRef.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || task.IsFaulted) return;

            string uid = task.Result.Value?.ToString();

            if (string.IsNullOrEmpty(uid) || uid == "null") return;
            if (uid == currentUID) return;

            // 隱藏上一個按鈕
            if (activeClickArea != null && activeClickArea != clickArea)
                activeClickArea.HideButtons();

            clickArea.ShowButtons();
            activeClickArea = clickArea;
        });
    }

    // ⭐ 監聽全局點擊收起按鈕 (兼容新 Input System)
    void Update()
    {
        if (activeClickArea == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 判斷是否點擊在按鈕上
            if (!IsPointerOverUIObject(activeClickArea.addFriendButton.gameObject) &&
                !IsPointerOverUIObject(activeClickArea.stickyNoteButton.gameObject))
            {
                activeClickArea.HideButtons();
                activeClickArea = null;
            }
        }
    }



    // ⭐ 判斷滑鼠是否在指定 UI 元件上
    private bool IsPointerOverUIObject(GameObject obj)
    {
        if (obj == null) return false;

        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        foreach (var result in results)
        {
            if (result.gameObject == obj)
                return true;
        }
        return false;
    }
}