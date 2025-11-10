using UnityEngine;
using TMPro;

public class ProfileUIController : MonoBehaviour
{
    public TMP_Text userNameText;
    public TMP_Text coinsText;
    public TMP_Text levelText;

    private void Start()
    {
        // 🔹 從 Singleton 抓資料
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

        // 🔹 若資料在進入 Scene 後才更新，可監聽事件
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
        // 記得解除監聽
        if (FirebaseDatabaseController.Instance != null)
            FirebaseDatabaseController.Instance.OnDataLoaded -= UpdateUI;
    }

    public void LogOut()
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
    }
}
