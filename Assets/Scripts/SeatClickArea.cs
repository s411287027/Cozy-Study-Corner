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
        // ��l����
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
        if (stickyNoteButton != null) stickyNoteButton.gameObject.SetActive(false);

        // �j�w���s�ƥ�
        if (addFriendButton != null)
            addFriendButton.onClick.AddListener(OnAddFriendClicked);

        if (stickyNoteButton != null)
            stickyNoteButton.onClick.AddListener(OnStickyNoteClicked);

        // �j�w ClickArea �ۨ��� Button
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
        Debug.Log($"�[�n�͡G{seatId}");
        HideButtons();
    }

    private void OnStickyNoteClicked()
    {
        Debug.Log($"�ǫK�����G{seatId}");
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
