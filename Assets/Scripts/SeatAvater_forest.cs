using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
// using UnityEngine.SceneManagement; // ⭐ 在這個固定路徑版本中不需要這個命名空間

// ⭐ 修正點 1: 類別名稱應與檔案名匹配 (假設檔案名是 SeatAvatar_forest.cs)
public class SeatAvatar_forest : MonoBehaviour
{
    [System.Serializable]
    public class SeatData
    {
        public string seatName;                 // Firebase key (ex: "1-1")
        public Transform seatTransform;         // 座位位置
        public PlayerSitController sitControllerPrefab; // Avatar prefab
        public SitButton sitButton;             // 該座位的坐姿數據來源
    }

    public SeatData[] seats;

    private Dictionary<string, PlayerSitController> seatAvatars =
        new Dictionary<string, PlayerSitController>();

    // 儲存 Firebase 的最新快照，並標記是否需要更新
    private DataSnapshot latestSnapshot;
    private bool needsUpdate = false;

    private DatabaseReference firebaseRef; // ⭐ 修正點 2: 儲存 Firebase 參考，方便取消監聽

    void Start()
    {
        // 確保沒有重複監聽
        if (firebaseRef != null)
        {
            firebaseRef.ValueChanged -= OnSeatValueChanged;
        }

        Debug.Log("[SeatAvatar_forest] 開始監聽 Firebase 數據...");

        // ⭐ 修正點 3: 設置固定的 Firebase 路徑
        firebaseRef = FirebaseDatabase.DefaultInstance.GetReference("Seat/Forest");
        firebaseRef.ValueChanged += OnSeatValueChanged;
    }

    private void OnDestroy()
    {
        // ⭐ 修正點 4: 確保在腳本被銷毀時移除監聽，避免內存洩漏
        if (firebaseRef != null)
        {
            firebaseRef.ValueChanged -= OnSeatValueChanged;
        }
    }


    private void OnSeatValueChanged(object sender, ValueChangedEventArgs e)
    {
        // 在 Firebase 回調中只保存數據並設置旗標
        if (e.Snapshot == null)
        {
            Debug.LogWarning("[SeatAvatar_forest] Firebase 快照為空，跳過更新。");
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

        // 在 LateUpdate 中批量執行所有密集的創建/銷毀操作
        ProcessSeatUpdates(latestSnapshot);
        needsUpdate = false; // 重置旗標
    }

    private void ProcessSeatUpdates(DataSnapshot snapshot)
    {
        foreach (var seat in seats)
        {
            var uid = snapshot.Child(seat.seatName).Value as string;

            Debug.Log($"[Seat Check] Seat: {seat.seatName}, UID Read: '{uid ?? "null"}'");

            if (!string.IsNullOrEmpty(uid))
            {
                // 座位有人 → 顯示 Avatar
                if (seatAvatars.ContainsKey(seat.seatName))
                {
                    Debug.Log($"[Seat Check] Seat {seat.seatName} 已被追蹤，跳過實例化。");
                    continue;
                }

                Debug.Log($"[Seat Check] Seat {seat.seatName} 首次被佔用 ({uid})，開始實例化 Avatar...");

                // 錯誤檢查：Prefab 和 Transform 連結
                if (seat.sitControllerPrefab == null)
                {
                    Debug.LogError($"[Seat Check] 錯誤！Seat {seat.seatName} 的 'Sit Controller Prefab' 連結遺失！");
                    continue;
                }
                if (seat.seatTransform == null)
                {
                    Debug.LogError($"[Seat Check] 錯誤！Seat {seat.seatName} 的 'Seat Transform' 連結遺失！無法定位 Avatar。");
                    continue;
                }

                // 執行實例化
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
                // 座位沒人 → 移除 Avatar
                if (seatAvatars.ContainsKey(seat.seatName))
                {
                    Debug.Log($"[Seat Check] Seat {seat.seatName} 已清空，移除 Avatar。");

                    // 執行銷毀和字典移除
                    Destroy(seatAvatars[seat.seatName].gameObject);
                    seatAvatars.Remove(seat.seatName);
                }
            }
        }
    }
}