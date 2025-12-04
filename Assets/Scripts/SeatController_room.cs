using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;
using Firebase.Extensions;

public class SeatManager_Room : MonoBehaviour
{
    public Transform seatsParent;  // Coffee 場景的座位父物件
    private DatabaseReference dbRef;
    private string currentUID;

    public GameObject MapButton;
    public GameObject ItemButton;
    public GameObject FriendButton;
    public GameObject ReservationButton;
    public GameObject MessageButton;
    public GameObject LogOutButton;
    public GameObject StartTimeInputFil;
    public GameObject EndTimeInputFil;
    public GameObject MessInputFil;

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

        // 監聽 Coffee 資料變化
        FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUID}/StudyAtHome/")
            .ValueChanged += OnSeatDataChanged;
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
                    label.text = "Loading...";
                    UpdateLabelWithUserName(uid, label);

                    if (uid == currentUID)
                    {
                        currentSeat = seatId;
                        sitBtn.gameObject.SetActive(false);
                        leaveBtn.gameObject.SetActive(true);
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
    }

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

        string seatPath = $"users/{currentUID}/StudyAtHome/{seatId}";
        dbRef.Child(seatPath).SetValueAsync(currentUID);
        MapButton.SetActive(false);
        ItemButton.SetActive(false);
        FriendButton.SetActive(false);
        ReservationButton.SetActive(false);
        MessageButton.SetActive(false);
        LogOutButton.SetActive(false);
        StartTimeInputFil.SetActive(false);
        EndTimeInputFil.SetActive(false);
        MessInputFil.SetActive(false);
    }

    private void OnLeaveButtonClicked(string seatId)
    {
        if (seatId != currentSeat)
            return;

        string seatPath = $"users/{currentUID}/StudyAtHome/{seatId}";
        dbRef.Child(seatPath).SetValueAsync("");
        currentSeat = null;
        MapButton.SetActive(true);
        ItemButton.SetActive(true);
        FriendButton.SetActive(true);
        ReservationButton.SetActive(true);
        MessageButton.SetActive(true);
        LogOutButton.SetActive(true);
        StartTimeInputFil.SetActive(true);
        EndTimeInputFil.SetActive(true);
        MessInputFil.SetActive(true);
    }
}
