using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;

public class SeatAvatarManager : MonoBehaviour
{
    [System.Serializable]
    public class SeatData
    {
        public string seatName;
        public Transform seatTransform;
        public PlayerSitController sitControllerPrefab;
        public SitButton sitButton;
    }

    public SeatData[] seats;

    private Dictionary<string, PlayerSitController> seatAvatars =
        new Dictionary<string, PlayerSitController>();

    // ⭐ 修正點 1: 儲存 Firebase 的最新快照，而不是直接在回調中處理
    private DataSnapshot latestSnapshot;
    private bool needsUpdate = false; // 旗標，標記是否需要處理更新

    void Start()
    {
        Debug.Log("[SeatAvatarManager] 開始監聽 Firebase 數據...");
        FirebaseDatabase.DefaultInstance
            .GetReference("Seat/Classroom")
            .ValueChanged += OnSeatValueChanged;
    }

    private void OnSeatValueChanged(object sender, ValueChangedEventArgs e)
    {
        // ⭐ 修正點 2: 在回調中只保存數據並設置旗標
        if (e.Snapshot == null)
        {
            Debug.LogWarning("[SeatAvatarManager] Firebase 快照為空，跳過更新。");
            return;
        }

        latestSnapshot = e.Snapshot;
        needsUpdate = true; // 標記我們需要在下一個 LateUpdate 中處理數據
    }

    void LateUpdate()
    {
        if (!needsUpdate || latestSnapshot == null)
        {
            return;
        }

        // ⭐ 修正點 3: 在 LateUpdate 中批量執行所有密集的創建/銷毀操作
        ProcessSeatUpdates(latestSnapshot);
        needsUpdate = false; // 重置旗標
    }

    private void ProcessSeatUpdates(DataSnapshot snapshot)
    {
        foreach (var seat in seats)
        {
            var uid = snapshot.Child(seat.seatName).Value as string;

            // --- 偵錯 Log 1: 確認 Firebase 數據是否讀到 ---
            Debug.Log($"[Seat Check] Seat: {seat.seatName}, UID Read: '{uid ?? "null"}'");
            // --------------------------------------------------

            if (!string.IsNullOrEmpty(uid))
            {
                // -----------------------
                // 座位有人 → 顯示 Avatar
                // -----------------------
                if (seatAvatars.ContainsKey(seat.seatName))
                {
                    Debug.Log($"[Seat Check] Seat {seat.seatName} 已被追蹤，跳過實例化。");
                    continue;
                }

                Debug.Log($"[Seat Check] Seat {seat.seatName} 首次被佔用 ({uid})，開始實例化 Avatar...");

                if (seat.sitControllerPrefab == null)
                {
                    Debug.LogError($"[Seat Check] 錯誤！Seat {seat.seatName} 的 'Sit Controller Prefab' 連結遺失！無法實例化。");
                    continue;
                }

                // 執行實例化
                // ⭐ 修正點 4: 檢查 seatTransform 是否為空，避免 NullReferenceException
                if (seat.seatTransform == null)
                {
                    Debug.LogError($"[Seat Check] 錯誤！Seat {seat.seatName} 的 'Seat Transform' 連結遺失！無法定位 Avatar。");
                    continue;
                }

                var avatar = Instantiate(
                    seat.sitControllerPrefab,
                    seat.seatTransform.position,
                    Quaternion.identity
                );

                if (avatar == null)
                {
                    Debug.LogError($"[Seat Check] 嚴重錯誤！Instantiate 失敗，無法創建 {seat.seatName} 的 Avatar！");
                    continue;
                }

                avatar.name = $"Avatar_{seat.seatName}_{uid}";
                Debug.Log($"[Seat Check] 成功創建 Avatar: {avatar.name}，位置: {seat.seatTransform.position}");


                if (seat.sitButton != null && seat.sitButton.partsData != null)
                {
                    avatar.Sit(seat.sitButton.partsData);
                    Debug.Log($"[Seat Check] Avatar 呼叫 Sit()，使用 {seat.seatName} 的 Parts Data。");
                }
                else
                {
                    Debug.LogError($"[Seat Check] 警告！Seat {seat.seatName} 的 SitButton 或 Parts Data 為空，Avatar 將只顯示預設站姿。");
                }

                seatAvatars[seat.seatName] = avatar;
            }
            else
            {
                // -----------------------
                // 座位沒人 → 移除 Avatar
                // -----------------------
                if (seatAvatars.ContainsKey(seat.seatName))
                {
                    Debug.Log($"[Seat Check] Seat {seat.seatName} 已清空，移除 Avatar。");

                    // ⭐ 注意：Destroy 操作也應在主執行緒進行，LateUpdate 已經滿足這個條件
                    Destroy(seatAvatars[seat.seatName].gameObject);
                    seatAvatars.Remove(seat.seatName);
                }
            }
        }
    }
}