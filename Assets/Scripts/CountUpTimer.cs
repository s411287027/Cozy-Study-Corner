using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CountUpTimer : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText;        // 正計時顯示
    public Button startButton;               // Start 按鈕
    public Button pauseButton;               // Pause 按鈕

    [Header("Result Panel (彈出視窗)")]
    public GameObject resultPanel;           // 顯示結果用 Panel
    public TextMeshProUGUI resultTimeText;   // 顯示本次 Study Time
    public TextMeshProUGUI resultCoinsText;  // 顯示本次金幣

    [Header("Reward Settings")]
    public int coinsPerBlock = 10;           // 每一個時間區段的金幣數（10）
    public int secondsPerBlock = 300;        // 一個時間區段幾秒（5 分鐘）

    // ===== 內部狀態 =====
    private float elapsedRaw = 0f;           // 真正累積的 float 時間
    private int elapsedSeconds = 0;        // 顯示用整數秒
    private bool isRunning = false;         // 是否正在計時
    private bool sessionActive = false;     // 是否有一輪正在進行
    private bool rewardedThisSession = false;

    void Start()
    {
        ResetTimer();
        ClearResultPanel();
    }

    void OnEnable()
    {
        // 每次 Panel 顯示時，關閉結果視窗避免殘留
        ClearResultPanel();
    }

    // 重設時間與狀態
    void ResetTimer()
    {
        elapsedRaw = 0f;
        elapsedSeconds = 0;
        isRunning = false;
        sessionActive = false;
        rewardedThisSession = false;
        StudySessionRuntime.currentExtraSeconds = 0;
        UpdateTimerText();

        // 一開始：Start 可按、Pause 灰掉
        if (startButton != null)
            startButton.interactable = true;
        if (pauseButton != null)
            pauseButton.interactable = false;
    }

    void ClearResultPanel()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (resultTimeText != null) resultTimeText.text = "";
        if (resultCoinsText != null) resultCoinsText.text = "";
    }

    void Update()
    {
        if (!isRunning || !sessionActive) return;

        elapsedRaw += Time.deltaTime;

        int newSec = Mathf.FloorToInt(elapsedRaw);
        if (newSec != elapsedSeconds)
        {
            elapsedSeconds = newSec;

            // 同步給今天即時顯示用
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

    // Start：第一次會開新 session，Pause 之後再按 Start 只會繼續，不會清零
    public void OnStartButton()
    {
        if (isRunning) return;

        if (!sessionActive)
        {
            // 開啟新的一輪
            ResetTimer();
            ClearResultPanel();
            sessionActive = true;
        }

        isRunning = true;

        // 開始後：Start 灰掉、Pause 亮
        if (startButton != null)
            startButton.interactable = false;
        if (pauseButton != null)
            pauseButton.interactable = true;
    }

    // Pause：只是暫停，不清零
    public void OnPauseButton()
    {
        if (!sessionActive) return;
        if (!isRunning) return;

        isRunning = false;

        // 暫停時：Start 可按（可以繼續）、Pause 灰掉
        if (startButton != null)
            startButton.interactable = true;
        if (pauseButton != null)
            pauseButton.interactable = false;
    }

    // Stop：結束一輪、結算並重設
    public void OnStopButton()
    {
        if (!sessionActive) return;

        isRunning = false;

        FinishSessionAndReward();

        // 清空當前 session，準備下一次
        ResetTimer();
    }

    // 結算這一輪讀書時間 + 金幣，並顯示彈窗
    void FinishSessionAndReward()
    {
        if (rewardedThisSession) return;
        if (elapsedSeconds <= 0) return;

        // 超過 3 小時就不算
        int maxSessionSeconds = 3 * 60 * 60; // 10800 秒
        if (elapsedSeconds > maxSessionSeconds)
        {
            rewardedThisSession = true;
            StudySessionRuntime.currentExtraSeconds = 0;

            if (resultPanel != null) resultPanel.SetActive(true);

            if (resultTimeText != null)
                resultTimeText.text = "Study Time: " + TimeFormatter.FormatHMS(elapsedSeconds);

            if (resultCoinsText != null)
                resultCoinsText.text = "This session exceeded 3 hours and will not be counted.";

            return; // 不計入統計、不發金幣
        }

        rewardedThisSession = true;

    

        // 1. 把這次秒數加到今天累積
        StudyStats.AddStudySecondsInt(elapsedSeconds);

        if (FirebaseDatabaseController.Instance != null)
        {
            FirebaseDatabaseController.Instance.AddStudySecondsForToday(elapsedSeconds);
        }

        // 2. 算金幣：每 secondsPerBlock 秒給 coinsPerBlock 金幣
        int blocks = elapsedSeconds / secondsPerBlock;
        int coins = blocks * coinsPerBlock;

        // 3. 顯示本次 Study Time & 金幣
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultTimeText != null)
        {
            resultTimeText.text = "Study Time: " + TimeFormatter.FormatHMS(elapsedSeconds);
        }

        if (resultCoinsText != null)
        {
            resultCoinsText.text = "Coins: +" + coins;
        }

        // 4. 實際加到 FirebaseDatabaseController.dts.TotalCoins
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
}
