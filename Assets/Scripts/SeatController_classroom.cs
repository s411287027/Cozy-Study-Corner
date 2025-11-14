using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;

public class SeatManager_Classroom : MonoBehaviour
{
    public Transform seatsParent;  // 所有座位的父物件
    private DatabaseReference dbRef;
    private string currentUID;

    private string currentSeat = null;  // ⭐ 記錄玩家現在坐在哪個位置
    private Dictionary<string, GameObject> seatObjects = new Dictionary<string, GameObject>();

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
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

        // 監聽資料變化
        FirebaseDatabase.DefaultInstance
            .GetReference("Seat/Classroom")
            .ValueChanged += OnSeatDataChanged;
    }

    private void OnSeatDataChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // ⭐ 必須重置，重新從資料決定玩家的座位
        currentSeat = null;

        // 更新所有座位狀態
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
                    // 空位
                    label.text = "No person";
                    leaveBtn.gameObject.SetActive(false);

                    // ⭐ 若玩家沒坐，才允許按其他座位的 Sit
                    sitBtn.gameObject.SetActive(currentSeat == null);
                }
                else
                {
                    // 有玩家坐下
                    label.text = $"UID: {uid}";

                    if (uid == currentUID)
                    {
                        // 玩家自己坐在這
                        currentSeat = seatId;
                        sitBtn.gameObject.SetActive(false);
                        leaveBtn.gameObject.SetActive(true);
                    }
                    else
                    {
                        // 別人坐
                        sitBtn.gameObject.SetActive(false);
                        leaveBtn.gameObject.SetActive(false);
                    }
                }
            }
        }

        // ⭐ 第二輪調整：若玩家已坐下，所有其他空位要把 SitButton 關閉
        if (currentSeat != null)
        {
            foreach (var kv in seatObjects)
            {
                string seatId = kv.Key;
                GameObject seat = kv.Value;

                if (seatId != currentSeat)
                {
                    Button sitBtn = seat.transform.Find("SitButton").GetComponent<Button>();
                    sitBtn.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnSitButtonClicked(string seatId)
    {
        if (currentSeat != null)
        {
            Debug.Log("❌ 你已經坐在其他位置，不能再坐！");
            return;
        }

        string seatPath = $"Seat/Classroom/{seatId}";
        dbRef.Child(seatPath).SetValueAsync(currentUID).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"✅ 已坐下：{seatId}");
            }
            else
            {
                Debug.LogError($"❌ 坐下失敗：{seatId}, {task.Exception}");
            }
        });
    }

    private void OnLeaveButtonClicked(string seatId)
    {
        if (seatId != currentSeat)
            return;

        string seatPath = $"Seat/Classroom/{seatId}";

        // ⭐ 清空座位
        dbRef.Child(seatPath).SetValueAsync("").ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"🏃 已離開座位：{seatId}");
                currentSeat = null;
            }
            else
            {
                Debug.LogError($"❌ 離開失敗：{seatId}, {task.Exception}");
            }
        });
    }
}
