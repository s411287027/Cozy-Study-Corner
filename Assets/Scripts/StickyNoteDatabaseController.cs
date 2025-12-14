using UnityEngine;
using Firebase.Database;
using Firebase.Auth;
using System;
using System.Collections.Generic;
using Firebase.Extensions;

public class StickyNoteDatabaseController : MonoBehaviour
{
    public static StickyNoteDatabaseController Instance;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public string GetMyUid()
    {
        return auth.CurrentUser != null ? auth.CurrentUser.UserId : null;
    }

    // =========================
    // 傳送便利貼
    // =========================
    public void SendStickyNote(string targetUid, string message, string sourceScene)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ 尚未登入 Firebase");
            return;
        }

        string senderUid = auth.CurrentUser.UserId;

        string path = $"stickyNotes/{targetUid}";
        string key = dbRef.Child(path).Push().Key;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "senderUid", senderUid },
            { "message", message },
            { "timestamp", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
            { "sourceScene", sourceScene }
        };

        dbRef.Child(path).Child(key).SetValueAsync(data);
    }

    // =========================
    // 讀取我收到的便利貼
    // =========================
    public void LoadMyStickyNotes(Action<List<StickyNote>> onResult)
    {
        if (auth.CurrentUser == null)
        {
            onResult?.Invoke(new List<StickyNote>());
            return;
        }

        string myUid = auth.CurrentUser.UserId;

        dbRef.Child("stickyNotes").Child(myUid)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                List<StickyNote> result = new List<StickyNote>();

                if (task.IsCompleted && task.Result != null && task.Result.Exists)
                {
                    foreach (var child in task.Result.Children)
                    {
                        result.Add(new StickyNote
                        {
                            key        = child.Key,
                            senderUid   = child.Child("senderUid")?.Value?.ToString() ?? "",
                            message     = child.Child("message")?.Value?.ToString() ?? "",
                            timestamp   = child.Child("timestamp")?.Value?.ToString() ?? "",
                            sourceScene = child.Child("sourceScene")?.Value?.ToString() ?? ""
                        });
                    }
                }

                onResult?.Invoke(result);
            });
    }

    // =========================
    // 給紅點用：拿到我的 stickyNotes reference
    // =========================
    public DatabaseReference GetMyStickyNotesRef()
    {
        var uid = GetMyUid();
        if (string.IsNullOrEmpty(uid)) return null;
        return FirebaseDatabase.DefaultInstance.GetReference("stickyNotes").Child(uid);
    }
}
