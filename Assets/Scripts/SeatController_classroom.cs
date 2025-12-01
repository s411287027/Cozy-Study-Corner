using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions; // ⭐ 必須新增：為了使用 ContinueWithOnMainThread
using System.Collections.Generic;

public class SeatManager_Classroom : MonoBehaviour
{
    public Transform seatsParent;
    public GameObject homeButton;

    private DatabaseReference dbRef;
    private string currentUID;

    private string currentSeat = null;
    private Dictionary<string, GameObject> seatObjects = new Dictionary<string, GameObject>();

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        currentUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        foreach (Transform seat in seatsParent)
        {
            string seatId = seat.name.Replace("Seat ", "").Replace("Seat_", "");
            seatObjects[seatId] = seat.gameObject;

            Button sitBtn = seat.Find("SitButton").GetComponent<Button>();
            Button leaveBtn = seat.Find("LeaveButton").GetComponent<Button>();

            sitBtn.onClick.AddListener(() => OnSitButtonClicked(seatId));
            leaveBtn.onClick.AddListener(() => OnLeaveButtonClicked(seatId));
        }

        FirebaseDatabase.DefaultInstance
            .GetReference("Seat/Classroom")
            .ValueChanged += OnSeatDataChanged;
    }

    private void OnDestroy()
    {
        if (dbRef != null)
            dbRef.ValueChanged -= OnSeatDataChanged;
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
                    // ⭐ 修改開始：這裡不再直接顯示 UID，而是去抓名字
                    // 先顯示載入中，避免空白
                    label.text = "Loading...";

                    // 呼叫函式讀取名字
                    UpdateLabelWithUserName(uid, label);
                    // ⭐ 修改結束

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

        // ... (下方按鈕狀態更新邏輯保持不變)
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
        }
    }

    // ⭐ 新增函式：根據 UID 去資料庫抓取名字
    private void UpdateLabelWithUserName(string targetUid, TMP_Text labelToUpdate)
    {
        // 假設你的使用者資料路徑是 users/UID/username
        // 如果你的名字欄位叫 name，請把 "username" 改成 "name"
        dbRef.Child("users").Child(targetUid).Child("UserName")
            .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("無法讀取名字");
                labelToUpdate.text = targetUid; // 失敗時至少顯示 UID
                return;
            }

            if (task.Result.Exists)
            {
                string userName = task.Result.Value.ToString();
                labelToUpdate.text = userName; // ✅ 成功顯示名字
            }
            else
            {
                labelToUpdate.text = "Unknown"; // 找不到名字資料
            }
        });
    }

    // ... (OnSitButtonClicked 和 OnLeaveButtonClicked 保持不變)
    private void OnSitButtonClicked(string seatId)
    {
        if (currentSeat != null) { Debug.Log("❌ 你已經坐在其他位置，不能再坐！"); return; }
        string seatPath = $"Seat/Classroom/{seatId}";
        dbRef.Child(seatPath).SetValueAsync(currentUID).ContinueWith(task =>
        {
            if (task.IsCompleted) { Debug.Log($"✅ 已坐下：{seatId}"); if (homeButton != null) homeButton.SetActive(false); }
        });
    }

    private void OnLeaveButtonClicked(string seatId)
    {
        if (seatId != currentSeat) return;
        string seatPath = $"Seat/Classroom/{seatId}";
        dbRef.Child(seatPath).SetValueAsync("").ContinueWith(task =>
        {
            if (task.IsCompleted) { Debug.Log($"🏃 已離開座位：{seatId}"); currentSeat = null; if (homeButton != null) homeButton.SetActive(true); }
        });
    }
}