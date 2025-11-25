using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Auth;

public class ShirtSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject shirtButtonPrefab;
    public Transform content;

    [Header("Player 設定")]
    public Transform playerDisplayPosition;
    private GameObject player;
    private ShirtController shirtController;

    [Header("所有可選衣服")]
    public List<ShirtData> shirtList = new List<ShirtData>();

    private DatabaseReference dbRef;

    // Firebase 擁有的衣服 ID
    private HashSet<int> ownedShirtIDs = new HashSet<int>();

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;
        if (player == null)
        {
            Debug.LogError("找不到 Player!");
            return;
        }

        shirtController = player.GetComponentInChildren<ShirtController>();

        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 從 Firebase 讀玩家擁有的衣服
        StartCoroutine(LoadOwnedShirtsFromFirebase());
    }

    // ============================================================
    // 🔥 從 Firebase 讀 "users/UID/ownedItems/shirt"
    // ============================================================
    IEnumerator LoadOwnedShirtsFromFirebase()
    {
        string uid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        var task = dbRef.Child("users").Child(uid).Child("ownedItems").Child("shirt")
                        .GetValueAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("❌ 無法讀取衣服資料：" + task.Exception);
            yield break;
        }

        DataSnapshot snapshot = task.Result;

        ownedShirtIDs.Clear();

        // Firebase 格式：0:0, 1:1, 2:2 → 代表擁有衣服 ID = 0,1,2
        foreach (var child in snapshot.Children)
        {
            // ⚠️ 根據你 Firebase 存的是 value 或 key 決定使用哪個
            // 如果像你說的 0:0, 1:1, 2:2 → value 即是衣服 ID
            int id = int.Parse(child.Value.ToString());
            ownedShirtIDs.Add(id);
        }

        // 生成 UI
        GenerateShirtUI();
    }

    // ============================================================
    // 🔥 根據 ownedShirtIDs 顯示衣服按鈕
    // ============================================================
    void GenerateShirtUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var shirt in shirtList)
        {
            // 只顯示玩家擁有的衣服
            //if (!ownedShirtIDs.Contains(shirt.shirtID))
            //    continue;

            GameObject obj = Instantiate(shirtButtonPrefab, content);
            ShirtButtonUI ui = obj.GetComponent<ShirtButtonUI>();
            ui.Setup(shirt, this);
        }
    }

    // ============================================================
    // 🔥 玩家點擊更換衣服
    // ============================================================
    public void SelectShirt(ShirtData shirt)
    {
        if (shirtController == null) return;

        shirtController.shirtUp = shirt.shirtUp;
        shirtController.shirtDown = shirt.shirtDown;
        shirtController.shirtLeft = shirt.shirtLeft;
        shirtController.shirtRight = shirt.shirtRight;

        shirtController.shirtUpFrames = shirt.shirtUpFrames;
        shirtController.shirtDownFrames = shirt.shirtDownFrames;
        shirtController.shirtLeftFrames = shirt.shirtLeftFrames;
        shirtController.shirtRightFrames = shirt.shirtRightFrames;

        // 顯示向下姿勢
        shirtController.UpdateShirtDirection(0f, -1f);

        Debug.Log($"成功替換衣服：{shirt.shirtName}");
    }
}
