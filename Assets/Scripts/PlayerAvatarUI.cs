using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq; 
using Firebase.Database;
using Firebase.Auth;

public class PlayerAvatarUI : MonoBehaviour
{
    [Header("UI 綁定")]
    public Image hairImage;
    public Image faceImage;
    public Image shirtImage;

    private DatabaseReference dbRef;
    private player_move playerMoveScript;

    IEnumerator Start()
    {
        Debug.Log("🔍 [1/5] 頭像腳本開始執行...");

        // 1. 檢查 Firebase 登入
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogError("⛔ [錯誤] 沒登入！請先執行 Login 場景。");
            yield break; 
        }
        string uid = currentUser.UserId;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 2. 等待 PlayerManager 和 Player 出現
        while (PlayerManager.Instance == null || PlayerManager.Instance.playerInstance == null)
        {
            yield return null;
        }

        // 3. 🔥【修正重點】強力搜尋 player_move 腳本
        GameObject playerObj = PlayerManager.Instance.playerInstance;
        Debug.Log($"🎮 PlayerManager 認為玩家物件是: [{playerObj.name}]");

        // 方法 A: 直接在該物件上找
        playerMoveScript = playerObj.GetComponent<player_move>();

        // 方法 B: 如果找不到，去它的「子物件」裡面找 (很常發生在腳本掛在下一層的情況)
        if (playerMoveScript == null)
        {
            Debug.LogWarning("⚠️ 在 root 找不到腳本，嘗試搜尋子物件...");
            playerMoveScript = playerObj.GetComponentInChildren<player_move>();
        }

        // 方法 C: 真的還是找不到，就搜遍全場景 (終極手段)
        if (playerMoveScript == null)
        {
            Debug.LogWarning("⚠️ 子物件也找不到，嘗試搜尋全場景...");
            playerMoveScript = FindObjectOfType<player_move>();
        }

        // 最後確認
        if (playerMoveScript == null)
        {
            Debug.LogError("❌ [崩潰] 真的找不到 player_move！請檢查 Player Prefab 是否有掛載此腳本。");
            yield break;
        }

        Debug.Log($"✅ [成功] 找到腳本了！掛在物件: [{playerMoveScript.gameObject.name}]");

        // 4. 下載裝備
        Debug.Log("⏳ [4/5] 開始從 Firebase 下載裝備...");
        var task = dbRef.Child("users").Child(uid).Child("currentEquip").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("❌ Firebase 讀取失敗：" + task.Exception);
            yield break;
        }

        DataSnapshot snapshot = task.Result;
        
        // 預設給 -1 代表沒資料
        int hairID = -1;
        int faceID = -1;
        int shirtID = -1;

        if (snapshot.Exists)
        {
            if (snapshot.HasChild("hair")) int.TryParse(snapshot.Child("hair").Value.ToString(), out hairID);
            if (snapshot.HasChild("face")) int.TryParse(snapshot.Child("face").Value.ToString(), out faceID);
            if (snapshot.HasChild("shirt")) int.TryParse(snapshot.Child("shirt").Value.ToString(), out shirtID);
            Debug.Log($"📥 Firebase 資料: Hair={hairID}, Face={faceID}, Shirt={shirtID}");
        }

        UpdateAvatarVisuals(hairID, faceID, shirtID);
    }

    void UpdateAvatarVisuals(int hairID, int faceID, int shirtID)
    {
        // 只有當 ID 不是 -1 時才更新圖片，否則保留你在 Scene 拉好的預設圖
        if (hairImage != null && hairID != -1)
        {
            HairData data = playerMoveScript.allHairList.FirstOrDefault(x => x.hairID == hairID);
            if (data != null) hairImage.sprite = data.hairDown;
        }

        if (faceImage != null && faceID != -1)
        {
            FaceData data = playerMoveScript.allFaceList.FirstOrDefault(x => x.faceID == faceID);
            if (data != null) faceImage.sprite = data.faceDown;
        }

        if (shirtImage != null && shirtID != -1)
        {
            ShirtData data = playerMoveScript.allShirtList.FirstOrDefault(x => x.shirtID == shirtID);
            if (data != null) shirtImage.sprite = data.shirtDown;
        }
        
        Debug.Log("✅ 頭像更新完畢");
    }
}