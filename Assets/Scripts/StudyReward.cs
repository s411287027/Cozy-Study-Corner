using UnityEngine;
using TMPro;

public class StudyReward : MonoBehaviour
{
    [Header("Coin System")]
    public MonoBehaviour dtsScript;  // 掛有 TotalCoins 的腳本
    public string totalCoinsFieldName = "TotalCoins";

    [Header("UI (optional)")]
    public TextMeshProUGUI lastSessionTimeText;
    public TextMeshProUGUI lastSessionCoinsText;

    // 每 5 分鐘 10 金幣
    public int coinsPerBlock = 10;
    public int secondsPerBlock = 300; // 5*60

    // ✅ 在計時結束時呼叫
    public void GiveReward(float sessionSeconds)
    {
        if (sessionSeconds <= 0f) sessionSeconds = 0f;

        int blocks = Mathf.FloorToInt(sessionSeconds / secondsPerBlock);
        int coins = blocks * coinsPerBlock;

        // 顯示本次時間 / 金幣
        if (lastSessionTimeText != null)
            lastSessionTimeText.text = "Study Time: " + TimeFormatter.FormatHMS(sessionSeconds);

        if (lastSessionCoinsText != null)
            lastSessionCoinsText.text = "Coins: +" + coins.ToString();

        // 把金幣加到 dts.TotalCoins
        if (coins > 0 && dtsScript != null)
        {
            var type = dtsScript.GetType();
            var field = type.GetField(totalCoinsFieldName);
            if (field != null)
            {
                int current = (int)field.GetValue(dtsScript);
                field.SetValue(dtsScript, current + coins);
            }
            else
            {
                Debug.LogWarning("StudyReward: 找不到欄位 " + totalCoinsFieldName);
            }
        }
    }
}
