using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;

public class SeatManager : MonoBehaviour
{
    public Transform seatsParent;  // 所有座位的父物件
    private DatabaseReference dbRef;
    private string currentUID;

    private Dictionary<string, GameObject> seatObjects = new Dictionary<string, GameObject>();

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        currentUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        // 收集所有座位
        foreach (Transform seat in seatsParent)
        {
            string seatId = seat.name.Replace("Seat_", "");
            seatObjects[seatId] = seat.gameObject;

            // 找到按鈕並綁定事件
            Button sitBtn = seat.Find("SitButton").GetComponent<Button>();
            sitBtn.onClick.AddListener(() => OnSitButtonClicked(seatId));
        }

        // 開始監聽資料變化
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

        foreach (var seatData in args.Snapshot.Children)
        {
            string seatId = seatData.Key;
            string uid = seatData.Value?.ToString();

            if (seatObjects.TryGetValue(seatId, out GameObject seat))
            {
                Button sitBtn = seat.transform.Find("SitButton").GetComponent<Button>();
                TMP_Text label = seat.transform.Find("Label").GetComponent<TMP_Text>();

                if (string.IsNullOrEmpty(uid) || uid == "null")
                {
                    label.text = "空位";
                    sitBtn.gameObject.SetActive(true);
                }
                else
                {
                    label.text = $"UID: {uid}";
                    sitBtn.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnSitButtonClicked(string seatId)
    {
        string seatPath = $"Seat/Classroom/{seatId}";

        // 寫入自己的 UID
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
}
