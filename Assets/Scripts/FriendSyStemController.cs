using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
    [Header("Friend Info UI")]
    public GameObject friendInfoPanel;
    public TMP_Text infoNameText;
    public TMP_Text infoReservationText;
    public TMP_Text infoMessageText;
    public TMP_InputField messageInput;
    public Transform messageContent;
    [Header("Chat UI Prefabs")]
    public GameObject messageItemPrefab;
    public GameObject myMessageItemPrefab;

    void Awake()
    {
        // 移除單例模式 (Singleton) 的強制保留邏輯
        // 因為 UI 元素 (FriendSystemPanel) 會隨場景銷毀，
        // 所以這個控制器也應該隨場景銷毀並重新建立。

        Instance = this;

        // 確保剛開始時面板是關閉的 (視需求而定)
        if (FriendSystemPanel != null)
            FriendSystemPanel.SetActive(false);
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

        FriendListItem listItem = item.GetComponent<FriendListItem>();
        listItem.friendUID = friendUid;

        TMP_Text friendNameText = item.transform.Find("FriendNameText").GetComponent<TMP_Text>();
        TMP_Text uidText = item.transform.Find("UIDText").GetComponent<TMP_Text>();

        friendNameText.text = "Loading...";
        uidText.text = friendUid;

        // 查詢好友名稱
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
                    statusText.color = (status == "Online") ? Color.green : Color.gray;
                }
                else
                {
                    statusText.text = "Unknown";
                    statusText.color = Color.gray;
                }
            });

        // ⭐⭐ Info Button 設定 ⭐⭐
        Button infoButton = item.transform.Find("InfoButton").GetComponent<Button>();
        infoButton.onClick.RemoveAllListeners();
        infoButton.onClick.AddListener(() =>
        {
            LoadFriendInfo(friendUid);
        });
    }
    private string currentChatFriendUid;

    public void LoadFriendInfo(string friendUid)
    {
        friendInfoPanel.SetActive(true);
        currentChatFriendUid = friendUid;

        infoNameText.text = "Loading...";
        infoReservationText.text = "Loading...";
        infoMessageText.text = "";

        string myUid = dbController.userId;
        string roomId = GetMessageRoomId(myUid, friendUid);

        // 讀取好友基本資料
        dbRef.Child("users").Child(friendUid)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted || !task.Result.Exists)
                {
                    infoNameText.text = "Unknown User";
                    infoReservationText.text = "No Data";
                    infoMessageText.text = "No Message";
                    return;
                }

                DataSnapshot snapshot = task.Result;

                string name = snapshot.Child("UserName").Exists ?
                    snapshot.Child("UserName").Value.ToString() : friendUid;
                string reservation = snapshot.Child("TomorrowReservationTime").Exists ?
                    snapshot.Child("TomorrowReservationTime").Value.ToString() : "No Reservation";
                string message = snapshot.Child("Message").Exists ?
                    snapshot.Child("Message").Value.ToString() : "No Message";

                infoNameText.text = name;
                infoReservationText.text = reservation;
                infoMessageText.text = message;
            });
        LoadPrivateMessages(roomId, myUid);
    }

    public void LoadPrivateMessages(string roomId, string myUid)
    {
        // 1. ✅ 使用 DestroyImmediate 確保舊訊息立刻被刪除
        int childCount = messageContent.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(messageContent.GetChild(i).gameObject);
        }

        infoMessageText.text = "Loading...";

        // 2. 限制載入數量 (例如只載入最後 50 筆)，避免訊息太多卡頓
        dbRef.Child("private_messages")
             .Child(roomId)
             .Child("messages")
             .LimitToLast(50) // ⭐ 建議加上這個
             .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || !task.Result.Exists)
            {
                infoMessageText.text = "No Messages";
                return;
            }

            infoMessageText.text = ""; // 清空狀態文字

            foreach (var msg in task.Result.Children)
            {
                // 加上安全檢查，避免資料缺漏報錯
                if (!msg.Child("from").Exists || !msg.Child("text").Exists) continue;

                string from = msg.Child("from").Value.ToString();
                string text = msg.Child("text").Value.ToString();

                GameObject prefabToUse;

                if (from == myUid)
                {
                    // 如果是我傳的，用「我的 Prefab」(靠右)
                    prefabToUse = myMessageItemPrefab;
                }
                else
                {
                    // 如果是對方傳的，用「原本 Prefab」(靠左)
                    prefabToUse = messageItemPrefab;
                }

                // 生成物件
                GameObject item = Instantiate(prefabToUse, messageContent);

                // 設定文字 (現代聊天軟體通常不需要顯示 "You:" 或 "Friend:"，靠左右區分即可)
                TMP_Text t = item.GetComponentInChildren<TMP_Text>();
                t.text = text;
            }

            // 3. ✅ 強制滾動到最底部 (顯示最新訊息)
            // 需要等待一幀讓 UI Layout 重建完成
            StartCoroutine(ScrollToBottom());
        });
    }

    // 輔助函式：滾動到底部
    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame(); // 等待 UI 生成完畢

        // 假設 messageContent 的 parent 是 ScrollRect 的 Content
        // 你需要找到 ScrollRect 組件
        ScrollRect sr = messageContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            sr.verticalNormalizedPosition = 0f; // 0 代表最底部，1 代表最頂部
        }
    }

    private string GetMessageRoomId(string uid1, string uid2)
    {
        // 排序兩個 UID，避免「A_B」與「B_A」重複
        return string.Compare(uid1, uid2) < 0 ? uid1 + "_" + uid2 : uid2 + "_" + uid1;
    }
    public void SendPrivateMessage()
    {
        if (dbRef == null)
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        string msg = messageInput.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        if (string.IsNullOrEmpty(currentChatFriendUid))
        {
            Debug.LogError("❌ currentChatFriendUid is NULL!");
            return;
        }

        string myUid = dbController.userId;
        string roomId = GetMessageRoomId(myUid, currentChatFriendUid);

        DatabaseReference msgRef = dbRef.Child("private_messages")
                                        .Child(roomId)
                                        .Child("messages")
                                        .Push();

        // ⭐⭐⭐ 修改重點：改用 Dictionary <string, object> ⭐⭐⭐
        Dictionary<string, object> msgData = new Dictionary<string, object>
        {
            { "from", myUid },
            { "to", currentChatFriendUid },
            { "text", msg },
            { "time", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") }
        };

        // 這裡加上了上一則回應提到的「錯誤判斷修正」，請一併使用
        msgRef.SetValueAsync(msgData).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                // 這裡會抓到連線錯誤或權限錯誤
                Debug.LogError("❌ 寫入失敗: " + task.Exception);
            }
            else if (task.IsCanceled)
            {
                Debug.LogError("❌ 寫入被取消");
            }
            else if (task.IsCompleted)
            {
                Debug.Log("✔ Message sent!");
                messageInput.text = "";
                LoadPrivateMessages(roomId, myUid);
            }
        });
    }


    public void CloseFriendInfoPanel()
    {
        friendInfoPanel.SetActive(false);
    }

    private void ClearFriendListUI()
    {
        // 必須從後往前刪除，且使用 DestroyImmediate 以確保在生成新列表前，舊的已經完全消失
        int childCount = friendListContainer.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = friendListContainer.GetChild(i).gameObject;
            // 使用 DestroyImmediate 強制當下刪除，避免擋住後面的重複檢查邏輯
            DestroyImmediate(child);
        }
    }

    private void ClearRequestListUI()
    {
        if (requestListContainer == null) return;

        // 改用 DestroyImmediate
        int childCount = requestListContainer.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = requestListContainer.GetChild(i);
            if (child != null)
                DestroyImmediate(child.gameObject);
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
