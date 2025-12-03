using UnityEngine;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;
// 建議加入 Firebase.Extensions，以防 LoadUserEquip 使用 ContinueWithOnMainThread
// using Firebase.Extensions; 

public class SeatAvatar_Cafe : MonoBehaviour
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
        public Vector3 pantsOffset = new Vector3(0, 0, 0);
        public Vector3 shoesOffset = new Vector3(0, 0, 0);

        [Header("個別縮放微調 (Scale)")]
        public Vector3 hairScale = new Vector3(2, 2, 1);
        public Vector3 faceScale = new Vector3(2, 2, 1);
        public Vector3 shirtScale = new Vector3(2, 2, 1);
        public Vector3 sleeveScale = new Vector3(2, 2, 1);
        public Vector3 pantsScale = new Vector3(2, 2, 1);
        public Vector3 shoesScale = new Vector3(2, 2, 1);

        [Header("圖層順序微調 (Order Offset)")]
        public int hairOrder = 2;
        public int faceOrder = 1;
        public int shirtOrder = 1;
        public int sleeveOrder = 3;
        public int pantsOrder = 1;
        public int shoesOrder = 1;

        // Runtime 變數
        [HideInInspector] public GameObject currentAvatarObj;
        [HideInInspector] public SpriteRenderer runtimeHair;
        [HideInInspector] public SpriteRenderer runtimeFace;
        [HideInInspector] public SpriteRenderer runtimeShirt;
        [HideInInspector] public SpriteRenderer runtimeSleeve;
        [HideInInspector] public SpriteRenderer runtimePants;
        [HideInInspector] public SpriteRenderer runtimeShoes;
        [HideInInspector] public PlayerSitController runtimeController;
        [HideInInspector] public string currentUid;
    }

    public SeatData[] seats;

    [Header("必須設定：小人預製件")]
    public GameObject avatarPrefab;

    [Header("共用圖片資料庫 (請拉入設定檔)")]
    public AvatarDatabase avatarDB;

    // ⭐ 房間變數：控制目前監聽哪個房間
    [Header("房間設定")]
    public string currentRoomID = "Room1";

    // ⭐ 延遲清除追蹤：追蹤目前成功顯示的房間ID，避免閃爍
    private string lastDisplayedRoomID = "NOT_INITIALIZED";

    private DataSnapshot latestSnapshot;
    private bool needsUpdate = false;
    private DatabaseReference firebaseRef; // 監聽特定房間路徑

    // 用來暫存走路的角色
    private GameObject myWalkingPlayer;

    private struct AppearanceTask
    {
        public int seatIndex;
        public int hairId;
        public int faceId;
        public int shirtId;
        public int sleeveId;
        public int pantsId;
        public int shoesId;
    }
    private Queue<AppearanceTask> pendingAppearanceUpdates = new Queue<AppearanceTask>();
    private object queueLock = new object();

    void Start()
    {
        foreach (var seat in seats) seat.currentUid = "";

        if (avatarDB == null)
        {
            Debug.LogError("❌ 錯誤：請在 SeatAvatar_Cafe 元件中放入 AvatarDatabase 設定檔！");
            return;
        }

        // ⭐ 啟動時，連線到預設房間
        ConnectToRoom(currentRoomID);
    }

    // ⭐ 核心函式：切換房間的邏輯
    public void ConnectToRoom(string roomID)
    {
        // 1. 如果原本有連線，先取消監聽
        if (firebaseRef != null)
        {
            firebaseRef.ValueChanged -= OnSeatValueChanged;
        }

        // 2. 清空【換裝排程隊列】(舊排程對新房間無效)
        lock (queueLock)
        {
            pendingAppearanceUpdates.Clear();
        }

        // ⭐ 關鍵：不立刻清除畫面小人，實作延遲清除

        currentRoomID = roomID;
        Debug.Log($"[SeatAvatar_Cafe] 切換至房間：{currentRoomID}");

        // 3. 設定新的監聽路徑：Seat/Coffee/RoomX
        firebaseRef = FirebaseDatabase.DefaultInstance.GetReference($"Seat/Coffee/{currentRoomID}");
        firebaseRef.ValueChanged += OnSeatValueChanged;
    }

    // ⭐ 輔助函式：只清除畫面上視覺元素和引用
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
            seat.runtimePants = null;
            seat.runtimeShoes = null;
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
        if (targetSeat.currentAvatarObj == null) return;

        if (targetSeat.currentAvatarObj != null)
        {
            // 1. 抓取身體 (body) 的 Sorting Order
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

            Vector3 basePos = targetSeat.currentAvatarObj.transform.position;
            int baseOrder = 0;

            if (bodyData != null)
            {
                if (bodyData.position != null) basePos = bodyData.position.position;
                baseOrder = bodyData.sortingOrder;
            }

            // 輔助函式：快速設定 Renderer
            SetRenderer(targetSeat.runtimeHair, avatarDB.GetHair(task.hairId), basePos, targetSeat.hairOffset, targetSeat.hairScale, baseOrder + targetSeat.hairOrder);
            SetRenderer(targetSeat.runtimeFace, avatarDB.GetFace(task.faceId), basePos, targetSeat.faceOffset, targetSeat.faceScale, baseOrder + targetSeat.faceOrder);
            SetRenderer(targetSeat.runtimeShirt, avatarDB.GetShirt(task.shirtId), basePos, targetSeat.shirtOffset, targetSeat.shirtScale, baseOrder + targetSeat.shirtOrder);
            SetRenderer(targetSeat.runtimeSleeve, avatarDB.GetSleeve(task.sleeveId), basePos, targetSeat.sleeveOffset, targetSeat.sleeveScale, baseOrder + targetSeat.sleeveOrder);

            // ⭐ 設定褲子和鞋子
            SetRenderer(targetSeat.runtimePants, avatarDB.GetPants(task.pantsId), basePos, targetSeat.pantsOffset, targetSeat.pantsScale, baseOrder + targetSeat.pantsOrder);
            SetRenderer(targetSeat.runtimeShoes, avatarDB.GetShoes(task.shoesId), basePos, targetSeat.shoesOffset, targetSeat.shoesScale, baseOrder + targetSeat.shoesOrder);

            targetSeat.currentAvatarObj.SetActive(true);
        }
    }

    // 簡化代碼用的輔助函式
    private void SetRenderer(SpriteRenderer sr, Sprite sprite, Vector3 basePos, Vector3 offset, Vector3 scale, int order)
    {
        if (sr != null)
        {
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.transform.position = basePos + offset;
            sr.transform.localScale = scale;
            sr.sortingOrder = order;
            sr.color = Color.white;
        }
    }

    // 隱藏顏色
    private void HideRenderer(SpriteRenderer sr)
    {
        if (sr != null) sr.color = Color.clear;
    }


    void LateUpdate()
    {
        if (!needsUpdate || latestSnapshot == null) return;
        ProcessSeatUpdates(latestSnapshot);
        needsUpdate = false;
    }

    private void ProcessSeatUpdates(DataSnapshot snapshot)
    {
        // ⭐ 修正延遲清除邏輯
        if (currentRoomID != lastDisplayedRoomID)
        {
            // 只有當上一個顯示的 ID 不是初始值時才執行清除，避免 Startup 閃爍
            if (lastDisplayedRoomID != "NOT_INITIALIZED")
            {
                Debug.Log($"[SeatAvatar_Cafe] 收到 {currentRoomID} 資料，清除舊房間 ({lastDisplayedRoomID}) 顯示...");
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

                // ⭐ 綁定所有部位 (包含褲子鞋子)
                seat.runtimeHair = FindRenderer(newAvatar.transform, "hair_sit");
                seat.runtimeFace = FindRenderer(newAvatar.transform, "face_sit");
                seat.runtimeShirt = FindRenderer(newAvatar.transform, "shirt_sit");
                seat.runtimeSleeve = FindRenderer(newAvatar.transform, "sleeve_sit");
                seat.runtimePants = FindRenderer(newAvatar.transform, "pants_sit");
                seat.runtimeShoes = FindRenderer(newAvatar.transform, "shoes_sit");

                // 隱藏初始顏色
                HideRenderer(seat.runtimeHair);
                HideRenderer(seat.runtimeFace);
                HideRenderer(seat.runtimeShirt);
                HideRenderer(seat.runtimeSleeve);
                HideRenderer(seat.runtimePants);
                HideRenderer(seat.runtimeShoes);

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

        // ⭐ 處理隱藏/顯示走路角色
        HandleMyWalkingPlayerVisibility(amISitting);
    }

    // ⭐ 控制走路角色的顯示邏輯
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
            int hId = 0, fId = 0, sId = 0, pId = 0, shId = 0;

            if (equipSnapshot.Exists)
            {
                int.TryParse(equipSnapshot.Child("hair").Value?.ToString(), out hId);
                int.TryParse(equipSnapshot.Child("face").Value?.ToString(), out fId);
                int.TryParse(equipSnapshot.Child("shirt").Value?.ToString(), out sId);

                // ⭐ 讀取褲子和鞋子
                int.TryParse(equipSnapshot.Child("pants").Value?.ToString(), out pId);
                int.TryParse(equipSnapshot.Child("shoes").Value?.ToString(), out shId);
            }

            lock (queueLock)
            {
                pendingAppearanceUpdates.Enqueue(new AppearanceTask
                {
                    seatIndex = seatIndex,
                    hairId = hId,
                    faceId = fId,
                    shirtId = sId,
                    sleeveId = sId, // 袖子 ID 同衣服
                    pantsId = pId,
                    shoesId = shId
                });
            }
        });
    }
}