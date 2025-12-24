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

        // 監聽 Coffee 資料變化
        FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUID}/StudyAtHome/")
            .ValueChanged += OnSeatDataChanged;
    }

    // ⭐ 新增：更新使用者狀態的輔助函式
    private void UpdateUserStatus(string newStatus)
    {
        if (string.IsNullOrEmpty(currentUID)) return;

        dbRef.Child("users").Child(currentUID).Child("Status")
            .SetValueAsync(newStatus).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ 更新狀態為 {newStatus} 失敗: {task.Exception}");
                }
                else
                {
                    Debug.Log($"✅ 用戶狀態已更新為: {newStatus}");
                }
            });
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
        UpdateUserStatus("Studying");
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
        UpdateUserStatus("Online");
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
