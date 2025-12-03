using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using Firebase.Auth;
// 建議加入
// using Firebase.Extensions; 

public class SeatAvatar_swimmingpool : MonoBehaviour
{
    [System.Serializable]
    public class SeatData
    {
        [Header("基本設定")]
        public string seatName;
        public Transform seatTransform;
        public SitButton sitButton;

        [Header("個別位置微調 (Offset)")]
        public Vector3 hairOffset = new Vector3(0, 2.75f, 0);
        public Vector3 faceOffset = new Vector3(0, 2.75f, 0);
        public Vector3 shirtOffset = new Vector3(0, 0.12f, 0);
        public Vector3 sleeveOffset = new Vector3(0, 0.12f, 0);

        [Header("個別縮放微調 (Scale)")]
        public Vector3 hairScale = new Vector3(2, 2, 1);
        public Vector3 faceScale = new Vector3(2, 2, 1);
        public Vector3 shirtScale = new Vector3(2, 2, 1);
        public Vector3 sleeveScale = new Vector3(2, 2, 1);

        // Runtime 變數
        [HideInInspector] public GameObject currentAvatarObj;
        [HideInInspector] public SpriteRenderer runtimeHair;
        [HideInInspector] public SpriteRenderer runtimeFace;
        [HideInInspector] public SpriteRenderer runtimeShirt;
        [HideInInspector] public SpriteRenderer runtimeSleeve;
        [HideInInspector] public PlayerSitController runtimeController;
        [HideInInspector] public string currentUid;
    }

    public SeatData[] seats;

    [Header("必須設定：小人預製件")]
    public GameObject avatarPrefab;

    [Header("共用圖片資料庫 (請拉入設定檔)")]
    public AvatarDatabase avatarDB;

    // ⭐ 修正 1: 新增房間變數
    [Header("房間設定")]
    public string currentRoomID = "Room1";

    // ⭐ 修正 2: 延遲清除追蹤
    private string lastDisplayedRoomID = "NOT_INITIALIZED";

    private DataSnapshot latestSnapshot;
    private bool needsUpdate = false;
    private DatabaseReference firebaseRef;
    private GameObject myWalkingPlayer;
    private struct AppearanceTask
    {
        public int seatIndex;
        public int hairId;
        public int faceId;
        public int shirtId;
        public int sleeveId;
    }
    private Queue<AppearanceTask> pendingAppearanceUpdates = new Queue<AppearanceTask>();
    private object queueLock = new object();

    void Start()
    {
        foreach (var seat in seats) seat.currentUid = "";

        if (avatarDB == null)
        {
            Debug.LogError("❌ 錯誤：請在 SeatAvatar_swimmingpool 元件中放入 AvatarDatabase 設定檔！");
            return;
        }

        // ⭐ 修正 3: 啟動時連線到預設房間
        ConnectToRoom(currentRoomID);
    }

    // ⭐ 修正 4: 核心函式：切換房間的邏輯 (處理隊列清除)
    public void ConnectToRoom(string roomID)
    {
        if (firebaseRef != null)
        {
            firebaseRef.ValueChanged -= OnSeatValueChanged;
        }

        lock (queueLock)
        {
            pendingAppearanceUpdates.Clear();
        }

        currentRoomID = roomID;
        Debug.Log($"[SeatAvatar_Swimmingpool] 準備切換至房間：{currentRoomID}");

        // ⭐ 設定新的監聽路徑：Seat/Swimmingpool/RoomX
        firebaseRef = FirebaseDatabase.DefaultInstance.GetReference($"Seat/Swimmingpool/{currentRoomID}");
        firebaseRef.ValueChanged += OnSeatValueChanged;
    }

    // ⭐ 修正 5: 只清除畫面上視覺元素和引用 (供延遲清除使用)
    private void ClearVisualAvatars()
    {
        foreach (var seat in seats)
        {
            seat.currentUid = "";
            if (seat.currentAvatarObj != null)
            {
                Destroy(seat.currentAvatarObj);
                seat.currentAvatarObj = null;
            }
            // 清除所有執行期引用
            seat.runtimeHair = null;
            seat.runtimeFace = null;
            seat.runtimeShirt = null;
            seat.runtimeSleeve = null;
            seat.runtimeController = null;
        }
        latestSnapshot = null;
    }

