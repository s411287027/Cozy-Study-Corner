using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class StickyNoteDatabaseController : MonoBehaviour
{
    public static StickyNoteDatabaseController Instance;

    private DatabaseReference dbRef;

    private void Awake()
    {
        // 確保 Singleton 只初始化一次
        if (Instance == null)
        {
            Instance = this;
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            DontDestroyOnLoad(gameObject); // 保證在場景切換後不會被刪除
        }
        else
        {
            Destroy(gameObject); // 若已經有實例則摧毀當前物件
        }
    }

    public void SaveStickyNote(StickyNote stickyNote)
    {
        // 儲存便利貼到 Firebase
        string path = $"stickyNotes/{stickyNote.senderUid}";
        string key = dbRef.Child(path).Push().Key;

        Dictionary<string, object> stickyNoteData = new Dictionary<string, object>
        {
            { "senderUid", stickyNote.senderUid },
            { "message", stickyNote.message },
            { "timestamp", stickyNote.timestamp }
        };

        dbRef.Child(path).Child(key).SetValueAsync(stickyNoteData).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Sticky Note saved successfully.");
            }
            else
            {
                Debug.LogError("Error saving sticky note: " + task.Exception);
            }
        });
    }
}
