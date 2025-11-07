using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;

public class SeatController : MonoBehaviour
{
    public int seatNumber;             // 座位編號 (1~8)
    public Button seatButton;          // 座位按鈕
    public Image playerIcon;           // 玩家圖示
    public Sprite[] avatarSprites;     // 可選的頭像圖片 (在 Inspector 設定)

    private DatabaseReference dbRef;
    private string userId;
    private int myAvatarId;

    void Start()
    {
        // 取得 Firebase DB 根目錄
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("PlayerIcon: " + (playerIcon != null));
        // 取得登入玩家 ID
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            //Debug.LogError("PlayerId:" + user.UserId);
            userId = user.UserId;
        }
        else
        {
            Debug.LogError("Player not logged in!");
            return;
        }

        // 隨機給玩家一個頭像 ID
        myAvatarId = Random.Range(0, avatarSprites.Length);

        // 設定按鈕點擊事件
        seatButton.onClick.AddListener(OnSeatClicked);

        // 監聽這個座位的變化（即時同步）
        FirebaseDatabase.DefaultInstance
            .GetReference("Seats")
            .Child(seatNumber.ToString())
            .ValueChanged += HandleSeatChanged;
    }

    void OnSeatClicked()
    {
        Debug.Log($"Try occupy seat {seatNumber} with userId: {userId}");

        dbRef.Child("Seats").Child(seatNumber.ToString()).RunTransaction(mutableData =>
        {
            var seatData = mutableData.Value as Dictionary<string, object>;
            if (seatData == null)
            {
                seatData = new Dictionary<string, object>();
            }

            string occupiedBy = seatData.ContainsKey("occupiedBy") ? seatData["occupiedBy"] as string : null;

            if (!string.IsNullOrEmpty(occupiedBy))
            {
                Debug.Log($"座位 {seatNumber} 已被 {occupiedBy} 佔用");
                return TransactionResult.Abort(); // 已有人佔用 → 停止寫入
            }

            // 設定座位資料
            seatData["occupiedBy"] = userId;
            seatData["avatarId"] = myAvatarId;

            mutableData.Value = seatData;
            Debug.Log($"座位 {seatNumber} 成功佔用，userId: {userId}");
            return TransactionResult.Success(mutableData);
        });
    }


    void HandleSeatChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (!args.Snapshot.Exists || string.IsNullOrEmpty(args.Snapshot.Child("occupiedBy").Value as string))
        {
            playerIcon.gameObject.SetActive(false);
            seatButton.interactable = true;
            return;
        }

        var occupiedBy = args.Snapshot.Child("occupiedBy").Value as string;
        var avatarId = args.Snapshot.Child("avatarId").Value;

        Debug.Log($"座位 {seatNumber} 被 {occupiedBy} 佔用");

        if (!string.IsNullOrEmpty(occupiedBy))
        {
            int id = avatarId != null ? int.Parse(avatarId.ToString()) : 0;
            if (id >= 0 && id < avatarSprites.Length)
                playerIcon.sprite = avatarSprites[id];
            playerIcon.gameObject.SetActive(true);

            seatButton.interactable = false; // 已被佔 → 按鈕不能點
        }
        else
        {
            playerIcon.gameObject.SetActive(false);
            seatButton.interactable = true;
        }
    }


    private void OnDestroy()
    {
        // 取消監聽，避免記憶體洩漏
        FirebaseDatabase.DefaultInstance
            .GetReference("Seats")
            .Child(seatNumber.ToString())
            .ValueChanged -= HandleSeatChanged;
    }
}
