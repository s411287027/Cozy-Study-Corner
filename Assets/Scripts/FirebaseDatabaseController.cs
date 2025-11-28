using UnityEngine;
using System;
using Firebase.Database;
using System.Collections;
using Firebase.Extensions;
using TMPro;
using System.Collections.Generic;

[Serializable]
public class EquipData
{
    public int hair;
    public int pants;
    public int shoes;
    public int face;
    public int shirt;
}

[Serializable]
public class OwnedItems
{
    public List<int> hair = new List<int>();
    public List<int> pants = new List<int>();
    public List<int> shoes = new List<int>();
    public List<int> face = new List<int>();
    public List<int> shirt = new List<int>();
    public List<int> furniture = new List<int>();

    public List<int> GetList(string itemType)
    {
        return itemType switch
        {
            "hair" => hair,
            "pants" => pants,
            "shoes" => shoes,
            "face" => face,
            "shirt" => shirt,
            "furniture" => furniture,
            _ => null
        };
    }
}

[Serializable]
public class DataToSave
{
    public string UserName;
    public int TotalCoins;
    public int CrrLevel;
    public string TomorrowReservationTime;
    public string Message;
    public string StudyAtHome;
    public EquipData currentEquip = new EquipData();
    public OwnedItems ownedItems = new OwnedItems();

    public List<string> Friends = new List<string>(); // 好友 UID
    public FriendRequests FriendRequests = new FriendRequests();
}

[System.Serializable]
public class FriendRequests
{
    public List<string> Sent = new List<string>();     // 送出的好友邀請
    public List<string> Received = new List<string>(); // 收到的好友邀請
}

public class FirebaseDatabaseController : MonoBehaviour
{
    public static FirebaseDatabaseController Instance;
    public DataToSave dts;
    public string userId;
    DatabaseReference dbRef;
    private bool dataUpdated = false;
    public event Action OnDataLoaded;

    public TMP_Text profileUserLevel_Text, profileUserCoins_Text;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void SaveDataFn()
    {
        string json = JsonUtility.ToJson(dts);
        /*var userData = new Dictionary<string, object>
        {
            {"UserName", dts.UserName},
            {"TotalCoins", dts.TotalCoins},
            {"CrrLevel", dts.CrrLevel},
            {"currentEquip", dts.currentEquip},
            {"ownedItems", dts.ownedItems}
        };
        dbRef.Child("Users").Child(userId).SetValueAsync(userData);*/
        dbRef.Child("users").Child(userId).SetRawJsonValueAsync(json);
    }

    public void LoadDataFn()
    {
        //StartCoroutine(LoadDataEnum());
        dbRef.Child("users").Child(userId).ValueChanged += HandleValueChanged;
    }

    IEnumerator LoadDataEnum()
    {
        var serverData = dbRef.Child("users").Child(userId).GetValueAsync();
        yield return new WaitUntil(predicate: () => serverData.IsCompleted);

        print("process is completed");

        DataSnapshot snapshot = serverData.Result;
        string jsonData = snapshot.GetRawJsonValue();

        if (jsonData != null)
        {
            print("server data found");
            dts = JsonUtility.FromJson<DataToSave>(jsonData);
            profileUserCoins_Text.text = dts.TotalCoins.ToString();
            profileUserLevel_Text.text = dts.CrrLevel.ToString();
        }
        else
        {
            print("no data found");
        }
    }

    private void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Database error: " + args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Exists)
        {
            string jsonData = args.Snapshot.GetRawJsonValue();
            dts = JsonUtility.FromJson<DataToSave>(jsonData);

            // 更新 UI
            profileUserCoins_Text.text = dts.TotalCoins.ToString();
            profileUserLevel_Text.text = dts.CrrLevel.ToString();

            Debug.Log("Data Updated in Real-Time");
            OnDataLoaded?.Invoke();
        }
        else
        {
            Debug.Log("No data found for this user.");
        }
    }

    private void Update()
    {
        if (dataUpdated)
        {
            profileUserCoins_Text.text = dts.TotalCoins.ToString();
            profileUserLevel_Text.text = dts.CrrLevel.ToString();
            dataUpdated = false;
        }
    }

    public async void UpdatePurchase(string itemType, int itemId, int price)
    {
        // 1️⃣ 扣金幣
        dts.TotalCoins -= price;

        // 2️⃣ 新增物品到擁有清單
        List<int> ownedList = dts.ownedItems.GetList(itemType);
        if (ownedList != null && !ownedList.Contains(itemId))
            ownedList.Add(itemId);

        // 3️⃣ 建立要更新的欄位（只更新金幣和該項物品）
        var updates = new Dictionary<string, object>
    {
        { "TotalCoins", dts.TotalCoins },
        { $"ownedItems/{itemType}", ownedList } // 只更新該類型的清單
    };

        // 4️⃣ 執行局部更新，不會覆蓋整筆資料
        await dbRef.Child("users").Child(userId).UpdateChildrenAsync(updates);
        Debug.Log($"✅ 成功購買 {itemType} {itemId}，剩餘金幣 {dts.TotalCoins}");
    }

    public void SetTomorrowReservationTime(string uid, string time)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("❌ Cannot set reservation time, UID is empty!");
            return;
        }

        DatabaseReference userRef = FirebaseDatabase.DefaultInstance
            .RootReference
            .Child("users")
            .Child(uid)
            .Child("TomorrowReservationTime");

        userRef.SetValueAsync(time).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log("✔ Reservation time saved: " + time);
            }
            else
            {
                Debug.LogError("❌ Failed to write reservation time: " + task.Exception);
            }
        });
    }
    public void SetUserMessage(string uid, string message)
    {
        DatabaseReference userRef = FirebaseDatabase.DefaultInstance
            .GetReference("users").Child(uid).Child("Message");
        userRef.SetValueAsync(message);
    }

    public void AddCoins(int amount)
    {
        // 1. 先更新本地端的數據，讓 UI 即時反應
        dts.TotalCoins += amount;

        // 2. 只更新 Firebase 上的 TotalCoins 欄位，不影響其他資料 (如 Friends)
        dbRef.Child("users").Child(userId).Child("TotalCoins").SetValueAsync(dts.TotalCoins);

        // 如果想要確保資料一致性，也可以像 UpdatePurchase 那樣用 UpdateChildrenAsync，
        // 但因為這裡只改一個值，直接指名路徑 Child("TotalCoins") 最快且最安全。
        Debug.Log($"已更新金幣: +{amount}, 目前總額: {dts.TotalCoins}");
    }
}
