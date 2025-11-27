using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Auth;

public class PantsSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject pantsButtonPrefab;
    public Transform content;

    [Header("Player 設定")]
    public Transform playerDisplayPosition;
    private GameObject player;
    private PantsController pantsController;

    [Header("所有可選褲子")]
    public List<PantsData> pantsList = new List<PantsData>();

    private DatabaseReference dbRef;

    // Firebase 擁有的褲子 ID
    private HashSet<int> ownedPantsIDs = new HashSet<int>();

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;
        if (player == null)
        {
            Debug.LogError("找不到 Player!");
            return;
        }

        pantsController = player.GetComponentInChildren<PantsController>();

        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 從 Firebase 讀玩家擁有的褲子
        StartCoroutine(LoadOwnedPantsFromFirebase());
    }

    // ============================================================
    // 🔥 從 Firebase 讀 "users/UID/ownedItems/pants"
    // ============================================================
    IEnumerator LoadOwnedPantsFromFirebase()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null) yield break;
        string uid = currentUser.UserId;

        var task = dbRef.Child("users").Child(uid).Child("ownedItems").Child("pants")
                        .GetValueAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("❌ 無法讀取褲子資料：" + task.Exception);
            yield break;
        }

        DataSnapshot snapshot = task.Result;

        ownedPantsIDs.Clear();

        // Firebase 格式：0:0, 1:1, 2:2 → 代表擁有褲子 ID = 0,1,2
        foreach (var child in snapshot.Children)
        {
            int id = int.Parse(child.Value.ToString());
            ownedPantsIDs.Add(id);
        }

        // 生成 UI
        GeneratePantsUI();
    }

    // ============================================================
    // 🔥 根據 ownedPantsIDs 顯示褲子按鈕
    // ============================================================
    void GeneratePantsUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var pants in pantsList)
        {
            // 只顯示玩家擁有的褲子
            if (!ownedPantsIDs.Contains(pants.pantsID))
                continue;

            GameObject obj = Instantiate(pantsButtonPrefab, content);
            PantsButtonUI ui = obj.GetComponent<PantsButtonUI>();
            ui.Setup(pants, this);
        }
    }

    // ============================================================
    // 🔥 玩家點擊更換褲子 (已修改：加入儲存功能)
    // ============================================================
    public void SelectPants(PantsData p)
    {
        if (pantsController == null) return;

        // 1. 更新視覺顯示
        pantsController.pantsUp = p.pantsUp;
        pantsController.pantsDown = p.pantsDown;
        pantsController.pantsLeft = p.pantsLeft;
        pantsController.pantsRight = p.pantsRight;

        pantsController.pantsUpFrames = p.pantsUpFrames;
        pantsController.pantsDownFrames = p.pantsDownFrames;
        pantsController.pantsLeftFrames = p.pantsLeftFrames;
        pantsController.pantsRightFrames = p.pantsRightFrames;

        // 顯示向下姿勢
        pantsController.ForceUpdatePantsSprite(0f, -1f);

        Debug.Log($"成功替換褲子：{p.pantsName}");

        // 2. [新增] 儲存 ID 到 Firebase
        SaveCurrentPantsToFirebase(p.pantsID);
    }

    // ============================================================
    // 🔥 [新增] 儲存當前褲子 ID 到 Firebase
    // 路徑: users/UID/currentEquip/pants
    // ============================================================
    private void SaveCurrentPantsToFirebase(int pantsID)
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogError("尚未登入，無法儲存褲子裝備");
            return;
        }

        string uid = currentUser.UserId;

        // 設定路徑並寫入 ID
        dbRef.Child("users").Child(uid).Child("currentEquip").Child("pants")
             .SetValueAsync(pantsID).ContinueWith(task =>
             {
                 if (task.IsFaulted)
                 {
                     Debug.LogError("❌ 褲子儲存失敗：" + task.Exception);
                 }
                 else
                 {
                     Debug.Log($"✅ 褲子 ID [{pantsID}] 已儲存至 currentEquip/pants");
                 }
             });
    }
}