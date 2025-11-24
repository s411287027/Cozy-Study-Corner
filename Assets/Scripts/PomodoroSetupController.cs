using UnityEngine;
using TMPro;

public class PomodoroSetupController : MonoBehaviour
{
    public TMP_InputField minutesInput;
    public FocusTimer focusTimer;
    public StudyUIManager uiManager;

    public void OnConfirmButton()
    {
        int minutes = 25;
        if (minutesInput != null && int.TryParse(minutesInput.text, out int m))
            minutes = Mathf.Max(1, m);

        focusTimer.SetDurationMinutes(minutes);
        uiManager.OnPomodoroDurationConfirmed();  // 切到番茄鐘頁
    }

    public void OnCancelButton()
    {
        uiManager.OnBackToMode();
    }
}
