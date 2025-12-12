using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Globalization;

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
    public TMP_Text emptyHintText;          // 沒有收到時顯示（可不接）
    public Button closeReceivedButton;

    private string currentTargetUid;

    private void Awake()
    {
        if (sendPanel) sendPanel.SetActive(false);
        if (receivedPanel) receivedPanel.SetActive(false);

        if (sendButton) sendButton.onClick.AddListener(OnClickSend);
        if (closeSendButton) closeSendButton.onClick.AddListener(() => { if (sendPanel) sendPanel.SetActive(false); });
        if (closeReceivedButton) closeReceivedButton.onClick.AddListener(() => { if (receivedPanel) receivedPanel.SetActive(false); });
    }

    public void OpenSendPanel(string targetUid)
    {
        currentTargetUid = targetUid;
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

        StickyNoteDatabaseController.Instance.SendStickyNote(currentTargetUid, msg);

        if (sendPanel) sendPanel.SetActive(false);
    }

    // View 按鈕接這個
    public void OpenReceivedPanel()
    {
        if (receivedPanel) receivedPanel.SetActive(true);
        LoadReceivedStickyNotes();
    }

    private void LoadReceivedStickyNotes()
    {
        if (receivedContainer == null || stickyNoteItemPrefab == null)
        {
            Debug.LogError("receivedContainer / stickyNoteItemPrefab 沒有指派");
            return;
        }

        // 1) 清空舊 UI，避免重疊
        for (int i = receivedContainer.childCount - 1; i >= 0; i--)
            Destroy(receivedContainer.GetChild(i).gameObject);

        if (emptyHintText) emptyHintText.gameObject.SetActive(false);

        if (StickyNoteDatabaseController.Instance == null)
        {
            Debug.LogError("StickyNoteDatabaseController.Instance is null");
            return;
        }

        // 2) 從 Firebase 拉資料
        StickyNoteDatabaseController.Instance.LoadMyStickyNotes(notes =>
        {
            // notes 可能為空
            if (notes == null || notes.Count == 0)
            {
                if (emptyHintText)
                {
                    emptyHintText.text = "No sticky notes received.";
                    emptyHintText.gameObject.SetActive(true);
                }
                return;
            }

            // 3) 排序：你要「越早越下面」= 最新在上、最舊在下 → DESC
            notes.Sort((a, b) =>
            {
                DateTime ta = ParseTimeSafe(a.timestamp);
                DateTime tb = ParseTimeSafe(b.timestamp);
                return tb.CompareTo(ta); // DESC
            });

            // 4) 生成每一張便利貼
            foreach (var n in notes)
                CreateItem(n);
        });
    }

    private void CreateItem(StickyNote note)
    {
        GameObject item = Instantiate(stickyNoteItemPrefab, receivedContainer);

        // 用 Prefab 上的 StickyNoteItemUI（最穩）
        var ui = item.GetComponent<StickyNoteItemUI>();
        if (ui == null)
        {
            Debug.LogError("stickyNoteItemPrefab 根物件上沒有 StickyNoteItemUI，請加上並拖好三個 TMP_Text");
            return;
        }

        string sender = string.IsNullOrEmpty(note.senderUid) ? "(unknown)" : note.senderUid;
        string msg    = string.IsNullOrEmpty(note.message)   ? "" : note.message;
        string time   = string.IsNullOrEmpty(note.timestamp) ? "" : note.timestamp;

        ui.Set(sender, msg, time);
    }

    private DateTime ParseTimeSafe(string s)
    {
        if (string.IsNullOrEmpty(s)) return DateTime.MinValue;

        // 你存的是 "yyyy/MM/dd HH:mm:ss"
        if (DateTime.TryParseExact(s, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt))
            return dt;

        // 退一步用一般 Parse
        if (DateTime.TryParse(s, out dt)) return dt;

        return DateTime.MinValue;
    }
}