    private void OnDestroy()
    {
        if (firebaseRef != null) firebaseRef.ValueChanged -= OnSeatValueChanged;
    }

    private void OnSeatValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.Snapshot == null) return;
        latestSnapshot = e.Snapshot;
        needsUpdate = true;
    }

    void Update()
    {
        lock (queueLock)
        {
            while (pendingAppearanceUpdates.Count > 0)
            {
                ApplyAppearance(pendingAppearanceUpdates.Dequeue());
            }
        }
    }

    private void ApplyAppearance(AppearanceTask task)
    {
        if (task.seatIndex < 0 || task.seatIndex >= seats.Length) return;

        SeatData targetSeat = seats[task.seatIndex];

        if (targetSeat.currentAvatarObj == null) return; // 安全檢查

        if (targetSeat.currentAvatarObj != null)
        {
            // 1. 抓取身體 (body) 的設定資料
            SitButton.SitPartData bodyData = null;
            if (targetSeat.sitButton != null && targetSeat.sitButton.partsData != null)
            {
                foreach (var part in targetSeat.sitButton.partsData)
                {
                    if (part.partName == "body")
                    {
                        bodyData = part;
                        break;
                    }
                }
            }

            // 2. 準備基準位置
            Vector3 basePos = targetSeat.currentAvatarObj.transform.position;
            int baseOrder = 0;

            if (bodyData != null)
            {
                if (bodyData.position != null) basePos = bodyData.position.position;
                baseOrder = bodyData.sortingOrder;
            }

            // 3. 設定頭髮
            if (targetSeat.runtimeHair != null)
            {
                targetSeat.runtimeHair.gameObject.SetActive(true);
                targetSeat.runtimeHair.sprite = avatarDB.GetHair(task.hairId);
                targetSeat.runtimeHair.transform.position = basePos + targetSeat.hairOffset;
                targetSeat.runtimeHair.transform.localScale = targetSeat.hairScale;
                targetSeat.runtimeHair.sortingOrder = baseOrder + 2;
                targetSeat.runtimeHair.color = Color.white;
            }

            // 4. 設定臉
            if (targetSeat.runtimeFace != null)
            {
                targetSeat.runtimeFace.gameObject.SetActive(true);
                targetSeat.runtimeFace.sprite = avatarDB.GetFace(task.faceId);
                targetSeat.runtimeFace.transform.position = basePos + targetSeat.faceOffset;
                targetSeat.runtimeFace.transform.localScale = targetSeat.faceScale;
                targetSeat.runtimeFace.sortingOrder = baseOrder + 1;
                targetSeat.runtimeFace.color = Color.white;
            }

            // 5. 設定衣服
            if (targetSeat.runtimeShirt != null)
            {
                targetSeat.runtimeShirt.gameObject.SetActive(true);
                targetSeat.runtimeShirt.sprite = avatarDB.GetShirt(task.shirtId);
                targetSeat.runtimeShirt.transform.position = basePos + targetSeat.shirtOffset;
                targetSeat.runtimeShirt.transform.localScale = targetSeat.shirtScale;
                targetSeat.runtimeShirt.sortingOrder = baseOrder + 3;
                targetSeat.runtimeShirt.color = Color.white;
            }

            // 6. 設定袖子
            if (targetSeat.runtimeSleeve != null)
            {
                targetSeat.runtimeSleeve.gameObject.SetActive(true);
                targetSeat.runtimeSleeve.sprite = avatarDB.GetSleeve(task.sleeveId); // 從 DB 拿袖子

                targetSeat.runtimeSleeve.transform.position = basePos + targetSeat.sleeveOffset;
                targetSeat.runtimeSleeve.transform.localScale = targetSeat.sleeveScale;
                targetSeat.runtimeSleeve.sortingOrder = baseOrder + 11;
                targetSeat.runtimeSleeve.color = Color.white;
            }

            targetSeat.currentAvatarObj.SetActive(true);
        }
    }

    void LateUpdate()
    {
        if (!needsUpdate || latestSnapshot == null) return;
        ProcessSeatUpdates(latestSnapshot);
        needsUpdate = false;
    }

    private void ProcessSeatUpdates(DataSnapshot snapshot)
    {
        // ⭐ 修正 6: 延遲清除邏輯
        if (currentRoomID != lastDisplayedRoomID)
        {
            // 只有當上一個顯示的 ID 不是初始值時才執行清除
            if (lastDisplayedRoomID != "NOT_INITIALIZED")
            {
                Debug.Log($"[SeatAvatar_Swimmingpool] 收到 {currentRoomID} 資料，清除舊房間 ({lastDisplayedRoomID}) 顯示...");
                ClearVisualAvatars(); // 清除舊房間的小人
            }
            lastDisplayedRoomID = currentRoomID;
        }

        string myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        bool amISitting = false;
        for (int i = 0; i < seats.Length; i++)
        {
            var seat = seats[i];
            var uid = snapshot.Child(seat.seatName).Value as string;

            if (!string.IsNullOrEmpty(uid))
            {
                if (uid == myUid) amISitting = true;
                if (seat.currentUid == uid && seat.currentAvatarObj != null) continue;

                seat.currentUid = uid;
                if (seat.currentAvatarObj != null) Destroy(seat.currentAvatarObj);

                GameObject newAvatar = Instantiate(avatarPrefab, seat.seatTransform.position, Quaternion.identity);
                seat.currentAvatarObj = newAvatar;
                newAvatar.SetActive(false);

                seat.runtimeController = newAvatar.GetComponent<PlayerSitController>();

                // ⭐ 尋找對應的 Renderer
                seat.runtimeHair = FindRenderer(newAvatar.transform, "hair_sit");
                seat.runtimeFace = FindRenderer(newAvatar.transform, "face_sit");
                seat.runtimeShirt = FindRenderer(newAvatar.transform, "shirt_sit");
                seat.runtimeSleeve = FindRenderer(newAvatar.transform, "sleeve_sit");

                // 隱藏初始顏色
                if (seat.runtimeHair != null) seat.runtimeHair.color = Color.clear;
                if (seat.runtimeFace != null) seat.runtimeFace.color = Color.clear;
                if (seat.runtimeShirt != null) seat.runtimeShirt.color = Color.clear;
                if (seat.runtimeSleeve != null) seat.runtimeSleeve.color = Color.clear;

                if (seat.runtimeController != null && seat.sitButton != null)
                {
                    seat.runtimeController.Sit(seat.sitButton.partsData);
                }

                LoadUserEquip(uid, i);
            }
            else
            {
                if (seat.currentAvatarObj != null)
                {
                    Destroy(seat.currentAvatarObj);
                    seat.currentAvatarObj = null;
                    seat.currentUid = "";
                    seat.runtimeController = null;
                }
            }
        }
        HandleMyWalkingPlayerVisibility(amISitting);
    }

    private void HandleMyWalkingPlayerVisibility(bool isSitting)
    {
        if (myWalkingPlayer == null)
        {
            myWalkingPlayer = GameObject.Find("player(Clone)");
        }

        if (myWalkingPlayer != null)
        {
            if (myWalkingPlayer.activeSelf == isSitting)
            {
                myWalkingPlayer.SetActive(!isSitting);
            }
        }
    }

    private SpriteRenderer FindRenderer(Transform root, string exactName)
    {
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            if (r.gameObject.name.Equals(exactName) || r.gameObject.name.Contains(exactName)) return r;
        }
        return null;
    }

    private void LoadUserEquip(string uid, int seatIndex)
    {
        FirebaseDatabase.DefaultInstance
            .GetReference($"users/{uid}/currentEquip")
            .GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled) return;

            DataSnapshot equipSnapshot = task.Result;
            int hId = 0, fId = 0, sId = 0, slId = 0;

            if (equipSnapshot.Exists)
            {
                int.TryParse(equipSnapshot.Child("hair").Value?.ToString(), out hId);
                int.TryParse(equipSnapshot.Child("face").Value?.ToString(), out fId);

                int.TryParse(equipSnapshot.Child("shirt").Value?.ToString(), out sId);

                slId = sId;
            }

            lock (queueLock)
            {
                pendingAppearanceUpdates.Enqueue(new AppearanceTask
                {
                    seatIndex = seatIndex,
                    hairId = hId,
                    faceId = fId,
                    shirtId = sId,
                    sleeveId = slId
                });
            }
        });
    }
}