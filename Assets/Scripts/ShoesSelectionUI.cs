using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Auth;

public class ShoesSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject shoesButtonPrefab;
    public Transform content;

    [Header("Player 設定")]
    public Transform playerDisplayPosition;
    private GameObject player;
    private ShoesController shoesController;

    [Header("所有可選鞋子")]
    public List<ShoesData> shoesList = new List<ShoesData>();

    private DatabaseReference dbRef;

    // Firebase 擁有的鞋子 ID
    private HashSet<int> ownedShoesIDs = new HashSet<int>();

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;
        if (player == null)
        {
            Debug.LogError("找不到 Player!");
            return;
        }

        shoesController = player.GetComponentInChildren<ShoesController>();

        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 從 Firebase 讀玩家擁有的鞋子
        StartCoroutine(LoadOwnedShoesFromFirebase());
    }

    // ============================================================
    // 🔥 從 Firebase 讀 "users/UID/ownedItems/shoes"
    // ============================================================
    IEnumerator LoadOwnedShoesFromFirebase()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null) yield break;
        string uid = currentUser.UserId;

        var task = dbRef.Child("users").Child(uid).Child("ownedItems").Child("shoes")
                        .GetValueAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("❌ 無法讀取鞋子資料：" + task.Exception);
            yield break;
        }

        DataSnapshot snapshot = task.Result;

        ownedShoesIDs.Clear();

        // Firebase 格式：0:0, 1:1, 2:2 → 代表擁有鞋子 ID = 0,1,2
        foreach (var child in snapshot.Children)
        {
            int id = int.Parse(child.Value.ToString());
            ownedShoesIDs.Add(id);
        }

        // 生成 UI
        GenerateShoesUI();
    }

    // ============================================================
    // 🔥 根據 ownedShoesIDs 顯示鞋子按鈕
    // ============================================================
    void GenerateShoesUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var shoes in shoesList)
        {
            // 只顯示玩家擁有的鞋子
            if (!ownedShoesIDs.Contains(shoes.shoesID))
                continue;

            GameObject obj = Instantiate(shoesButtonPrefab, content);
            ShoesButtonUI ui = obj.GetComponent<ShoesButtonUI>();
            ui.Setup(shoes, this);
        }
    }

    // ============================================================
    // 🔥 玩家點擊更換鞋子 (已修改：加入儲存功能)
    // ============================================================
    public void SelectShoes(ShoesData shoes)
    {
        if (shoesController == null) return;

        // 1. 更新視覺顯示
        shoesController.shoesUp = shoes.shoesUp;
        shoesController.shoesDown = shoes.shoesDown;
        shoesController.shoesLeft = shoes.shoesLeft;
        shoesController.shoesRight = shoes.shoesRight;

        shoesController.shoesUpFrames = shoes.shoesUpFrames;
        shoesController.shoesDownFrames = shoes.shoesDownFrames;
        shoesController.shoesLeftFrames = shoes.shoesLeftFrames;
        shoesController.shoesRightFrames = shoes.shoesRightFrames;

        shoesController.ForceUpdateShoesSprite(0f, -1f);

        Debug.Log($"成功替換鞋子：{shoes.shoesName}");

        // 2. [新增] 儲存 ID 到 Firebase
        SaveCurrentShoesToFirebase(shoes.shoesID);
    }

    // ============================================================
    // 🔥 [新增] 儲存當前鞋子 ID 到 Firebase
    // 路徑: users/UID/currentEquip/shoes
    // ============================================================
    private void SaveCurrentShoesToFirebase(int shoesID)
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogError("尚未登入，無法儲存鞋子裝備");
            return;
        }

        string uid = currentUser.UserId;

        // 設定路徑並寫入 ID
        dbRef.Child("users").Child(uid).Child("currentEquip").Child("shoes")
             .SetValueAsync(shoesID).ContinueWith(task =>
             {
                 if (task.IsFaulted)
                 {
                     Debug.LogError("❌ 鞋子儲存失敗：" + task.Exception);
                 }
                 else
                 {
                     Debug.Log($"✅ 鞋子 ID [{shoesID}] 已儲存至 currentEquip/shoes");
                 }
             });
    }
}