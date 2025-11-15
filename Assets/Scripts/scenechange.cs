using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
        SceneManager.LoadScene("Map");
    }
    public void GoToFriend()
    {
        StartCoroutine(EnsureCozyLoadedAndOpenFriend());
    }

    private IEnumerator EnsureCozyLoadedAndOpenFriend()
    {
        // 先確保 CozyStudyCorner 載入
        Scene cozyScene = SceneManager.GetSceneByName("CozyStudyCorner");
        if (!cozyScene.isLoaded)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("CozyStudyCorner", LoadSceneMode.Additive);
            yield return asyncLoad;
        }

        // 等單例初始化
        while (FriendSystemController.Instance == null)
            yield return null;

        // 開啟 FriendSystem
        FriendSystemController.Instance.OpenFriendSystemController();

        // 將 CozyStudyCorner 設為 active scene，確保 UI 事件正常
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("CozyStudyCorner"));
    }



    public void GoToShop()
    {
        StartCoroutine(LoadCSceneAndOpenShop());
    }

    private IEnumerator LoadCSceneAndOpenShop()
    {
        // 先檢查 CozyStudyCorner 是否已載入
        Scene cozyScene = SceneManager.GetSceneByName("CozyStudyCorner");
        if (!cozyScene.isLoaded)
        {
            var asyncLoad = SceneManager.LoadSceneAsync("CozyStudyCorner", LoadSceneMode.Additive);
            yield return new WaitUntil(() => asyncLoad.isDone);
        }

        // 等場景完全載入
        cozyScene = SceneManager.GetSceneByName("CozyStudyCorner");

        // 找 ShopController
        ShopController shop = null;
        foreach (var root in cozyScene.GetRootGameObjects())
        {
            shop = root.GetComponentInChildren<ShopController>();
            if (shop != null) break;
        }

        if (shop != null)
        {
            shop.OpenShopPanel();
        }
        else
        {
            Debug.LogWarning("⚠️ ShopController not found in CozyStudyCorner!");
        }

        // 可選：設定 CozyStudyCorner 為 active scene，確保 UI 事件正常
        SceneManager.SetActiveScene(cozyScene);
    }

    public void GoToCafe()
    {
        SceneManager.LoadScene("CafeScene");
    }

    public void GoToLibrary()
    {
        SceneManager.LoadScene("LibraryScene");
    }

    public void GoToForest()
    {
        SceneManager.LoadScene("ForestScene");
    }

    public void GoToPool()
    {
        SceneManager.LoadScene("SwimmingPool");
    }

    public void GoToDress()
    {
        SceneManager.LoadScene("DressScene");
    }
}
