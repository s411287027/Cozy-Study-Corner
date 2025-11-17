using UnityEngine;
using TMPro;

public class TodayStudyDisplay : MonoBehaviour
{
    public TextMeshProUGUI todayText;

    void Update()
    {
        // 以前結束過的 session：先取整數
        int baseSeconds = Mathf.FloorToInt(StudyStats.GetTodayStudySeconds());
        // 目前這一輪正在跑的整數秒
        int totalSeconds = baseSeconds + StudySessionRuntime.currentExtraSeconds;

        if (todayText != null)
        {
            todayText.text = "Today: " + TimeFormatter.FormatHMS(totalSeconds);
        }
    }
}
