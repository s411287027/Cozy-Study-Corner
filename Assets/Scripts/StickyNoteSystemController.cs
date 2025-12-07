using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System;

public class StickyNoteSystemController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject stickyNotePanel;
    public TMP_InputField messageInputField;
    public Button sendButton;
    public Button closeButton;

    public GameObject receivedStickyNotesPanel;
    public Transform receivedStickyNotesContainer;
    public GameObject stickyNoteItemPrefab;
    public TMP_Text receivedStickyNotesText;

    // 收到的便利貼
    private List<StickyNote> receivedStickyNotes = new List<StickyNote>();

    // Firebase Database 參考
    private DatabaseReference dbRef;
    private FirebaseDatabaseController dbController;

    private void Awake()
    {
        // 初始化 Firebase 資料庫參考
        dbController = FirebaseDatabaseController.Instance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // 顯示便利貼面板
    public void ShowStickyNotePanel()
    {
        stickyNotePanel.SetActive(true);
    }

    // 隱藏便利貼面板
    public void HideStickyNotePanel()
    {
        stickyNotePanel.SetActive(false);
    }

    // 顯示收到的便利貼面板
    public void ShowReceivedStickyNotesPanel()
    {
        // 先隱藏面板，然後再顯示
        receivedStickyNotesPanel.SetActive(false);
        receivedStickyNotesPanel.SetActive(true);
        
        // 加載收到的便利貼
        LoadReceivedStickyNotes();
    }

    // 隱藏收到的便利貼面板
    public void HideReceivedStickyNotesPanel()
    {
        receivedStickyNotesPanel.SetActive(false);
    }

    // 發送便利貼
    public void SendStickyNote()
    {
        string message = messageInputField.text.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            // 建立新的便利貼物件
            StickyNote newStickyNote = new StickyNote
            {
                senderUid = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId,
                message = message,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 傳送到資料庫
            StickyNoteDatabaseController.Instance.SaveStickyNote(newStickyNote);

            // 清空輸入框
            messageInputField.text = "";
        }
    }

    // 加載收到的便利貼
    public void LoadReceivedStickyNotes()
    {
        // 清空先前顯示的便利貼
        foreach (Transform child in receivedStickyNotesContainer)
        {
            Destroy(child.gameObject);  // 刪除所有子物件
        }

        // 從 Firebase 加載新的便利貼
        dbRef.Child("stickyNotes").Child(dbController.userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Result.Exists)
                {
                    foreach (var stickyNote in task.Result.Children)
                    {
                        string senderUid = stickyNote.Child("senderUid").Value.ToString();
                        string message = stickyNote.Child("message").Value.ToString();
                        string timestamp = stickyNote.Child("timestamp").Value.ToString();

                        // 創建並顯示收到的便利貼項目
                        CreateReceivedStickyNoteItem(senderUid, message, timestamp);
                    }
                }
                else
                {
                    receivedStickyNotesText.text = "No sticky notes found."; // 如果沒有找到資料
                }
            }
            else
            {
                Debug.LogError("Error loading sticky notes: " + task.Exception);
            }
        });
    }

    // 創建顯示收到的便利貼項目
    private void CreateReceivedStickyNoteItem(string senderUid, string message, string timestamp)
    {
        // 確保有物件可用
        GameObject item = Instantiate(stickyNoteItemPrefab, receivedStickyNotesContainer);
        TMP_Text senderText = item.transform.Find("SenderText")?.GetComponent<TMP_Text>();
        TMP_Text messageText = item.transform.Find("MessageText")?.GetComponent<TMP_Text>();
        TMP_Text timestampText = item.transform.Find("TimestampText")?.GetComponent<TMP_Text>();

        if (senderText != null)
        {
            senderText.text = "From: " + senderUid;
        }
        else
        {
            Debug.LogError("SenderText component not found in stickyNoteItemPrefab.");
        }

        if (messageText != null)
        {
            messageText.text = message;
        }
        else
        {
            Debug.LogError("MessageText component not found in stickyNoteItemPrefab.");
        }

        if (timestampText != null)
        {
            timestampText.text = timestamp;
        }
        else
        {
            Debug.LogError("TimestampText component not found in stickyNoteItemPrefab.");
        }
    }

    private void OnEnable()
    {
        sendButton.onClick.AddListener(SendStickyNote);
        closeButton.onClick.AddListener(HideStickyNotePanel);
    }

    private void OnDisable()
    {
        sendButton.onClick.RemoveListener(SendStickyNote);
        closeButton.onClick.RemoveListener(HideStickyNotePanel);
    }
}

// 便利貼資料模型
public class StickyNote
{
    public string senderUid;
    public string message;
    public string timestamp;
}
