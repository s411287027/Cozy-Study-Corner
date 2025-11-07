using UnityEngine;
using UnityEngine.UI;

public class FriendListItem : MonoBehaviour
{
    public string friendUID;
    public Button visitButton; // Inspector 拖入
    public FriendRoomController roomController;

    private void Awake()
    {
        roomController = FindObjectOfType<FriendRoomController>();
        if (roomController == null)
            Debug.LogError("FriendRoomController not found in scene!");

        if (visitButton != null)
            visitButton.onClick.AddListener(OnVisitClicked);
        else
            Debug.LogError("Visit Button not assigned!");
    }

    private void OnVisitClicked()
    {
        if (roomController == null || string.IsNullOrEmpty(friendUID)) return;

        roomController.friendUID = friendUID;
        roomController.LoadFriendFurniture();
        roomController.OpenFriendRoomPanel();
    }
}
