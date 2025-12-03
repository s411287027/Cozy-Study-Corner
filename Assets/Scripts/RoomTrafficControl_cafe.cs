using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

public class RoomTrafficControl_cafe : MonoBehaviour
{
    public SeatAvatar_Cafe avatarController; // 負責顯示
    public SeatManager_Coffee interactionController; // 負責按鈕與寫入

    // 定義最大人數
    private const int MAX_SEATS = 8;

    void Start()
    {
        // 遊戲開始時，先檢查 Room1
        CheckRoomStatusAndConnect("Room1");
    }

    public void CheckRoomStatusAndConnect(string roomId)
    {
        Debug.Log($"正在檢查 {roomId} 的人數...");

        FirebaseDatabase.DefaultInstance
            .GetReference($"Seat/Coffee/{roomId}")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("讀取房間資料失敗，預設進入 Room1");
                    ConnectTo(roomId); // 失敗時的保底機制
                    return;
                }

                DataSnapshot snapshot = task.Result;
                int realUserCount = 0;

                // ⭐ 關鍵修正：遍歷所有子節點，檢查 Value 是否真的有人
                if (snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        string uid = child.Value.ToString();
                        // 只有當 UID 不為空且不是 null 時，才算是一個人
                        if (!string.IsNullOrEmpty(uid) && uid != "null")
                        {
                            realUserCount++;
                        }
                    }
                }

                Debug.Log($"{roomId} 目前實際人數: {realUserCount}/{MAX_SEATS}");

                if (realUserCount < MAX_SEATS)
                {
                    // 還有位置，進入此房間
                    ConnectTo(roomId);
                }
                else
                {
                    // 滿了，嘗試檢查 Room2 (這裡簡單實作，若 Room2 也滿可再擴充)
                    if (roomId == "Room1")
                    {
                        Debug.Log("Room1 已滿，轉導至 Room2");
                        CheckRoomStatusAndConnect("Room2");
                    }
                    else
                    {
                        Debug.Log("所有房間都滿了！(或你可以擴充 Room3)");
                        // 這裡可以選擇讓玩家進入唯讀模式或 Room2
                        ConnectTo("Room2");
                    }
                }
            });
    }

    // 同步讓兩個控制器都切換到同一個房間
    private void ConnectTo(string targetRoom)
    {
        Debug.Log($"✅ 決定進入：{targetRoom}");
        avatarController.ConnectToRoom(targetRoom);      // 更新顯示 (看到誰)
        interactionController.ConnectToRoom(targetRoom); // 更新互動 (坐在哪)
    }
}