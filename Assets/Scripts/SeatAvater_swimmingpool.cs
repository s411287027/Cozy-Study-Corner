using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using Firebase.Auth;
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
        public Vector3 sleeveOffset = new Vector3(0, 0.12f, 0); // ⭐ 新增：袖子位置

        [Header("個別縮放微調 (Scale)")]
        public Vector3 hairScale = new Vector3(2, 2, 1);
        public Vector3 faceScale = new Vector3(2, 2, 1);
        public Vector3 shirtScale = new Vector3(2, 2, 1);
        public Vector3 sleeveScale = new Vector3(2, 2, 1);      // ⭐ 新增：袖子縮放

        // Runtime 變數
        [HideInInspector] public GameObject currentAvatarObj;
        [HideInInspector] public SpriteRenderer runtimeHair;
        [HideInInspector] public SpriteRenderer runtimeFace;
        [HideInInspector] public SpriteRenderer runtimeShirt;
        [HideInInspector] public SpriteRenderer runtimeSleeve;  // ⭐ 新增：袖子 Renderer
        [HideInInspector] public PlayerSitController runtimeController;
        [HideInInspector] public string currentUid;
    }

    public SeatData[] seats;

    [Header("必須設定：小人預製件")]
    public GameObject avatarPrefab;

    [Header("共用圖片資料庫 (請拉入設定檔)")]
    public AvatarDatabase avatarDB;

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
        public int sleeveId; // ⭐ 新增：袖子 ID
    }
    private Queue<AppearanceTask> pendingAppearanceUpdates = new Queue<AppearanceTask>();
    private object queueLock = new object();

    void Start()
    {
        foreach (var seat in seats) seat.currentUid = "";

        if (avatarDB == null)
        {
            return;
        }

        Debug.Log("[SeatAvatar] 開始監聽...");
        firebaseRef = FirebaseDatabase.DefaultInstance.GetReference("Seat/Swimmingpool");
        firebaseRef.ValueChanged += OnSeatValueChanged;
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
                targetSeat.runtimeShirt.sortingOrder = baseOrder + 1;
                targetSeat.runtimeShirt.color = Color.white;
            }

            // 6. 設定袖子 (⭐ 新增邏輯)
            if (targetSeat.runtimeSleeve != null)
            {
                targetSeat.runtimeSleeve.gameObject.SetActive(true);
                targetSeat.runtimeSleeve.sprite = avatarDB.GetSleeve(task.sleeveId); // 從 DB 拿袖子

                targetSeat.runtimeSleeve.transform.position = basePos + targetSeat.sleeveOffset;
                targetSeat.runtimeSleeve.transform.localScale = targetSeat.sleeveScale;
                // 袖子通常跟衣服同一層，或是比衣服高一層 (取決於你的美術)，這裡設為 +1 或 +2 皆可
                targetSeat.runtimeSleeve.sortingOrder = baseOrder + 3;
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
        string myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        bool amISitting = false;
        for (int i = 0; i < seats.Length; i++)
        {
            var seat = seats[i];
            var uid = snapshot.Child(seat.seatName).Value as string;

            if (!string.IsNullOrEmpty(uid))
            {
                if (seat.currentUid == uid && seat.currentAvatarObj != null) continue;
                if (uid == myUid) amISitting = true;
                seat.currentUid = uid;
                if (seat.currentAvatarObj != null) Destroy(seat.currentAvatarObj);

                GameObject newAvatar = Instantiate(avatarPrefab, seat.seatTransform.position, Quaternion.identity);
                seat.currentAvatarObj = newAvatar;
                newAvatar.SetActive(false);

                seat.runtimeController = newAvatar.GetComponent<PlayerSitController>();

                // ⭐ 尋找對應的 Renderer (請確保 Prefab 裡面有這些名字)
                seat.runtimeHair = FindRenderer(newAvatar.transform, "hair_sit");
                seat.runtimeFace = FindRenderer(newAvatar.transform, "face_sit");
                seat.runtimeShirt = FindRenderer(newAvatar.transform, "shirt_sit");
                seat.runtimeSleeve = FindRenderer(newAvatar.transform, "sleeve_sit"); // ⭐ 找袖子物件

                // 隱藏初始顏色
                if (seat.runtimeHair != null) seat.runtimeHair.color = Color.clear;
                if (seat.runtimeFace != null) seat.runtimeFace.color = Color.clear;
                if (seat.runtimeShirt != null) seat.runtimeShirt.color = Color.clear;
                if (seat.runtimeSleeve != null) seat.runtimeSleeve.color = Color.clear; // ⭐

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
                }
            }
        }
        HandleMyWalkingPlayerVisibility(amISitting);
    }

    private void HandleMyWalkingPlayerVisibility(bool isSitting)
    {
        if (myWalkingPlayer == null)
        {
            // 根據你的截圖，角色名是 player(Clone)
            myWalkingPlayer = GameObject.Find("player(Clone)");
        }

        if (myWalkingPlayer != null)
        {
            // 如果坐著，就隱藏走路角色；如果沒坐，就顯示
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

                // 1. 抓取衣服 ID
                int.TryParse(equipSnapshot.Child("shirt").Value?.ToString(), out sId);

                // 2. ⭐ 修改這裡：袖子 ID 直接使用衣服的 ID (因為它們是同一套)
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
                    sleeveId = slId // 這裡就會傳入跟衣服一樣的 ID 去 Database 找圖片
                });
            }
        });
    }
}