using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

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
        if (au != null)
        {
            au.LogOut();
        }
        else
        {
            Debug.LogWarning("⚠️ FriendSystemController 尚未載入！");
        }
        SceneManager.LoadScene("CozyStudyCorner");
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
