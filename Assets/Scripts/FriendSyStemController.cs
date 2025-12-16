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
    public static FriendSystemController Instance;

    public FirebaseDatabaseController dbController;
    private DatabaseReference dbRef;
    private DatabaseReference friendRequestRef;

    [Header("Main Panels")]
    public GameObject FriendSystemPanel;

    [Header("Player Info UI")]
    public TMP_Text playerUIDText;

    [Header("UI References")]
    public TMP_InputField searchInput;
    public TMP_Text resultText;
    public TMP_Text requestListText;

    // ⭐ 新增：關閉按鈕引用 (解決按鈕失效問題)
    [Header("Main UI Buttons")]
    public Button closeSystemButton;

    [Header("Search UI")]
    public GameObject addFriendButtonPrefab;
    public Transform searchResultsContainer;

    [Header("Request List UI")]
    public GameObject friendRequestItemPrefab;
    public Transform requestListContainer;

    [Header("Friend List UI")]
    public GameObject friendListItemPrefab; // Prefab 用於顯示好友
    public Transform friendListContainer; // 容器

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

    // ⭐ 新增：本地好友緩存 (解決 JsonUtility 讀取 List 失敗的問題)
    private List<string> _localFriendCache = new List<string>();
    // 用來暫存「已寄出邀請」的 UID
    private List<string> _sentRequestCache = new List<string>();
    void Awake()
    {

        
        // ⭐ 單例模式 + DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            // 把掛載此腳本的物件 (連同它的 UI) 全部保留到下一關
            DontDestroyOnLoad(gameObject);
            // ⭐ 新增：場景切換事件訂閱
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // 如果從別的場景回來，發現已經有一個好友系統了，就把新的刪掉
            Destroy(gameObject);
            return;
        }

        if (FriendSystemPanel != null)
            FriendSystemPanel.SetActive(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ⭐ 只在特定場景才調整 Canvas
        if (scene.name != "SampleScene") return;

        Canvas myCanvas = GetComponent<Canvas>();
        if (myCanvas == null)
            myCanvas = GetComponentInChildren<Canvas>();
        if (myCanvas == null) return;

        Camera newCam = Camera.main; // 新場景主 Camera
        if (myCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            myCanvas.worldCamera = newCam;
        }

        // 調整 Camera 尺寸 (如果是 Orthographic)
        if (newCam != null && newCam.orthographic)
        {
            newCam.orthographicSize = 6f; // 這個只影響 MainScene
        }

        // World Space Canvas 調整 RectTransform
        if (myCanvas.renderMode == RenderMode.WorldSpace)
        {
            RectTransform rt = myCanvas.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(1920, 1080); // 只在 MainScene 生效
            }
        }
    }



    void Start()
    {
        // ⭐ 自動綁定關閉按鈕事件 (防止 Inspector 連結斷掉)
        if (closeSystemButton != null)
        {
            closeSystemButton.onClick.RemoveAllListeners();
            closeSystemButton.onClick.AddListener(CloseFriendSystemController);
        }
        else
        {
            Debug.LogError("❌ 請在 Inspector 中將右上角的 'X 按鈕' 拖曳到 closeSystemButton 欄位！");
        }
    }

    private void OnEnable()
    {
        // 嘗試訂閱事件，但不依賴它來做初始化顯示
        if (FirebaseDatabaseController.Instance != null)
        {
            dbController = FirebaseDatabaseController.Instance;
            dbController.OnDataLoaded -= OnDataLoaded;
            dbController.OnDataLoaded += OnDataLoaded;

            // 如果剛好資料已經有了，就載入一次
            if (dbController.dts != null)
            {
                // 注意：這裡不強制呼叫 LoadFriends，交給 OpenFriendSystemController 處理
                // 避免重複呼叫
            }
        }
    }

    private void OnDisable()
    {
        if (dbController != null)
        {
            dbController.OnDataLoaded -= OnDataLoaded;
        }
    }

    private void OnDataLoaded()
    {
        if (dbRef == null)
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        StartListeningForFriendRequests();
        LoadFriends();
        LoadSentRequests();
    }

    // ==========================================
    // ⭐ 搜尋與加好友邏輯 (已修正重複添加問題)
    // ==========================================
    public void SearchUser()
    {
        string idToSearch = searchInput.text.Trim();

        if (string.IsNullOrEmpty(idToSearch))
        {
            resultText.text = "Please Input UserID";
            return;
        }

        // 檢查是否為自己
        if (dbController != null && idToSearch == dbController.userId)
        {
            resultText.text = "Cannot Find Yourself";
            ClearSearchResults();
            return;
        }

        // ⭐ 檢查緩存：是否已經是好友
        if (_localFriendCache.Contains(idToSearch))
        {
            resultText.text = "User is already your friend.";
            ClearSearchResults();
            return;
        }

        if (dbRef == null) dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        dbRef.Child("users").Child(idToSearch)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string username = task.Result.Child("UserName").Value.ToString();
                    resultText.text = $"Find User: {username}\nUID: {idToSearch}";

                    // ⭐ 再次檢查緩存 (防止非同步時間差)
                    if (_localFriendCache.Contains(idToSearch))
                    {
                        resultText.text = $"User {username} is already your friend.";
                        ClearSearchResults();
                    }
                    else
                    {
                        CreateAddFriendButton(idToSearch, username);
                    }
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
        if (dbController == null) return;

        if (targetUid == dbController.userId)
        {
            resultText.text = "Cannot send friend request to yourself.";
            return;
        }

        // ⭐ 檢查緩存：是否已經是好友
        if (_localFriendCache.Contains(targetUid))
        {
            resultText.text = "You are already friends.";
            return;
        }

        // 檢查是否已經送過邀請 (這裡可以用 dts 檢查，因為 Sent 列表通常比較單純)
        if (dbController.dts != null &&
            dbController.dts.FriendRequests != null &&
            dbController.dts.FriendRequests.Sent != null &&
            dbController.dts.FriendRequests.Sent.Contains(targetUid))
        {
            resultText.text = "Sent Invited Already.";
            return;
        }

        // 寫入資料庫
        dbRef.Child("users").Child(dbController.userId)
            .Child("FriendRequests").Child("Sent").Push().SetValueAsync(targetUid);

        dbRef.Child("users").Child(targetUid)
            .Child("FriendRequests").Child("Received").Push().SetValueAsync(dbController.userId);
        if (!_sentRequestCache.Contains(targetUid))
        {
            _sentRequestCache.Add(targetUid);
        }
        resultText.text = "Success send invite.";
    }

    public bool CheckIsFriendOrRequested(string targetUid)
    {
        // 1. 檢查是否是自己
        if (dbController != null && targetUid == dbController.userId) return true;

        // 2. 檢查本地好友緩存
        if (_localFriendCache != null && _localFriendCache.Contains(targetUid)) return true;

        // 3. ⭐ 檢查我們手動載入的 Sent 名單 (這才是準確的)
        if (_sentRequestCache != null && _sentRequestCache.Contains(targetUid))
        {
            return true;
        }

        return false;
    }

    // 專門用來讀取 Sent 資料夾，解決 dts 讀不到的問題
    private void LoadSentRequests()
    {
        if (dbController == null || string.IsNullOrEmpty(dbController.userId)) return;

        string path = $"users/{dbController.userId}/FriendRequests/Sent";

        // 直接從 Firebase 抓取資料
        if (dbRef == null) dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        dbRef.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                _sentRequestCache.Clear(); // 清空舊資料
                foreach (var child in task.Result.Children)
                {
                    // 這裡的 child.Value 就是目標 UID
                    string uid = child.Value.ToString();
                    if (uid != "init" && !_sentRequestCache.Contains(uid))
                    {
                        _sentRequestCache.Add(uid);
                    }
                }
            }
        });
    }

    // ==========================================
    // ⭐ 載入好友邏輯 (已修正 UI 消失與緩存問題)
    // ==========================================
    public void LoadFriends()
    {
        ClearFriendListUI();

        // ⭐ 1. 清空緩存，重新填入
        _localFriendCache.Clear();

        if (dbController == null || string.IsNullOrEmpty(dbController.userId)) return;
        if (dbRef == null) dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        Debug.Log("開始載入好友列表...");

        dbRef.Child("users").Child(dbController.userId).Child("Friends")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (!snapshot.Exists)
                    {
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
                            // ⭐ 2. 將確認存在的 UID 加入緩存
                            if (!_localFriendCache.Contains(friendUid))
                            {
                                _localFriendCache.Add(friendUid);
                            }

                            CreateFriendListItem(friendUid);
                        }
                    }

                    // ⭐ 3. 強制刷新 UI，解決第二次打開變隱形的問題
                    StartCoroutine(ForceRebuildLayout());
                }
            });
    }

    private void CreateFriendListItem(string friendUid)
    {
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
                    friendNameText.text = task.Result.Value.ToString();
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

        // Info Button 設定
        Button infoButton = item.transform.Find("InfoButton").GetComponent<Button>();
        infoButton.onClick.RemoveAllListeners();
        infoButton.onClick.AddListener(() =>
        {
            LoadFriendInfo(friendUid);
        });
    }

    // 強制刷新 UI 協程
    private System.Collections.IEnumerator ForceRebuildLayout()
    {
        yield return new WaitForEndOfFrame();
        if (friendListContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(friendListContainer.GetComponent<RectTransform>());
        }
    }

    // ==========================================
    // ⭐ 面板控制邏輯 (已修正初始化問題)
    // ==========================================
    public void OpenFriendSystemController()
    {
        // 確保 dbRef 存在
        if (dbRef == null) dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 1. 強制將 Canvas 拉到最上層 (防止被場景物件遮擋)
        Canvas myCanvas = GetComponent<Canvas>();
        if (myCanvas == null) myCanvas = GetComponentInParent<Canvas>();
        if (myCanvas != null)
        {
            myCanvas.sortingOrder = 999;
            // 確保 Render Mode 是 Overlay 效果最好
        }

        // 2. 啟動面板
        FriendSystemPanel.SetActive(true);

        // 3. 強制更新 DB Controller 引用 (因為場景切換後舊的可能失效)
        if (FirebaseDatabaseController.Instance != null)
        {
            dbController = FirebaseDatabaseController.Instance;
        }

        // 4. 強制更新 UID 顯示與載入好友
        if (dbController != null)
        {
            // 如果 Controller 的 ID 是空的，嘗試從 Auth 抓
            if (string.IsNullOrEmpty(dbController.userId))
            {
                if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
                    dbController.userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            }

            if (playerUIDText != null)
                playerUIDText.text = dbController.userId;

            // 5. 重新載入好友 (確保看到最新資料 + 填入緩存)
            LoadFriends();
        }
        else
        {
            Debug.LogError("FirebaseDatabaseController 遺失！");
        }
    }

    public void CloseFriendSystemController()
    {
        FriendSystemPanel.SetActive(false);
    }

    // ==========================================
    // 其他不需修改的功能 (邀請通知、接受、拒絕、聊天)
    // ==========================================

    public void AcceptFriendRequest(string fromUid)
    {
        dbRef.Child("users").Child(dbController.userId).Child("Friends").Push().SetValueAsync(fromUid);
        dbRef.Child("users").Child(fromUid).Child("Friends").Push().SetValueAsync(dbController.userId);

        RemoveRequest(fromUid, dbController.userId, "Received");
        RemoveRequest(dbController.userId, fromUid, "Sent");

        resultText.text = "Accept";
        LoadFriends();
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

    private void ClearFriendListUI()
    {
        foreach (Transform child in friendListContainer) Destroy(child.gameObject);
    }
    private void ClearRequestListUI()
    {
        if (requestListContainer != null)
            foreach (Transform child in requestListContainer) Destroy(child.gameObject);
    }

    private void CreateFriendRequestItem(string fromUid)
    {
        GameObject item = Instantiate(friendRequestItemPrefab, requestListContainer);
        TMP_Text uidText = item.transform.Find("UIDText").GetComponent<TMP_Text>();
        Button acceptButton = item.transform.Find("AcceptButton").GetComponent<Button>();
        Button declineButton = item.transform.Find("DeclineButton").GetComponent<Button>();

        uidText.text = "Loading...";
        dbRef.Child("users").Child(fromUid).Child("UserName").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
                uidText.text = $"{task.Result.Value} sent you a friend request.";
            else
                uidText.text = $"{fromUid} sent you a friend request.";
        });

        acceptButton.onClick.AddListener(() => AcceptFriendRequest(fromUid));
        declineButton.onClick.AddListener(() => DeclineFriendRequest(fromUid));
    }

    private void OnFriendRequestChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null) return;
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
        // 如果有新邀請，通常也可以順便刷新好友列表 (視需求而定)
    }

    public void StartListeningForFriendRequests()
    {
        if (dbRef == null) dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        if (string.IsNullOrEmpty(dbController.userId))
        {
            Invoke(nameof(StartListeningForFriendRequests), 1.5f);
            return;
        }

        if (friendRequestRef != null)
        {
            friendRequestRef.ValueChanged -= OnFriendRequestChanged;
            friendRequestRef = null;
        }

        friendRequestRef = dbRef.Child("users").Child(dbController.userId).Child("FriendRequests").Child("Received");
        friendRequestRef.ValueChanged += OnFriendRequestChanged;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (friendRequestRef != null)
            friendRequestRef.ValueChanged -= OnFriendRequestChanged;
    }

    // ==========================================
    // 聊天與好友詳情 (保持原樣)
    // ==========================================
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

        dbRef.Child("users").Child(friendUid).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || !task.Result.Exists)
            {
                infoNameText.text = "Unknown";
                return;
            }
            DataSnapshot snapshot = task.Result;
            infoNameText.text = snapshot.Child("UserName").Exists ? snapshot.Child("UserName").Value.ToString() : friendUid;
            infoReservationText.text = snapshot.Child("TomorrowReservationTime").Exists ? snapshot.Child("TomorrowReservationTime").Value.ToString() : "No Reservation";
            infoMessageText.text = snapshot.Child("Message").Exists ? snapshot.Child("Message").Value.ToString() : "No Message";
        });
        LoadPrivateMessages(roomId, myUid);
    }

    public void LoadPrivateMessages(string roomId, string myUid)
    {
        int childCount = messageContent.childCount;
        for (int i = childCount - 1; i >= 0; i--) DestroyImmediate(messageContent.GetChild(i).gameObject);

        infoMessageText.text = "Loading...";
        dbRef.Child("private_messages").Child(roomId).Child("messages").LimitToLast(50).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || !task.Result.Exists) { infoMessageText.text = "No Messages"; return; }
            infoMessageText.text = "";

            foreach (var msg in task.Result.Children)
            {
                if (!msg.Child("from").Exists || !msg.Child("text").Exists) continue;
                string from = msg.Child("from").Value.ToString();
                string text = msg.Child("text").Value.ToString();
                GameObject prefab = (from == myUid) ? myMessageItemPrefab : messageItemPrefab;
                GameObject item = Instantiate(prefab, messageContent);
                item.GetComponentInChildren<TMP_Text>().text = text;
            }
            StartCoroutine(ScrollToBottom());
        });
    }

    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        ScrollRect sr = messageContent.GetComponentInParent<ScrollRect>();
        if (sr != null) sr.verticalNormalizedPosition = 0f;
    }

    private string GetMessageRoomId(string uid1, string uid2)
    {
        return string.Compare(uid1, uid2) < 0 ? uid1 + "_" + uid2 : uid2 + "_" + uid1;
    }

    public void SendPrivateMessage()
    {
        if (dbRef == null) dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        string msg = messageInput.text.Trim();
        if (string.IsNullOrEmpty(msg) || string.IsNullOrEmpty(currentChatFriendUid)) return;

        string myUid = dbController.userId;
        string roomId = GetMessageRoomId(myUid, currentChatFriendUid);

        Dictionary<string, object> msgData = new Dictionary<string, object>
        {
            { "from", myUid },
            { "to", currentChatFriendUid },
            { "text", msg },
            { "time", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") }
        };

        dbRef.Child("private_messages").Child(roomId).Child("messages").Push().SetValueAsync(msgData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                messageInput.text = "";
                LoadPrivateMessages(roomId, myUid);
            }
        });
    }

    public void CloseFriendInfoPanel()
    {
        friendInfoPanel.SetActive(false);
    }
}