using UnityEngine;

public class StudyUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject homePanel;      // 有 Study 按鈕的主畫面
    public GameObject modePanel;      // 選模式（正計時 / 番茄鐘）
    public GameObject countUpPanel;   // 正計時畫面
    public GameObject pomodoroPanel;  // 番茄鐘畫面

    void Start()
    {
        ShowHome();
    }

    void ShowHome()
    {
        homePanel.SetActive(true);
        modePanel.SetActive(false);
        countUpPanel.SetActive(false);
        pomodoroPanel.SetActive(false);
    }

    void ShowMode()
    {
        homePanel.SetActive(false);
        modePanel.SetActive(true);
        countUpPanel.SetActive(false);
        pomodoroPanel.SetActive(false);
    }

    // Study 按鈕
    public void OnStudyButton()
    {
        ShowMode();
    }

    // 選正計時
    public void OnChooseCountUp()
    {
        homePanel.SetActive(false);
        modePanel.SetActive(false);
        countUpPanel.SetActive(true);
        pomodoroPanel.SetActive(false);
    }

    // 選番茄鐘
    public void OnChoosePomodoro()
    {
        homePanel.SetActive(false);
        modePanel.SetActive(false);
        countUpPanel.SetActive(false);
        pomodoroPanel.SetActive(true);
    }

    // 從 Timer 回到模式選擇（Stop 之後用）
    public void OnBackToMode()
    {
        ShowMode();
    }

    // 從模式選擇回到 Study 主畫面
    public void OnBackToHome()
    {
        ShowHome();
    }
}
