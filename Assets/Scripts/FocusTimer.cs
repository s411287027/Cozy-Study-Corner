using UnityEngine;
using TMPro;

public class FocusTimer : MonoBehaviour
{
    [Header("Settings")]
    public float defaultFocusMinutes = 25f;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TMP_InputField minutesInput;

    private float remainingRaw = 0f;     // 剩餘時間（float）
    private int   remainingSeconds = 0;  // 顯示用整數秒

    private float sessionRaw = 0f;       // 本輪已經跑的 float 秒
    private int   sessionSeconds = 0;    // 本輪已跑的整數秒

    private bool isRunning = false;

    void Start()
    {
        if (minutesInput != null && string.IsNullOrWhiteSpace(minutesInput.text))
        {
            minutesInput.text = defaultFocusMinutes.ToString("0");
        }

        float minutes = GetFocusMinutesFromInput();
        remainingRaw = minutes * 60f;
        remainingSeconds = Mathf.CeilToInt(remainingRaw);
        UpdateTimerText();
    }

    void Update()
    {
        if (!isRunning || remainingRaw <= 0f) return;

        float delta = Time.deltaTime;
        remainingRaw -= delta;
        if (remainingRaw < 0f) remainingRaw = 0f;

        sessionRaw += delta;

        // 更新顯示用整數秒
        int newRemaining = Mathf.CeilToInt(remainingRaw);
        int newSessionSec = Mathf.FloorToInt(sessionRaw);

        bool needUpdateText = false;

        if (newRemaining != remainingSeconds)
        {
            remainingSeconds = newRemaining;
            needUpdateText = true;
        }

        if (newSessionSec != sessionSeconds)
        {
            sessionSeconds = newSessionSec;

            // 同步給 Today 顯示
            StudySessionRuntime.currentExtraSeconds = sessionSeconds;
        }

        if (needUpdateText)
        {
            UpdateTimerText();
        }

        // 倒數結束
        if (remainingRaw <= 0f)
        {
            isRunning = false;

            if (sessionSeconds > 0)
            {
                // 累積整數秒
                StudyStats.AddStudySecondsInt(sessionSeconds);
            }

            sessionRaw = 0f;
            sessionSeconds = 0;
            StudySessionRuntime.currentExtraSeconds = 0;
        }
    }

    void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text = TimeFormatter.FormatHMS(remainingSeconds);
        }
    }

    float GetFocusMinutesFromInput()
    {
        if (minutesInput == null)
            return defaultFocusMinutes;

        if (float.TryParse(minutesInput.text, out float m))
        {
            return Mathf.Max(m, 1f); // 至少 1 分鐘
        }

        return defaultFocusMinutes;
    }

    public void OnStartButton()
    {
        float minutes = GetFocusMinutesFromInput();

        if (!isRunning)
        {
            remainingRaw = minutes * 60f;
            remainingSeconds = Mathf.CeilToInt(remainingRaw);

            sessionRaw = 0f;
            sessionSeconds = 0;
            StudySessionRuntime.currentExtraSeconds = 0;

            UpdateTimerText();
        }

        isRunning = true;
    }

    public void OnPauseButton()
    {
        isRunning = false;
    }

    public void OnStopButton()
    {
        // 手動停止：也要把目前整數秒加進今天
        if (sessionSeconds > 0)
        {
            StudyStats.AddStudySecondsInt(sessionSeconds);
        }

        isRunning = false;

        sessionRaw = 0f;
        sessionSeconds = 0;
        StudySessionRuntime.currentExtraSeconds = 0;

        float minutes = GetFocusMinutesFromInput();
        remainingRaw = minutes * 60f;
        remainingSeconds = Mathf.CeilToInt(remainingRaw);

        UpdateTimerText();
    }

    public void OnMinutesInputChanged(string _)
    {
        if (!isRunning)
        {
            float minutes = GetFocusMinutesFromInput();
            remainingRaw = minutes * 60f;
            remainingSeconds = Mathf.CeilToInt(remainingRaw);
            UpdateTimerText();
        }
    }
}
