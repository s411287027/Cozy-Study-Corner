using UnityEngine;

public class StudyUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject homePanel;      
    public GameObject modePanel;      
    public GameObject countUpPanel;   
    public GameObject pomodoroSetupPanel; // ✅ 新增：番茄鐘時間輸入頁
    public GameObject pomodoroPanel;      // 原本的番茄鐘畫面
    public GameObject chartPanel;     

    [Header("Seats Group (All Sit Buttons)")]
    public GameObject seatsGroup;     

    void Start()
    {
        // 一開始只有座位
        HideAll();
        if (seatsGroup != null)
            seatsGroup.SetActive(true);
    }

    void HideAll()
    {
        homePanel?.SetActive(false);
        modePanel?.SetActive(false);
        countUpPanel?.SetActive(false);
        pomodoroSetupPanel?.SetActive(false); // ✅ 記得關
        pomodoroPanel?.SetActive(false);
        chartPanel?.SetActive(false);
    }

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

    // ===== 座位 & Home =====

    public void OnSeatSit()
    {
        if (seatsGroup != null)
            seatsGroup.SetActive(false);   // 全部座位消失

        ShowHome();
    }

    public void OnBackToSeats()
    {
        HideAll();
        if (seatsGroup != null)
            seatsGroup.SetActive(true);
    }

    // ===== Home / Mode / Pomodoro 流程 =====

    public void OnStudyButton()
    {
        ShowMode();
    }

    public void OnChooseCountUp()
    {
        ShowCountUp();
    }

    // 🟥 Mode 裡的「番茄鐘」按鈕 → 先去輸入時間的畫面
    public void OnChoosePomodoro()
    {
        ShowPomodoroSetup();
    }

    // ✅ 輸入時間畫面的「確認」按鈕：時間設定好了 → 進入番茄鐘 Start 頁
    public void OnPomodoroDurationConfirmed()
    {
        ShowPomodoro();
    }

    // ✅ 番茄鐘 Start 頁的 Back 按鈕：回到輸入時間畫面（只會在開始前可用）
    public void OnBackToPomodoroSetup()
    {
        ShowPomodoroSetup();
    }

    public void OnBackToMode()
    {
        ShowMode();
    }

    public void OnBackToHome()
    {
        ShowHome();
    }

    public void OnGoToCharts()
    {
        ShowChart();
    }
}
