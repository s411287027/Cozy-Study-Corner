using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Globalization;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;

public class StickyNoteSystemController : MonoBehaviour
{
    [Header("Send Panel")]
    public GameObject sendPanel;
    public TMP_InputField messageInput;
    public Button sendButton;
    public Button closeSendButton;

    [Header("Received Panel")]
    public GameObject receivedPanel;
    public Transform receivedContainer;
    public GameObject stickyNoteItemPrefab;
    public TMP_Text emptyHintText;
    public Button closeReceivedButton;

    [Header("Unread Badge (紅點)")]
    public GameObject badgeRoot;   // 紅點 Image（整顆）
    public TMP_Text badgeText;     // 紅點數字（可不接）

    private string currentTargetUid;
    private string currentSourceScene;

    // ====== realtime badge ======
    private DatabaseReference myNotesRef;
    private EventHandler<ChildChangedEventArgs> childAddedHandler;
    private bool listening = false;

    // 用來避免「初次掛監聽時把舊資料當新資料」
    private bool initialSyncDone = false;
    private HashSet<string> knownKeys = new HashSet<string>();
    private int unreadCount = 0;

    private void Awake()
    {
        if (sendPanel) sendPanel.SetActive(false);
        if (receivedPanel) receivedPanel.SetActive(false);

        if (badgeRoot) badgeRoot.SetActive(false);

        if (sendButton) sendButton.onClick.AddListener(OnClickSend);
        if (closeSendButton) closeSendButton.onClick.AddListener(() => { if (sendPanel) sendPanel.SetActive(false); });
        if (closeReceivedButton) closeReceivedButton.onClick.AddListener(() => { if (receivedPanel) receivedPanel.SetActive(false); });

        // ✅ 建議：啟動就開始監聽（有登入才會成功）
        StartListenNewNotes();
    }

    private void OnDestroy()
    {
        StopListenNewNotes();
    }

    // SeatClickArea 會呼叫這個
    public void OpenSendPanel(string targetUid, string sourceScene)
    {
        Debug.Log("✅ OpenSendPanel called, source = " + sourceScene);

        currentTargetUid = targetUid;
        currentSourceScene = sourceScene;

        if (messageInput) messageInput.text = "";
        if (sendPanel) sendPanel.SetActive(true);
    }

    private void OnClickSend()
    {
        if (string.IsNullOrEmpty(currentTargetUid)) return;
        if (!messageInput) return;

        string msg = messageInput.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        if (StickyNoteDatabaseController.Instance == null)
        {
            Debug.LogError("StickyNoteDatabaseController.Instance is null");
            return;
        }

        StickyNoteDatabaseController.Instance.SendStickyNote(currentTargetUid, msg, currentSourceScene);

        if (sendPanel) sendPanel.SetActive(false);
    }

    // View 按鈕接這個
    public void OpenReceivedPanel()
    {
        if (receivedPanel) receivedPanel.SetActive(true);
        LoadReceivedStickyNotes();

        // ✅ 看過就清紅點
        ClearUnread();
    }

    private void LoadReceivedStickyNotes()
    {
        if (receivedContainer == null || stickyNoteItemPrefab == null)
        {
            Debug.LogError("receivedContainer / stickyNoteItemPrefab 沒有指派");
            return;
        }

        // 清空舊 UI
        for (int i = receivedContainer.childCount - 1; i >= 0; i--)
            Destroy(receivedContainer.GetChild(i).gameObject);

        if (emptyHintText) emptyHintText.gameObject.SetActive(false);

        if (StickyNoteDatabaseController.Instance == null)
        {
            Debug.LogError("StickyNoteDatabaseController.Instance is null");
            return;
        }

        StickyNoteDatabaseController.Instance.LoadMyStickyNotes(notes =>
        {
            if (notes == null || notes.Count == 0)
            {
                if (emptyHintText)
                {
                    emptyHintText.text = "No sticky notes received.";
                    emptyHintText.gameObject.SetActive(true);
                }
                return;
            }

            // 最新在上、最舊在下
            notes.Sort((a, b) =>
            {
                DateTime ta = ParseTimeSafe(a.timestamp);
                DateTime tb = ParseTimeSafe(b.timestamp);
                return tb.CompareTo(ta);
            });

            foreach (var n in notes)
                CreateItem(n);
        });
    }

    private void CreateItem(StickyNote note)
    {
        GameObject item = Instantiate(stickyNoteItemPrefab, receivedContainer);
        var ui = item.GetComponent<StickyNoteItemUI>();
        if (ui == null)
        {
            Debug.LogError("stickyNoteItemPrefab 根物件上沒有 StickyNoteItemUI");
            return;
        }
        ui.Set(note);
    }

    private DateTime ParseTimeSafe(string s)
    {
        if (string.IsNullOrEmpty(s)) return DateTime.MinValue;

        if (DateTime.TryParseExact(s, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(s, out dt)) return dt;
        return DateTime.MinValue;
    }

    // =========================
    // 紅點監聽（重點：不把舊資料算新）
    // =========================
    public void StartListenNewNotes()
    {
        if (listening) return;
        if (StickyNoteDatabaseController.Instance == null) return;

        myNotesRef = StickyNoteDatabaseController.Instance.GetMyStickyNotesRef();
        if (myNotesRef == null)
        {
            // 可能還沒登入
            Debug.LogWarning("StartListenNewNotes: myNotesRef null (not logged in yet?)");
            return;
        }

        initialSyncDone = false;
        knownKeys.Clear();

        // 1) 先抓一次目前已有資料，這些不算新
        myNotesRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Initial sync failed.");
                return;
            }

            if (task.Result != null && task.Result.Exists)
            {
                foreach (var child in task.Result.Children)
                    knownKeys.Add(child.Key);
            }

            // 2) 再掛 ChildAdded
            childAddedHandler = (object sender, ChildChangedEventArgs args) =>
            {
                if (args.DatabaseError != null) return;
                if (args.Snapshot == null || !args.Snapshot.Exists) return;

                string key = args.Snapshot.Key;

                // 初始化重播期間：只記錄 key，不加未讀
                if (!initialSyncDone)
                {
                    knownKeys.Add(key);
                    return;
                }

                // 真正新來的
                if (knownKeys.Contains(key)) return;
                knownKeys.Add(key);

                // 如果正在看 receivedPanel，就不要增加紅點（你想要也可改）
                if (receivedPanel != null && receivedPanel.activeInHierarchy)
                    return;

                unreadCount++;
                UpdateBadge();
            };

            myNotesRef.ChildAdded += childAddedHandler;
            listening = true;

            // 3) 從此刻起才算新
            initialSyncDone = true;
        });
    }

    public void StopListenNewNotes()
    {
        if (!listening) return;

        if (myNotesRef != null && childAddedHandler != null)
            myNotesRef.ChildAdded -= childAddedHandler;

        listening = false;
        childAddedHandler = null;
        myNotesRef = null;
        initialSyncDone = false;
    }

    public void ClearUnread()
    {
        unreadCount = 0;
        UpdateBadge();
    }

    private void UpdateBadge()
    {
        if (badgeRoot) badgeRoot.SetActive(unreadCount > 0);
        if (badgeText) badgeText.text = unreadCount.ToString();
    }
}
