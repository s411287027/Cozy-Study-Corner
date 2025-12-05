using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SeatClickArea : MonoBehaviour
{
    public string seatId;
    public SeatManager_Forest manager;

    public Button addFriendButton;
    public Button stickyNoteButton;

    private void Awake()
    {
        // 初始隱藏
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(false);

        // 綁定按鈕事件
        if (addFriendButton != null)
            addFriendButton.onClick.AddListener(OnAddFriendClicked);

        if (stickyNoteButton != null)
            stickyNoteButton.onClick.AddListener(OnStickyNoteClicked);

        // 綁定 ClickArea 自身的 Button
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClickAreaClicked);
    }

    private void OnClickAreaClicked()
    {
        manager?.OnSeatClicked(seatId, this);
    }

    private void OnAddFriendClicked()
    {
        Debug.Log($"加好友：{seatId}");
        HideButtons();
    }

    private void OnStickyNoteClicked()
    {
        Debug.Log($"傳便條給：{seatId}");
        HideButtons();
    }

    public void ShowButtons()
    {
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(true);
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(true);
    }

    public void HideButtons()
    {
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(false);
    }
}
