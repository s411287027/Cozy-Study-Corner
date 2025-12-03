using UnityEngine;
using Firebase.Database;
using Firebase.Extensions; // ⭐ 1. 務必加上這行

public class ForestManager : MonoBehaviour
{
    public SeatAvatar_forest seatController; // 拉入你的 SeatAvatar 腳本

    void Start()
    {
        CheckAndEnterRoom();
    }

    void CheckAndEnterRoom()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("Seat/Forest/Room1")
            .GetValueAsync()
            .ContinueWithOnMainThread(task => // ⭐ 2. 改用 ContinueWithOnMainThread
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("讀取房間資料失敗");
                    return;
                }

                long userCount = 0;
                if (task.Result.Exists)
                {
                    userCount = task.Result.ChildrenCount; // 計算裡面有幾個人
                }

                Debug.Log($"Room1 目前人數: {userCount}");

                // 假設滿座是 12 人
                if (userCount < 12)
                {
                    // Room1 還有空位，直接連線
                    // 因為用了 ContinueWithOnMainThread，這裡可以直接呼叫 Unity API
                    seatController.ConnectToRoom("Room1");
                }
                else
                {
                    // Room1 滿了，去 Room2
                    Debug.Log("Room1 客滿，自動轉入 Room2");
                    seatController.ConnectToRoom("Room2");
                }
            });
    }
}