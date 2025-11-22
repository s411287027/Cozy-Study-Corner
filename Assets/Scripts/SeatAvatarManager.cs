using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;

public class SeatAvatarManager : MonoBehaviour
{
    [System.Serializable]
    public class SeatData
    {
        public string seatName; // "1-1"
        public Transform seatTransform; // 座位位置
        public PlayerSitController sitControllerPrefab; // 小人 prefab
    }

    public SeatData[] seats;

    private DatabaseReference dbRef;
    private Dictionary<string, PlayerSitController> seatAvatars = new Dictionary<string, PlayerSitController>();

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        ListenSeatChanges();
    }

    void ListenSeatChanges()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("Seat/Classroom")
            .ValueChanged += OnSeatValueChanged;
    }

    private void OnSeatValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.Snapshot == null) return;

        foreach (var seat in seats)
        {
            var uid = e.Snapshot.Child(seat.seatName).Value as string;

            if (!string.IsNullOrEmpty(uid))
            {
                // 座位有人，顯示小人
                if (!seatAvatars.ContainsKey(seat.seatName))
                {
                    var avatar = Instantiate(seat.sitControllerPrefab, seat.seatTransform.position, Quaternion.identity);
                    Debug.Log("Avatar instantiated: " + avatar.name);
                    avatar.Sit(GetPartsDataForUID(uid, seat.seatTransform));
                    seatAvatars[seat.seatName] = avatar;
                }
            }
            else
            {
                // 座位沒人，移除小人
                if (seatAvatars.ContainsKey(seat.seatName))
                {
                    seatAvatars[seat.seatName].StandUp();
                    Destroy(seatAvatars[seat.seatName].gameObject);
                    seatAvatars.Remove(seat.seatName);
                }
            }
        }
    }

    // 這個函式負責生成 SitPartData，根據 UID 可以換不同造型
    private SitButton.SitPartData[] GetPartsDataForUID(string uid, Transform seatTransform)
    {
        return new SitButton.SitPartData[]
        {
        new SitButton.SitPartData()
        {
            partName = "Body",
            position = seatTransform,
            sprite = Resources.Load<Sprite>("Sprites/seat/坐姿 正面 身體"),
            sortingOrder = 10,
            scale = Vector3.one
        },
        new SitButton.SitPartData()
        {
            partName = "Leg",
            position = seatTransform,
            sprite = Resources.Load<Sprite>("Sprites/seat/坐姿 正面 腿"),
            sortingOrder = 11,
            scale = Vector3.one
        }
        };
    }

}
