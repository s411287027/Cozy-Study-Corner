using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FocusTimer : MonoBehaviour
{
    [Header("Settings")]
    public float defaultFocusMinutes = 25f;   // 預設番茄鐘分鐘數

    [Header("UI")]
    public TextMeshProUGUI timerText;        // 顯示倒數時間
    public TMP_InputField minutesInput;      // 使用者輸入幾分鐘
    public Button startButton;               // Start 按鈕
    public Button pauseButton;               // Pause 按鈕
    public Button backButton;                // 返回按鈕（回到輸入時間 / Mode）

    [Header("Result Panel (彈出視窗)")]
    public GameObject resultPanel;           // 顯示結果的 Panel
    public TextMeshProUGUI resultTimeText;   // 顯示本次 Study Time
    public TextMeshProUGUI resultCoinsText;  // 顯示本次金幣

    [Header("Reward Settings")]
    public int coinsPerBlock = 10;           // 每一個時間區段的金幣數（例如 10）
    public int secondsPerBlock = 300;        // 一個時間區段幾秒（5 分鐘 = 300 秒）

    // ===== 內部狀態 =====
    private float remainingRaw = 0f;         // 剩餘時間（float 秒）
    private int remainingSeconds = 0;      // 剩餘時間（整數秒，用來顯示）

    private float sessionRaw = 0f;           // 本輪已經跑的 float 秒
    private int sessionSeconds = 0;        // 本輪已跑的整數秒

    private bool isRunning = false;          // 是否正在倒數中
    private bool sessionActive = false;      // 這一輪是否已經開始（不管暫停與否）
    private bool rewardedThisSession = false;// 是否已經對這一輪結算過（避免重複計算）

    void Start()
    {
        // 若輸入框是空的，填入預設分鐘數
        if (minutesInput != null && string.IsNullOrWhiteSpace(minutesInput.text))
        {
            minutesInput.text = defaultFocusMinutes.ToString("0");
        }

        ResetToInitialDuration();
        ClearResultPanel();
    }

    void OnEnable()
    {
        // 每次 Panel 被重新顯示時，關閉結果視窗避免殘留
        ClearResultPanel();
    }

    // 依照目前輸入的分鐘數重設倒數時間與狀態
    void ResetToInitialDuration()
    {
        float minutes = GetFocusMinutesFromInput();
        remainingRaw = minutes * 60f;
        remainingSeconds = Mathf.CeilToInt(remainingRaw);

        sessionRaw = 0f;
        sessionSeconds = 0;
        StudySessionRuntime.currentExtraSeconds = 0;

        isRunning = false;
        sessionActive = false;
        rewardedThisSession = false;

        UpdateTimerText();

        // 一開始：Start 可按、Pause 灰掉、Back 可按
        if (startButton != null)
            startButton.interactable = true;
        if (pauseButton != null)
            pauseButton.interactable = false;
        if (backButton != null)
            backButton.interactable = true;
    }

    // 關閉結果視窗、清空文字
    void ClearResultPanel()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (resultTimeText != null) resultTimeText.text = "";
        if (resultCoinsText != null) resultCoinsText.text = "";
    }

    void Update()
    {
        if (!isRunning || !sessionActive) return;

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
            // 同步給今天即時顯示用
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
            FinishSessionAndReward();  // 結算這一輪
            EndSessionResetUI();
        }
    }

    void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text = TimeFormatter.FormatHMS(remainingSeconds);
        }
    }

    // 從輸入框取得設定的分鐘數
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

    // 給外部（例如 PomodoroSetupController）呼叫，直接指定分鐘
    public void SetDurationMinutes(int minutes)
    {
        minutes = Mathf.Max(1, minutes);
        defaultFocusMinutes = minutes;

        if (minutesInput != null)
        {
            minutesInput.text = minutes.ToString();
        }

        // 尚未開始這一輪時才重設時間
        if (!sessionActive && !isRunning)
        {
            ResetToInitialDuration();
            ClearResultPanel();
        }
    }

    // ===== 按鈕事件 =====

    // Start：第一次按會開啟新 session，之後按（在 running 狀態）不會重置
    public void OnStartButton()
    {
        if (isRunning) return; // 已經在跑就不重複開始

        if (!sessionActive)
        {
            // 開啟新的一輪
            float minutes = GetFocusMinutesFromInput();
            remainingRaw = minutes * 60f;
            remainingSeconds = Mathf.CeilToInt(remainingRaw);

            sessionRaw = 0f;
            sessionSeconds = 0;
            rewardedThisSession = false;
            StudySessionRuntime.currentExtraSeconds = 0;

            UpdateTimerText();
            ClearResultPanel();

            sessionActive = true;
        }

        isRunning = true;

        // 開始後：Start 灰掉、Pause 啟用、Back 灰掉
        if (startButton != null)
            startButton.interactable = false;
        if (pauseButton != null)
            pauseButton.interactable = true;
        if (backButton != null)
            backButton.interactable = false;
    }

    // 暫停：不清空時間，只停止 Update
    public void OnPauseButton()
    {
        if (!sessionActive) return;
        if (!isRunning) return;

        isRunning = false;

        // 暫停時：Start 可按（可以繼續）、Pause 灰掉，Back 通常仍維持灰掉避免中途改時間
        if (startButton != null)
            startButton.interactable = true;
        if (pauseButton != null)
            pauseButton.interactable = false;
    }

    // Stop：結束本輪、結算、重置時間
    public void OnStopButton()
    {
        if (!sessionActive) return;

        isRunning = false;

        FinishSessionAndReward();
        ResetToInitialDuration();

        // 停止後：Start 可按、Pause 灰掉、Back 可按
        if (startButton != null)
            startButton.interactable = true;
        if (pauseButton != null)
            pauseButton.interactable = false;
        if (backButton != null)
            backButton.interactable = true;
    }

    // 結束一輪後，將旗標歸零（不重設時間，由 ResetToInitialDuration 負責）
    void EndSessionResetUI()
    {
        sessionActive = false;
        StudySessionRuntime.currentExtraSeconds = 0;

        if (startButton != null)
            startButton.interactable = true;
        if (pauseButton != null)
            pauseButton.interactable = false;
        if (backButton != null)
            backButton.interactable = true;
    }

    // 結算本輪讀書時間 + 金幣，並顯示彈窗
    void FinishSessionAndReward()
    {
        if (rewardedThisSession) return;
        if (sessionSeconds <= 0) return;

        // 超過 3 小時的 session 不計入
        int maxSessionSeconds = 3 * 60 * 60; // 10800 秒 = 3 小時
        if (sessionSeconds > maxSessionSeconds)
        {
            rewardedThisSession = true;
            StudySessionRuntime.currentExtraSeconds = 0;

            if (resultPanel != null) resultPanel.SetActive(true);

            if (resultTimeText != null)
                resultTimeText.text = "Study Time: " + TimeFormatter.FormatHMS(sessionSeconds);

            if (resultCoinsText != null)
                resultCoinsText.text = "This session exceeded 3 hours and will not be counted.";

            return; // 不記錄時間、不加金幣
        }

        rewardedThisSession = true;

        // 1. 把這一輪秒數加入今天累積
        StudyStats.AddStudySecondsInt(sessionSeconds);

        // 2. 算金幣：每 secondsPerBlock 秒給 coinsPerBlock 金幣
        int blocks = sessionSeconds / secondsPerBlock;
        int coins = blocks * coinsPerBlock;

        // 3. 顯示結果 Panel
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultTimeText != null)
        {
            resultTimeText.text = "Study Time: " + TimeFormatter.FormatHMS(sessionSeconds);
        }

        if (resultCoinsText != null)
        {
            resultCoinsText.text = "Coins: +" + coins;
        }

        // 4. 寫入 FirebaseDatabaseController 的 TotalCoins
        // 4. 實際加到 FirebaseDatabaseController
        if (coins > 0 && FirebaseDatabaseController.Instance != null)
        {
            // 呼叫剛剛寫的新函式，只更新金幣
            FirebaseDatabaseController.Instance.AddCoins(coins);
        }
        else if (coins > 0 && FirebaseDatabaseController.Instance == null)
        {
            Debug.LogWarning("CountUpTimer: FirebaseDatabaseController.Instance is null, cannot add coins.");
        }

        // 5. 清掉 runtime 顯示
        StudySessionRuntime.currentExtraSeconds = 0;
    }

    // 當分鐘輸入欄改變時（還沒開始這一輪），更新初始倒數時間
    public void OnMinutesInputChanged(string _)
    {
        if (!sessionActive && !isRunning)
        {
            ResetToInitialDuration();
        }
    }
}
