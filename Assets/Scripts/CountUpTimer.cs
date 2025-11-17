using UnityEngine;
using TMPro;

public class CountUpTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    private float elapsedRaw = 0f;   // 真正累積的 float 時間
    private int elapsedSeconds = 0;  // 對外/顯示用的整數秒
    private bool isRunning = false;

    void Start()
    {
        UpdateTimerText();
    }

    void Update()
    {
        if (!isRunning) return;

        elapsedRaw += Time.deltaTime;

        int newSec = Mathf.FloorToInt(elapsedRaw);
        if (newSec != elapsedSeconds)
        {
            elapsedSeconds = newSec;

            // 同步給 Today 顯示用
            StudySessionRuntime.currentExtraSeconds = elapsedSeconds;

            UpdateTimerText();
        }
    }

    void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text = TimeFormatter.FormatHMS(elapsedSeconds);
        }
    }

    public void OnStartButton()
    {
        isRunning = true;
    }

    public void OnPauseButton()
    {
        isRunning = false;
    }

    public void OnStopButton()
    {
        isRunning = false;

        if (elapsedSeconds > 0)
        {
            // ✅ 累積整數秒
            StudyStats.AddStudySecondsInt(elapsedSeconds);
        }

        // 清空當前 session
        elapsedRaw = 0f;
        elapsedSeconds = 0;
        StudySessionRuntime.currentExtraSeconds = 0;

        UpdateTimerText();
    }
}
