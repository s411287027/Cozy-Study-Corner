using UnityEngine;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;

public class SeatAvatar_Room : MonoBehaviour
{
    [System.Serializable]
    public class SeatData
    {
        [Header("基本設定")]
        public string seatName;       // 必須對應資料庫裡存的字串 (例如 "Seat1")
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

    [Header("共用圖片資料庫")]
    public AvatarDatabase avatarDB;

    private DataSnapshot latestSnapshot;
    private bool needsUpdate = false;
    private DatabaseReference firebaseRef;
    private GameObject myWalkingPlayer;
    private string myUid; // 儲存自己的 ID

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
        // 1. 先取得自己的 UID
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        }
        else
        {
            Debug.LogError("尚未登入，無法取得 UID");
            return;
        }

        foreach (var seat in seats) seat.currentUid = "";

        if (avatarDB == null)
        {
            Debug.LogError("❌ 錯誤：請放入 AvatarDatabase！");
            return;
        }

        Debug.Log($"[SeatAvatar_Room] 開始監聽使用者: {myUid} 的讀書狀態...");

        // ⭐ 修改監聽路徑：只監聽「我自己」的 StudyAtHome 狀態
        firebaseRef = FirebaseDatabase.DefaultInstance.GetReference($"users/{myUid}/StudyAtHome");
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
            // 抓取 Body Order
            SitButton.SitPartData bodyData = null;
            if (targetSeat.sitButton != null && targetSeat.sitButton.partsData != null)
            {
                foreach (var part in targetSeat.sitButton.partsData)
                {
                    if (part.partName == "body") { bodyData = part; break; }
                }
            }

            Vector3 basePos = targetSeat.currentAvatarObj.transform.position;
            int baseOrder = 0;
            if (bodyData != null)
            {
                if (bodyData.position != null) basePos = bodyData.position.position;
                baseOrder = bodyData.sortingOrder;
            }

            // 設定所有部位
            SetRenderer(targetSeat.runtimeHair, avatarDB.GetHair(task.hairId), basePos, targetSeat.hairOffset, targetSeat.hairScale, baseOrder + targetSeat.hairOrder);
            SetRenderer(targetSeat.runtimeFace, avatarDB.GetFace(task.faceId), basePos, targetSeat.faceOffset, targetSeat.faceScale, baseOrder + targetSeat.faceOrder);
            SetRenderer(targetSeat.runtimeShirt, avatarDB.GetShirt(task.shirtId), basePos, targetSeat.shirtOffset, targetSeat.shirtScale, baseOrder + targetSeat.shirtOrder);
            SetRenderer(targetSeat.runtimeSleeve, avatarDB.GetSleeve(task.sleeveId), basePos, targetSeat.sleeveOffset, targetSeat.sleeveScale, baseOrder + targetSeat.sleeveOrder);
            SetRenderer(targetSeat.runtimePants, avatarDB.GetPants(task.pantsId), basePos, targetSeat.pantsOffset, targetSeat.pantsScale, baseOrder + targetSeat.pantsOrder);
            SetRenderer(targetSeat.runtimeShoes, avatarDB.GetShoes(task.shoesId), basePos, targetSeat.shoesOffset, targetSeat.shoesScale, baseOrder + targetSeat.shoesOrder);

            targetSeat.currentAvatarObj.SetActive(true);
        }
    }

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

    void LateUpdate()
    {
        if (!needsUpdate || latestSnapshot == null) return;
        ProcessSeatUpdates(latestSnapshot);
        needsUpdate = false;
    }

    // ⭐ 重大修改：這裡不再跑迴圈找 UID，而是看 Snapshot 裡的值
    private void ProcessSeatUpdates(DataSnapshot snapshot)
    {
        string myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        bool amISitting = false; // ⭐ 標記：我是否坐在某個位置上
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

                // ⭐ 尋找對應的 Renderer (請確保 Prefab 裡面有這些名字)
                seat.runtimeHair = FindRenderer(newAvatar.transform, "hair_sit");
                seat.runtimeFace = FindRenderer(newAvatar.transform, "face_sit");
                seat.runtimeShirt = FindRenderer(newAvatar.transform, "shirt_sit");
                seat.runtimePants = FindRenderer(newAvatar.transform, "pants_sit");
                seat.runtimeSleeve = FindRenderer(newAvatar.transform, "sleeve_sit"); // ⭐ 找袖子物件

                // 隱藏初始顏色
                if (seat.runtimeHair != null) seat.runtimeHair.color = Color.clear;
                if (seat.runtimeFace != null) seat.runtimeFace.color = Color.clear;
                if (seat.runtimeShirt != null) seat.runtimeShirt.color = Color.clear;
                if (seat.runtimePants != null) seat.runtimePants.color = Color.clear;
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

    private void HideRenderer(SpriteRenderer sr)
    {
        if (sr != null) sr.color = Color.clear;
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
            int hId = 0, fId = 0, sId = 0, pId = 0, shId = 0;

            if (equipSnapshot.Exists)
            {
                int.TryParse(equipSnapshot.Child("hair").Value?.ToString(), out hId);
                int.TryParse(equipSnapshot.Child("face").Value?.ToString(), out fId);
                int.TryParse(equipSnapshot.Child("shirt").Value?.ToString(), out sId);
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
                    sleeveId = sId,
                    pantsId = pId,
                    shoesId = shId
                });
            }
        });
    }
}