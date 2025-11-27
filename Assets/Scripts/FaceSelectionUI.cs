using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Auth;

public class FaceSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject faceButtonPrefab;
    public Transform content;

    [Header("Player 設定")]
    public Transform playerDisplayPosition;
    private GameObject player;
    private FaceController faceController;

    [Header("所有可選臉部")]
    public List<FaceData> faceList = new List<FaceData>();

    private DatabaseReference dbRef;

    // Firebase 擁有的臉部 ID
    private HashSet<int> ownedFaceIDs = new HashSet<int>();

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;
        if (player == null)
        {
            Debug.LogError("找不到 Player!");
            return;
        }

        faceController = player.GetComponentInChildren<FaceController>();

        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 使用正確的 IEnumerator（非泛型）
        StartCoroutine(LoadOwnedFacesFromFirebase());
    }

    // ✔ 正確使用 IEnumerator（不是 IEnumerator<T>）
    IEnumerator LoadOwnedFacesFromFirebase()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null) yield break;
        string uid = currentUser.UserId;

        var task = dbRef.Child("users").Child(uid).Child("ownedItems").Child("face")
                        .GetValueAsync();

        // 等待 Firebase 回傳
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("❌ 無法讀取臉部資料：" + task.Exception);
            yield break;
        }

        DataSnapshot snapshot = task.Result;

        ownedFaceIDs.Clear();

        // Firebase 資料格式：
        // 0:1, 1:4, 2:2 → 代表擁有 1,4,2
        foreach (var child in snapshot.Children)
        {
            int id = int.Parse(child.Value.ToString());
            ownedFaceIDs.Add(id);
        }

        // 生成 UI
        GenerateFaceUI();
    }

    void GenerateFaceUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var face in faceList)
        {
            // 只顯示玩家擁有的 faceID
            if (!ownedFaceIDs.Contains(face.faceID))
                continue;

            GameObject obj = Instantiate(faceButtonPrefab, content);
            FaceButtonUI ui = obj.GetComponent<FaceButtonUI>();
            ui.Setup(face, this);
        }
    }

    // ============================================================
    // 🔥 玩家點擊更換臉部 (已修改：加入儲存功能)
    // ============================================================
    public void SelectFace(FaceData face)
    {
        if (faceController == null) return;

        // 1. 更新視覺顯示
        faceController.faceUp = face.faceUp;
        faceController.faceDown = face.faceDown;
        faceController.faceLeft = face.faceLeft;
        faceController.faceRight = face.faceRight;

        faceController.faceUpFrames = face.faceUpFrames;
        faceController.faceDownFrames = face.faceDownFrames;
        faceController.faceLeftFrames = face.faceLeftFrames;
        faceController.faceRightFrames = face.faceRightFrames;

        faceController.ForceUpdateFaceSprite(0f, -1f);
        Debug.Log($"成功替換臉部：{face.faceName}");

        // 2. [新增] 儲存 ID 到 Firebase
        SaveCurrentFaceToFirebase(face.faceID);
    }

    // ============================================================
    // 🔥 [新增] 儲存當前臉部 ID 到 Firebase
    // 路徑: users/UID/currentEquip/face
    // ============================================================
    private void SaveCurrentFaceToFirebase(int faceID)
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogError("尚未登入，無法儲存臉部裝備");
            return;
        }

        string uid = currentUser.UserId;

        // 設定路徑並寫入 ID
        dbRef.Child("users").Child(uid).Child("currentEquip").Child("face")
             .SetValueAsync(faceID).ContinueWith(task =>
             {
                 if (task.IsFaulted)
                 {
                     Debug.LogError("❌ 臉部儲存失敗：" + task.Exception);
                 }
                 else
                 {
                     Debug.Log($"✅ 臉部 ID [{faceID}] 已儲存至 currentEquip/face");
                 }
             });
    }
}