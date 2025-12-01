using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;

public class SeatAvatar_classroom : MonoBehaviour
{
    [System.Serializable]
    public class SeatData
    {
        [Header("基本設定")]
        public string seatName;
        public Transform seatTransform;
        public SitButton sitButton;

        // ⭐ 修改 1：將 Offset 拆開，並新增 Scale 設定
        [Header("個別位置微調 (Offset)")]
        public Vector3 hairOffset = new Vector3(0, 2.75f, 0);
        public Vector3 faceOffset = new Vector3(0, 2.75f, 0); // 獨立的臉部偏移
        public Vector3 shirtOffset = new Vector3(0, 0.12f, 0);

        [Header("個別縮放微調 (Scale)")]
        public Vector3 hairScale = new Vector3(2, 2, 1);      // 獨立的頭髮縮放
        public Vector3 faceScale = new Vector3(2, 2, 1);      // 獨立的臉部縮放
        public Vector3 shirtScale = new Vector3(2, 2, 1);     // 獨立的衣服縮放

        // Runtime 變數 (保持原樣)
        [HideInInspector] public GameObject currentAvatarObj;
        [HideInInspector] public SpriteRenderer runtimeHair;
        [HideInInspector] public SpriteRenderer runtimeFace;
        [HideInInspector] public SpriteRenderer runtimeShirt;
        [HideInInspector] public PlayerSitController runtimeController;
        [HideInInspector] public string currentUid;
    }

    public SeatData[] seats;

    [Header("必須設定：小人預製件")]
    public GameObject avatarPrefab;

    [Header("Avatar Resources (圖片庫)")]
    public Sprite[] hairSprites;
    public Sprite[] faceSprites;
    public Sprite[] shirtSprites;

    private DataSnapshot latestSnapshot;
    private bool needsUpdate = false;
    private DatabaseReference firebaseRef;

    private struct AppearanceTask
    {
        public int seatIndex;
        public int hairId;
        public int faceId;
        public int shirtId;
    }
    private Queue<AppearanceTask> pendingAppearanceUpdates = new Queue<AppearanceTask>();
    private object queueLock = new object();

    void Start()
    {
        foreach (var seat in seats) seat.currentUid = "";
        Debug.Log("[SeatAvatar] 開始監聽...");
        firebaseRef = FirebaseDatabase.DefaultInstance.GetReference("Seat/Classroom");
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

    // ================== ⭐ 修正後的 ApplyAppearance ==================
    private void ApplyAppearance(AppearanceTask task)
    {
        if (task.seatIndex < 0 || task.seatIndex >= seats.Length) return;

        SeatData targetSeat = seats[task.seatIndex];

        if (targetSeat.currentAvatarObj != null)
        {
            // 1. 抓取身體 (body) 的設定資料 (主要是為了抓身體位置和SortingOrder)
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

            // 3. 設定頭髮 (完全依照 Inspector 設定)
            if (targetSeat.runtimeHair != null)
            {
                targetSeat.runtimeHair.gameObject.SetActive(true);
                targetSeat.runtimeHair.sprite = GetSpriteSafe(hairSprites, task.hairId);

                // ⭐ 使用獨立的 Offset 和 Scale
                targetSeat.runtimeHair.transform.position = basePos + targetSeat.hairOffset;
                targetSeat.runtimeHair.transform.localScale = targetSeat.hairScale;
                targetSeat.runtimeHair.sortingOrder = baseOrder + 2;
                targetSeat.runtimeHair.color = Color.white;
            }

            // 4. 設定臉 (完全依照 Inspector 設定)
            if (targetSeat.runtimeFace != null)
            {
                targetSeat.runtimeFace.gameObject.SetActive(true);
                targetSeat.runtimeFace.sprite = GetSpriteSafe(faceSprites, task.faceId);

                // ⭐ 使用獨立的 Offset 和 Scale
                targetSeat.runtimeFace.transform.position = basePos + targetSeat.faceOffset;
                targetSeat.runtimeFace.transform.localScale = targetSeat.faceScale;
                targetSeat.runtimeFace.sortingOrder = baseOrder + 1;
                targetSeat.runtimeFace.color = Color.white;
            }

            // 5. 設定衣服 (完全依照 Inspector 設定)
            if (targetSeat.runtimeShirt != null)
            {
                targetSeat.runtimeShirt.gameObject.SetActive(true);
                targetSeat.runtimeShirt.sprite = GetSpriteSafe(shirtSprites, task.shirtId);

                // ⭐ 使用獨立的 Offset 和 Scale
                targetSeat.runtimeShirt.transform.position = basePos + targetSeat.shirtOffset;
                targetSeat.runtimeShirt.transform.localScale = targetSeat.shirtScale;
                targetSeat.runtimeShirt.sortingOrder = baseOrder + 1;
                targetSeat.runtimeShirt.color = Color.white;
            }
            targetSeat.currentAvatarObj.SetActive(true);
        }
    }

    private Sprite GetSpriteSafe(Sprite[] list, int id)
    {
        if (list == null || list.Length == 0) return null;
        if (id < 0 || id >= list.Length) return list[0];
        return list[id];
    }

    void LateUpdate()
    {
        if (!needsUpdate || latestSnapshot == null) return;
        ProcessSeatUpdates(latestSnapshot);
        needsUpdate = false;
    }

    private void ProcessSeatUpdates(DataSnapshot snapshot)
    {
        for (int i = 0; i < seats.Length; i++)
        {
            var seat = seats[i];
            var uid = snapshot.Child(seat.seatName).Value as string;

            if (!string.IsNullOrEmpty(uid))
            {
                if (seat.currentUid == uid && seat.currentAvatarObj != null) continue;

                seat.currentUid = uid;
                if (seat.currentAvatarObj != null) Destroy(seat.currentAvatarObj);

                GameObject newAvatar = Instantiate(avatarPrefab, seat.seatTransform.position, Quaternion.identity);
                seat.currentAvatarObj = newAvatar;
                newAvatar.SetActive(false);
                seat.runtimeController = newAvatar.GetComponent<PlayerSitController>();
                seat.runtimeHair = FindRenderer(newAvatar.transform, "hair_sit");
                seat.runtimeFace = FindRenderer(newAvatar.transform, "face_sit");
                seat.runtimeShirt = FindRenderer(newAvatar.transform, "shirt_sit");
                if (seat.runtimeHair != null) seat.runtimeHair.color = Color.clear;
                if (seat.runtimeFace != null) seat.runtimeFace.color = Color.clear;
                if (seat.runtimeShirt != null) seat.runtimeShirt.color = Color.clear;
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
            int hId = 0, fId = 0, sId = 0;

            if (equipSnapshot.Exists)
            {
                int.TryParse(equipSnapshot.Child("hair").Value?.ToString(), out hId);
                int.TryParse(equipSnapshot.Child("face").Value?.ToString(), out fId);
                int.TryParse(equipSnapshot.Child("shirt").Value?.ToString(), out sId);
            }

            lock (queueLock)
            {
                pendingAppearanceUpdates.Enqueue(new AppearanceTask
                {
                    seatIndex = seatIndex,
                    hairId = hId,
                    faceId = fId,
                    shirtId = sId
                });
            }
        });
    }
}