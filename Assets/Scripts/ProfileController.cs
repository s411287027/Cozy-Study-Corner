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
        // 初始化時先更新一次
        UpdateUI();

        // 訂閱事件，當資料庫有變動通知時自動更新
        if (FirebaseDatabaseController.Instance != null)
        {
            FirebaseDatabaseController.Instance.OnDataLoaded += UpdateUI;
        }
    }

    // 🔥 修改重點：將 private 改為 public
    // 這樣其他的腳本 (例如 ShopController) 就可以在購買後呼叫這個方法
    public void UpdateUI()
    {
        if (FirebaseDatabaseController.Instance == null) return;

        var data = FirebaseDatabaseController.Instance.dts;

        if (data != null)
        {
            userNameText.text = data.UserName;
            coinsText.text = data.TotalCoins.ToString(); // 更新金幣
            levelText.text = data.CrrLevel.ToString();
        }
        else
        {
            userNameText.text = "No Data";
        }
    }

    private void OnDestroy()
    {
        if (FirebaseDatabaseController.Instance != null)
            FirebaseDatabaseController.Instance.OnDataLoaded -= UpdateUI;
    }

    // ... (LogOut1, SetReservationTime, SendMessageToFirebase 等函式保持不變) ...

    public void LogOut1()
    {
        // ... (保持原本的登出邏輯) ...
        FirebaseController au = FindObjectOfType<FirebaseController>();

        void FinishLogoutProcess()
        {
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

            Debug.Log("👋以此狀態切換場景...");
            SceneManager.LoadScene("CozyStudyCorner");
        }

        if (au != null)
        {
            Debug.Log("⏳ 開始執行登出程序...");
            au.LogOutAsync().ContinueWithOnMainThread(task =>
            {
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
        // ... (保持原本邏輯) ...
        string uid = FirebaseDatabaseController.Instance.userId;
        if (string.IsNullOrEmpty(uid)) return;
        string start = startTimeDropdown.options[startTimeDropdown.value].text;
        string end = endTimeDropdown.options[endTimeDropdown.value].text;
        string time = start + "-" + end;
        FirebaseDatabaseController.Instance.SetTomorrowReservationTime(uid, time);
    }

    public void SendMessageToFirebase()
    {
        // ... (保持原本邏輯) ...
        string uid = FirebaseDatabaseController.Instance.userId;
        if (string.IsNullOrEmpty(uid)) return;
        string message = messageInput.text;
        if (string.IsNullOrEmpty(message)) return;
        FirebaseDatabaseController.Instance.SetUserMessage(uid, message);
        messageInput.text = "";
    }
}