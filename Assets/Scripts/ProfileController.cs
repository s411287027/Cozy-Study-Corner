using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class ProfileUIController : MonoBehaviour
{
    public TMP_Text userNameText;
    public TMP_Text coinsText;
    public TMP_Text levelText;
    public TMP_Dropdown startTimeDropdown;
    public TMP_Dropdown endTimeDropdown;

    // 新增訊息輸入框與送出按鈕
    public TMP_InputField messageInput;

    private void Start()
    {
        var data = FirebaseDatabaseController.Instance.dts;

        if (data != null)
        {
            userNameText.text = data.UserName;
            coinsText.text = data.TotalCoins.ToString();
            levelText.text = data.CrrLevel.ToString();
        }
        else
        {
            userNameText.text = "No Data";
        }

        FirebaseDatabaseController.Instance.OnDataLoaded += UpdateUI;
    }

    private void UpdateUI()
    {
        var data = FirebaseDatabaseController.Instance.dts;
        userNameText.text = data.UserName;
        coinsText.text = data.TotalCoins.ToString();
        levelText.text = data.CrrLevel.ToString();
    }

    private void OnDestroy()
    {
        if (FirebaseDatabaseController.Instance != null)
            FirebaseDatabaseController.Instance.OnDataLoaded -= UpdateUI;
    }

    public void LogOut1()
    {
        FirebaseController au = FindObjectOfType<FirebaseController>();

        // 定義一個切換場景與銷毀的函式 (避免代碼重複)
        void FinishLogoutProcess()
        {
            // --- 銷毀舊物件 ---
            if (au != null) Destroy(au.gameObject);

            if (FirebaseDatabaseController.Instance != null)
                Destroy(FirebaseDatabaseController.Instance.gameObject);
            else
            {
                var db = FindObjectOfType<FirebaseDatabaseController>();
                if (db != null) Destroy(db.gameObject);
            }

            if (FriendSystemController.Instance != null)
                Destroy(FriendSystemController.Instance.gameObject);

            // --- 切換場景 ---
            Debug.Log("👋以此狀態切換場景...");
            SceneManager.LoadScene("CozyStudyCorner"); // 請確認這是你的登入場景名稱
        }

        if (au != null)
        {
            Debug.Log("⏳ 開始執行登出程序...");

            // ⭐ 呼叫改寫後的 Async 版本，並等待它完成
            au.LogOutAsync().ContinueWithOnMainThread(task =>
            {
                // 無論 Firebase 寫入成功或失敗，最後都要執行銷毀與切換
                FinishLogoutProcess();
            });
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 FirebaseController，強制切換。");
            FinishLogoutProcess();
        }
    }

    public void SetReservationTime()
    {
        string uid = FirebaseDatabaseController.Instance.userId;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("❌ UID not found!");
            return;
        }

        string start = startTimeDropdown.options[startTimeDropdown.value].text;
        string end = endTimeDropdown.options[endTimeDropdown.value].text;
        string time = start + "-" + end;

        Debug.Log("📌 Setting reservation time: " + time);

        FirebaseDatabaseController.Instance.SetTomorrowReservationTime(uid, time);
    }

    // 🔹 新增：送出訊息到 Firebase
    public void SendMessageToFirebase()
    {
        string uid = FirebaseDatabaseController.Instance.userId;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("❌ UID not found!");
            return;
        }

        string message = messageInput.text;
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("⚠️ 訊息為空，無法送出！");
            return;
        }

        // 將訊息寫入 Firebase Database
        FirebaseDatabaseController.Instance.SetUserMessage(uid, message);

        Debug.Log("📩 Sent message: " + message);

        // 清空輸入框
        messageInput.text = "";
    }
}
