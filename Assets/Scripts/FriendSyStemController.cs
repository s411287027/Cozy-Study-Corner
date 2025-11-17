using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FriendSystemController : MonoBehaviour
{
    public FirebaseDatabaseController dbController;
    private DatabaseReference dbRef;
    private DatabaseReference friendRequestRef;
    public GameObject FriendSystemPanel;

    [Header("Player Info UI")]
    public TMP_Text playerUIDText;

    [Header("UI References")]
    public TMP_InputField searchInput;
    public TMP_Text resultText;
    public TMP_Text requestListText;

    [Header("Search UI")]
    public GameObject addFriendButtonPrefab;
    public Transform searchResultsContainer;

    [Header("Request List UI")]
    public GameObject friendRequestItemPrefab;
    public Transform requestListContainer;

    [Header("Friend List UI")]
    public GameObject friendListItemPrefab; // Prefab 用於顯示好友
    public Transform friendListContainer; // 容器
    public static FriendSystemController Instance;
    private bool isListeningFriendRequests = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // 永遠保留
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnEnable()
    {
        if (FirebaseDatabaseController.Instance == null)
        {
            Debug.LogWarning("FirebaseDatabaseController 尚未初始化，延遲 1 秒再嘗試...");
            Invoke(nameof(OnEnable), 1f);
            return;
        }

        dbController = FirebaseDatabaseController.Instance;

        dbController.OnDataLoaded -= OnDataLoaded;
        dbController.OnDataLoaded += OnDataLoaded;

        if (dbController.dts != null)
            OnDataLoaded();
        else
            dbController.LoadDataFn();
    }


    private void OnDataLoaded()
    {
        // 確保 dbRef 初始化
        if (dbRef == null)
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 開始監聽好友邀請
        StartListeningForFriendRequests();

        // 載入好友
        LoadFriends();
    }


    public void SearchUser()
    {
        string idToSearch = searchInput.text.Trim();
        if (string.IsNullOrEmpty(idToSearch))
        {
            resultText.text = "Please Input UserID";
            return;
        }
        if (idToSearch == dbController.userId)
        {
            resultText.text = "Cannot Find Yourself";
            ClearSearchResults();
            return;
        }

        dbRef.Child("users").Child(idToSearch)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string username = task.Result.Child("UserName").Value.ToString();
                    resultText.text = $"Find User: {username}\nUID: {idToSearch}";
                    CreateAddFriendButton(idToSearch, username);
                }
                else
                {
                    resultText.text = "Find Nobody";
                    ClearSearchResults();
                }
            });
    }

    private void CreateAddFriendButton(string targetUid, string username)
    {
        ClearSearchResults();

        GameObject button = Instantiate(addFriendButtonPrefab, searchResultsContainer);
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        buttonText.text = $"Add {username} As A Friend";

        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            SendFriendRequest(targetUid);
        });
    }

    private void ClearSearchResults()
    {
        foreach (Transform child in searchResultsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void SendFriendRequest(string targetUid)
    {
        if (targetUid == dbController.userId)
        {
            resultText.text = "Cannot send friend request to yourself.";
            return;
        }

        if (dbController.dts.Friends.Contains(targetUid))
        {
            resultText.text = "You are already friends.";
            return;
        }

        if (dbController.dts.FriendRequests.Sent.Contains(targetUid))
        {
            resultText.text = "Sent Invited Already.";
            return;
        }

        dbRef.Child("users").Child(dbController.userId)
            .Child("FriendRequests").Child("Sent").Push().SetValueAsync(targetUid);

        dbRef.Child("users").Child(targetUid)
            .Child("FriendRequests").Child("Received").Push().SetValueAsync(dbController.userId);

        resultText.text = "Sent Invited Already.";
    }

    public void AcceptFriendRequest(string fromUid)
    {
        dbRef.Child("users").Child(dbController.userId)
            .Child("Friends").Push().SetValueAsync(fromUid);
        dbRef.Child("users").Child(fromUid)
            .Child("Friends").Push().SetValueAsync(dbController.userId);

        RemoveRequest(fromUid, dbController.userId, "Received");
        RemoveRequest(dbController.userId, fromUid, "Sent");

        resultText.text = "Accept！";

        LoadFriends(); // 接受後刷新好友列表
    }

    public void DeclineFriendRequest(string fromUid)
    {
        RemoveRequest(fromUid, dbController.userId, "Received");
        RemoveRequest(dbController.userId, fromUid, "Sent");
        resultText.text = "Decline";
    }

    private void RemoveRequest(string targetUid, string ownerUid, string type)
    {
        dbRef.Child("users").Child(ownerUid).Child("FriendRequests").Child(type)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    foreach (var req in task.Result.Children)
                    {
                        if (req.Value.ToString() == targetUid)
                        {
                            dbRef.Child("users").Child(ownerUid)
                                .Child("FriendRequests").Child(type)
                                .Child(req.Key).RemoveValueAsync();
                        }
                    }
                }
            });
    }

    public void LoadFriends()
    {
        ClearFriendListUI();

        dbRef.Child("users").Child(dbController.userId).Child("Friends")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (!snapshot.Exists)
                    {
                        // 顯示「沒有好友」
                        GameObject noFriend = Instantiate(friendListItemPrefab, friendListContainer);
                        TMP_Text friendNameText = noFriend.transform.Find("FriendNameText").GetComponent<TMP_Text>();
                        friendNameText.text = "No Friends";
                        return;
                    }

                    foreach (var f in snapshot.Children)
                    {
                        string friendUid = f.Value.ToString();
                        if (friendUid != "init" && friendUid != dbController.userId)
                        {
                            CreateFriendListItem(friendUid);
                        }
                    }
                }
            });
    }

    private void CreateFriendListItem(string friendUid)
    {
        foreach (Transform child in friendListContainer)
        {
            TMP_Text uidText2 = child.Find("UIDText")?.GetComponent<TMP_Text>();
            if (uidText2 != null && uidText2.text == friendUid)
                return;
        }
        GameObject item = Instantiate(friendListItemPrefab, friendListContainer);

        // 設定 FriendListItem 腳本
        FriendListItem listItem = item.GetComponent<FriendListItem>();
        listItem.friendUID = friendUid;

        TMP_Text friendNameText = item.transform.Find("FriendNameText").GetComponent<TMP_Text>();
        TMP_Text uidText = item.transform.Find("UIDText").GetComponent<TMP_Text>();

        friendNameText.text = "Loading...";
        uidText.text = friendUid;

        dbRef.Child("users").Child(friendUid).Child("UserName")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string username = task.Result.Value.ToString();
                    friendNameText.text = username;
                }
                else
                {
                    friendNameText.text = "Unknown";
                }
            });
        // 查詢狀態
        TMP_Text statusText = item.transform.Find("StatusText").GetComponent<TMP_Text>();
        statusText.text = "Loading...";

        dbRef.Child("users").Child(friendUid).Child("Status")
            .GetValueAsync().ContinueWithOnMainThread(statusTask =>
            {
                if (statusTask.IsCompleted && statusTask.Result.Exists)
                {
                    string status = statusTask.Result.Value.ToString();
                    statusText.text = status;

                    if (status == "Online")
                        statusText.color = Color.green;
                    else
                        statusText.color = Color.gray;
                }
                else
                {
                    statusText.text = "Unknown";
                    statusText.color = Color.gray;
                }
            });
    }


    private void ClearFriendListUI()
    {
        foreach (Transform child in friendListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearRequestListUI()
    {
        foreach (Transform child in requestListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateFriendRequestItem(string fromUid)
    {
        GameObject item = Instantiate(friendRequestItemPrefab, requestListContainer);

        TMP_Text uidText = item.transform.Find("UIDText").GetComponent<TMP_Text>();
        Button acceptButton = item.transform.Find("AcceptButton").GetComponent<Button>();
        Button declineButton = item.transform.Find("DeclineButton").GetComponent<Button>();

        uidText.text = "Loading...";

        dbRef.Child("users").Child(fromUid).Child("UserName")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string username = task.Result.Value.ToString();
                    uidText.text = $"Friend Request from: {username}";
                }
                else
                {
                    uidText.text = $"Friend Request from: {fromUid}";
                }
            });

        acceptButton.onClick.AddListener(() =>
        {
            AcceptFriendRequest(fromUid);
        });

        declineButton.onClick.AddListener(() =>
        {
            DeclineFriendRequest(fromUid);
        });
    }

    private void OnFriendRequestChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("Firebase 監聽錯誤：" + e.DatabaseError.Message);
            return;
        }

        ClearRequestListUI();

        if (e.Snapshot == null || !e.Snapshot.Exists)
        {
            requestListText.text = "No Friend Request";
            return;
        }

        string list = "";
        foreach (var req in e.Snapshot.Children)
        {
            string fromUid = req.Value.ToString();
            if (fromUid != "init" && fromUid != dbController.userId)
            {
                list += fromUid + "\n";
                CreateFriendRequestItem(fromUid);
            }
        }

        requestListText.text = string.IsNullOrEmpty(list) ? "No Friend Request" : list;

        LoadFriends();
    }

    public void StartListeningForFriendRequests()
    {
        if (dbRef == null)
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        if (string.IsNullOrEmpty(dbController.userId))
        {
            Debug.LogWarning("UserID 尚未初始化，延遲啟動監聽...");
            Invoke(nameof(StartListeningForFriendRequests), 1.5f);
            return;
        }

        if (friendRequestRef != null)
        {
            friendRequestRef.ValueChanged -= OnFriendRequestChanged;
            friendRequestRef = null;
        }

        friendRequestRef = dbRef.Child("users")
            .Child(dbController.userId)
            .Child("FriendRequests")
            .Child("Received");

        friendRequestRef.ValueChanged += OnFriendRequestChanged;
        Debug.Log($"✅ 開始監聽好友邀請...（使用者: {dbController.userId}）");
    }

    void OnDestroy()
    {
        if (friendRequestRef != null)
        {
            friendRequestRef.ValueChanged -= OnFriendRequestChanged;
            Debug.Log("🛑 已移除好友邀請監聽。");
        }
    }

    public void OpenFriendSystemController()
    {
        // 初始化 dbRef
        if (dbRef == null)
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        if (FriendSystemPanel == null)
        {
            Debug.LogError("FriendSystemPanel 尚未指派！");
            return;
        }

        // 更新 Canvas sortingOrder
        Scene sceneA = SceneManager.GetSceneByName("CozyStudyCorner");
        foreach (var rootObj in sceneA.GetRootGameObjects())
        {
            Canvas canvas = rootObj.GetComponentInChildren<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 10; // 高於 SceneA
        }

        // 啟用面板
        FriendSystemPanel.SetActive(true);

        // 如果 dbController 尚未初始化，延遲顯示 UID 與載入好友
        if (dbController == null || string.IsNullOrEmpty(dbController.userId) || dbController.dts == null)
        {
            Debug.Log("Firebase 尚未初始化，延遲顯示 UID 與載入好友...");
            Invoke(nameof(DelayedLoadUI), 1f); // 1 秒後再嘗試
            return;
        }

        // 直接顯示 UID 與載入好友
        if (playerUIDText != null)
            playerUIDText.text = dbController.userId;

        LoadFriends();
    }

    // 延遲載入 UI
    private void DelayedLoadUI()
    {
        if (playerUIDText != null && dbController != null)
            playerUIDText.text = dbController.userId;

        if (dbController != null)
            LoadFriends();
    }



    public void CloseFriendSystemController()
    {
        FriendSystemPanel.SetActive(false);
    }
}
