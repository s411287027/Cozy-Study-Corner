using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions; // ⭐ 1. 記得加這個
using System.Collections.Generic;

public class SeatManager_Forest : MonoBehaviour
{
    public Transform seatsParent;  // Forest 場景的座位父物件
    private DatabaseReference dbRef;
    private string currentUID;
    public GameObject homeButton;

    private string currentSeat = null;
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

        // 監聽 Forest 資料變化
        FirebaseDatabase.DefaultInstance
            .GetReference("Seat/Forest")
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
                    // ⭐ 2. 修改：讀取名字
                    label.text = "Loading...";
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

    // ⭐ 3. 新增讀取名字函式
    private void UpdateLabelWithUserName(string targetUid, TMP_Text labelToUpdate)
    {
        dbRef.Child("users").Child(targetUid).Child("UserName")
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

        string seatPath = $"Seat/Forest/{seatId}";
        dbRef.Child(seatPath).SetValueAsync(currentUID);
    }

    private void OnLeaveButtonClicked(string seatId)
    {
        if (seatId != currentSeat)
            return;

        if (homeButton != null) homeButton.SetActive(true);

        string seatPath = $"Seat/Forest/{seatId}";
        dbRef.Child(seatPath).SetValueAsync("");
        currentSeat = null;
    }
}