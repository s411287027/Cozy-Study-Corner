using UnityEngine;
using System.Collections;

public class PageMusicTrigger : MonoBehaviour
{
    [Header("填寫這個頁面對應的名稱")]
    public string pageName;

    private void OnEnable()
    {
        // 啟動一個協程來處理音樂請求
        StartCoroutine(TrySwitchMusic());
    }

    IEnumerator TrySwitchMusic()
    {
        // 1. 如果 Manager 還沒準備好 (Instance 為 null)，就每幀等待
        // 這裡設定一個簡單的等待迴圈，確保一定找得到 Manager
        while (SceneMusicManager.Instance == null)
        {
            yield return null; // 等待下一個 Frame
        }

        // 2. 確定 Manager 存在了，發送播放請求
        SceneMusicManager.Instance.SwitchMusic(pageName);
    }
}