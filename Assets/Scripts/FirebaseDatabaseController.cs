using UnityEngine;
using System;
using Firebase.Database;
using System.Collections;
using TMPro;
using System.Collections.Generic;

[Serializable]
public class EquipData
{
    public int hair;
    public int pants;
    public int shoes;
    public int face;
    public int other;
}

[Serializable]
public class OwnedItems
{
    public List<int> hair = new List<int>();
    public List<int> pants = new List<int>();
    public List<int> shoes = new List<int>();
    public List<int> face = new List<int>();
    public List<int> other = new List<int>();
    public List<int> furniture = new List<int>();

    public List<int> GetList(string itemType)
    {
        return itemType switch
        {
            "hair" => hair,
            "pants" => pants,
            "shoes" => shoes,
            "face" => face,
            "other" => other,
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
    public DataToSave dts;
    public string userId;
    DatabaseReference dbRef;
    private bool dataUpdated = false;
    public event Action OnDataLoaded;

    public TMP_Text profileUserLevel_Text, profileUserCoins_Text;

    private void Awake()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
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

}
