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
        string uid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

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
    // 🔥 玩家點擊更換褲子
    // ============================================================
    public void SelectPants(PantsData p)
    {
        if (pantsController == null) return;

        pantsController.pantsUp = p.pantsUp;
        pantsController.pantsDown = p.pantsDown;
        pantsController.pantsLeft = p.pantsLeft;
        pantsController.pantsRight = p.pantsRight;

        pantsController.pantsUpFrames = p.pantsUpFrames;
        pantsController.pantsDownFrames = p.pantsDownFrames;
        pantsController.pantsLeftFrames = p.pantsLeftFrames;
        pantsController.pantsRightFrames = p.pantsRightFrames;

        // 顯示向下姿勢
        pantsController.UpdatePantsDirection(0f, -1f);

        Debug.Log($"成功替換褲子：{p.pantsName}");
    }
}
