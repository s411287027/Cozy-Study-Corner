using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;

public class SeatManager_Classroom : MonoBehaviour
{
    public Transform seatsParent;
    public GameObject homeButton;   // ⭐ 新增：Home 按鈕

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
                    label.text = $"UID: {uid}";

                    if (uid == currentUID)
                    {
                        currentSeat = seatId;
                        sitBtn.gameObject.SetActive(false);
                        leaveBtn.gameObject.SetActive(true);

                        // ⭐ 玩家已坐下 → 隱藏 HomeButton
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
                string seatId = kv.Key;
                GameObject seat = kv.Value;

                if (seatId != currentSeat)
                {
                    Button sitBtn = seat.transform.Find("SitButton").GetComponent<Button>();
                    sitBtn.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // ⭐ 玩家沒有坐任何位置 → 顯示 HomeButton
            if (homeButton != null) homeButton.SetActive(true);
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

                // ⭐ 玩家按下 Sit → 立即隱藏 HomeButton
                if (homeButton != null) homeButton.SetActive(false);
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

        dbRef.Child(seatPath).SetValueAsync("").ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"🏃 已離開座位：{seatId}");
                currentSeat = null;

                // ⭐ 玩家按下 Leave → 顯示 HomeButton
                if (homeButton != null) homeButton.SetActive(true);
            }
            else
            {
                Debug.LogError($"❌ 離開失敗：{seatId}, {task.Exception}");
            }
        });
    }
}
