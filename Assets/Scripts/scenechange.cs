using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    // 載入 Classroom，但保留 CozyStudyCorner（好友系統）
    public void GoToClassroom()
    {
        // 若 CozyStudyCorner 尚未載入，就一起載入
        if (!SceneManager.GetSceneByName("CozyStudyCorner").isLoaded)
        {
            SceneManager.LoadScene("CozyStudyCorner", LoadSceneMode.Additive);
        }

        // 切換到 Classroom（主視場景）
        SceneManager.LoadScene("classroom", LoadSceneMode.Single);
    }

    public void BackToMain()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void GoToWorld()
    {
        SceneManager.LoadScene("world");
    }

    public void LoadWithFriendSystem()
    {
        SceneManager.LoadScene("CozyStudyCorner", LoadSceneMode.Additive);
    }

    public void GoToMap()
    {
        // 嘗試取得 FriendSystemController
        FirebaseController au = FindObjectOfType<FirebaseController>();
        if (au != null)
        {
            au.OpenMapPanel();
        }
        else
        {
            Debug.LogWarning("⚠️ FriendSystemController 尚未載入！");
        }
    }
    public void GoToFriend()
    {
        // 嘗試取得 FriendSystemController
        FriendSystemController fs = FindObjectOfType<FriendSystemController>();
        if (fs != null)
        {
            fs.OpenFriendSystemController();
        }
        else
        {
            Debug.LogWarning("⚠️ FriendSystemController 尚未載入！");
        }
    }

    public void GoToShop()
    {
        // 嘗試取得 FriendSystemController
        ShopController shop = FindObjectOfType<ShopController>();
        if (shop != null)
        {
            shop.OpenShopPanel();
        }
        else
        {
            Debug.LogWarning("⚠️ ShopController 尚未載入！");
        }
    }
}
