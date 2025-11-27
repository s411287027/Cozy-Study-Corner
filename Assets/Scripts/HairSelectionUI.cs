using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Auth;

public class HairSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject hairButtonPrefab;
    public Transform content;

    [Header("Player 設定")]
    public Transform playerDisplayPosition;
    private GameObject player;
    private HairController hairController;

    [Header("所有可選髮型")]
    public List<HairData> hairList = new List<HairData>();

    private DatabaseReference dbRef;

    // Firebase 擁有的髮型 ID
    private HashSet<int> ownedHairIDs = new HashSet<int>();

    void Start()
    {
        this.player = PlayerManager.Instance.playerInstance;
        if (this.player == null)
        {
            Debug.LogError("找不到 Player!");
            return;
        }

        hairController = this.player.GetComponentInChildren<HairController>();

        var playerSR = this.player.GetComponent<SpriteRenderer>();
        var hairSR = hairController?.GetComponent<SpriteRenderer>();
        if (playerSR != null && hairSR != null)
        {
            //  確保配件在 Player 上方 (Player 的 Order + 1)
            hairSR.sortingLayerName = playerSR.sortingLayerName;
            hairSR.sortingOrder = playerSR.sortingOrder + 1;
        }
        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 使用 IEnumerator 載入 Firebase 髮型資料
        StartCoroutine(LoadOwnedHairFromFirebase());
    }

    // ============================================================
    // 🔥 從 Firebase 讀 "ownedItems/hair"
    // ============================================================
    IEnumerator LoadOwnedHairFromFirebase()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null) yield break;
        string uid = currentUser.UserId;

        var task = dbRef.Child("users").Child(uid).Child("ownedItems").Child("hair")
                        .GetValueAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("❌ 無法讀取髮型資料：" + task.Exception);
            yield break;
        }

        DataSnapshot snapshot = task.Result;

        ownedHairIDs.Clear();

        // Firebase 格式：0:1, 1:4, 2:2 → 代表擁有髮型 ID = 1,4,2
        foreach (var child in snapshot.Children)
        {
            int id = int.Parse(child.Value.ToString());
            ownedHairIDs.Add(id);
        }

        // 生成 UI
        GenerateHairUI();
    }

    // ============================================================
    // 🔥 根據 ownedHairIDs 顯示髮型按鈕
    // ============================================================
    void GenerateHairUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var hair in hairList)
        {
            // 只顯示玩家擁有的髮型
            if (!ownedHairIDs.Contains(hair.hairID))
                continue;

            GameObject obj = Instantiate(hairButtonPrefab, content);
            HairButtonUI ui = obj.GetComponent<HairButtonUI>();
            ui.Setup(hair, this);
        }
    }

    // ============================================================
    // 🔥 玩家點擊更換髮型 (已修改：加入儲存功能)
    // ============================================================
    public void SelectHair(HairData hair)
    {
        if (hairController == null) return;

        // 1. 更新視覺顯示
        hairController.hairUp = hair.hairUp;
        hairController.hairDown = hair.hairDown;
        hairController.hairLeft = hair.hairLeft;
        hairController.hairRight = hair.hairRight;

        hairController.hairUpFrames = hair.hairUpFrames;
        hairController.hairDownFrames = hair.hairDownFrames;
        hairController.hairLeftFrames = hair.hairLeftFrames;
        hairController.hairRightFrames = hair.hairRightFrames;

        // 顯示向下姿勢
        hairController.ForceUpdateHairSprite(0f, -1f);

        Debug.Log($"成功替換髮型：{hair.hairName}");

        // 2. 儲存到 Firebase
        SaveCurrentHairToFirebase(hair.hairID);
    }

    // ============================================================
    // 🔥 [新增] 儲存當前裝備 ID 到 Firebase
    // 路徑: users/UID/currentEquip/hair
    // ============================================================
    private void SaveCurrentHairToFirebase(int hairID)
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogError("尚未登入，無法儲存裝備資訊");
            return;
        }

        string uid = currentUser.UserId;

        // 設定路徑並寫入 ID
        dbRef.Child("users").Child(uid).Child("currentEquip").Child("hair")
             .SetValueAsync(hairID).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ 髮型儲存失敗：" + task.Exception);
            }
            else
            {
                Debug.Log($"✅ 髮型 ID [{hairID}] 已儲存至 currentEquip/hair");
            }
        });
    }
}