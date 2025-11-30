using UnityEngine;

public class StudyUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject homePanel;          // 主畫面（書桌、回到座位）
    public GameObject modePanel;          // Study Mode 點進去選 Count-Up / Pomodoro
    public GameObject countUpPanel;       // 正計時
    public GameObject pomodoroSetupPanel; // 番茄鐘：設定時間的畫面
    public GameObject pomodoroPanel;      // 番茄鐘：正式倒數畫面
    public GameObject chartPanel;         // 圖表畫面

    [Header("Seats Group (All seats object)")]
    public GameObject seatsGroup;         // 所有座位的集合（最一開始）

    void Start()
    {
        // 一開始只顯示 Seats
        HideAll();
        if (seatsGroup != null)
            seatsGroup.SetActive(true);
    }

    // 關閉所有 Panel
    void HideAll()
    {
        homePanel?.SetActive(false);
        modePanel?.SetActive(false);
        countUpPanel?.SetActive(false);
        pomodoroSetupPanel?.SetActive(false);
        pomodoroPanel?.SetActive(false);
        chartPanel?.SetActive(false);
    }

    // ===== 顯示函式 =====

    void ShowHome()
    {
        HideAll();
        homePanel?.SetActive(true);
    }

    void ShowMode()
    {
        HideAll();
        modePanel?.SetActive(true);
    }

    void ShowCountUp()
    {
        HideAll();
        countUpPanel?.SetActive(true);
    }

    void ShowPomodoroSetup()
    {
        HideAll();
        pomodoroSetupPanel?.SetActive(true);
    }

    void ShowPomodoro()
    {
        HideAll();
        pomodoroPanel?.SetActive(true);
    }

    void ShowChart()
    {
        HideAll();
        chartPanel?.SetActive(true);
    }

    // ============================================================
    //                     座位相關 (最外層 UI)
    // ============================================================

    public void OnSeatSit()
    {
        // 玩家選位置 → 進入 HomePanel
        if (seatsGroup != null)
            seatsGroup.SetActive(false);

        ShowHome();
    }

    public void OnBackToSeats()
    {
        HideAll();
        if (seatsGroup != null)
            seatsGroup.SetActive(true);
    }

    // ============================================================
    //                     Home / Mode / Chart 流程
    // ============================================================

    public void OnStudyButton()
    {
        ShowMode();
    }

    public void OnGoToCharts()
    {
        ShowChart();
    }

    public void OnBackToHome()
    {
        ShowHome();
    }

    public void OnBackToMode()
    {
        ShowMode();
    }

    // ============================================================
    //                     Count-Up 流程
    // ============================================================

    public void OnChooseCountUp()
    {
        ShowCountUp();
    }

    // ============================================================
    //                     Pomodoro 流程
    // ============================================================

    public void OnChoosePomodoro()
    {
        ShowPomodoroSetup();
    }

    // 番茄鐘 → 設定完成 (按 "確認時間")
    public void OnPomodoroDurationConfirmed()
    {
        ShowPomodoro();
    }

    // 番茄鐘 → Back (只在還沒開始時能按)
    public void OnBackToPomodoroSetup()
    {
        ShowPomodoroSetup();
    }
}
