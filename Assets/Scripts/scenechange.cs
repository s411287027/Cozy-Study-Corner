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

    public void GoToMap_shop()
    {
        // 1. 先嘗試找到 ShopController 並關閉它
        // 使用 FindAnyObjectByType 確保即使它在 Global_System 也能被找到
        ShopController shop = Object.FindAnyObjectByType<ShopController>();

        if (shop != null)
        {
            shop.CloseShopPanel(); // 呼叫剛剛新增的關閉方法
        }

        // 2. 接著才切換場景
        SceneManager.LoadScene("Map");
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
        Scene cozyScene = SceneManager.GetSceneByName("CozyStudyCorner");
        if (!cozyScene.isLoaded)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("CozyStudyCorner", LoadSceneMode.Additive);
            yield return asyncLoad;
            cozyScene = SceneManager.GetSceneByName("CozyStudyCorner");
        }

        // 找到登入面板並隱藏
        foreach (var rootObj in cozyScene.GetRootGameObjects())
        {
            var loginPanel = rootObj.transform.Find("LoginPage");
            var FriendPanel = rootObj.transform.Find("FriendPage");
            if (loginPanel != null)
            {
                loginPanel.gameObject.SetActive(false);
                FriendPanel.gameObject.SetActive(true);
            }
        }

        // 等單例初始化
        while (FriendSystemController.Instance == null)
            yield return null;

        FriendSystemController.Instance.OpenFriendSystemController();

        SceneManager.SetActiveScene(cozyScene);
    }

    public void GoToShop()
    {
        StartCoroutine(LoadCSceneAndOpenShop());
    }

    private IEnumerator LoadCSceneAndOpenShop()
    {
        // 1. 確保 CozyStudyCorner 場景已載入 (如果 Global_System 依賴於此場景的資源)
        // 注意：如果 Global_System 已經在運作，其實不一定需要載入 CozyStudyCorner，
        // 但為了保險起見保留你的原始邏輯。
        Scene cozyScene = SceneManager.GetSceneByName("CozyStudyCorner");
        if (!cozyScene.isLoaded)
        {
            var asyncLoad = SceneManager.LoadSceneAsync("CozyStudyCorner", LoadSceneMode.Additive);
            yield return new WaitUntil(() => asyncLoad.isDone);
        }

        // 等待一幀確保物件初始化完成
        yield return null;

        // 2. 尋找 ShopController
        // 修改重點：不要在特定場景找，而是全域尋找。
        // 因為 Global_System 被移到 DontDestroyOnLoad 區之後，不屬於一般場景。

        ShopController shop = Object.FindAnyObjectByType<ShopController>();
        // 如果你是舊版 Unity (2021以前)，請使用: ShopController shop = FindObjectOfType<ShopController>();

        if (shop != null)
        {
            // 確保 Global_System 或 Canvas 是開啟的 (如果有被關閉的話)
            // 這裡假設 shop.gameObject 就在 Global_System 下
            shop.gameObject.SetActive(true);

            shop.OpenShopPanel();
            Debug.Log("✅ 成功打開商店");
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 ShopController！請確認 Global_System 是否存在於場景中，且掛載了 ShopController 腳本。");
        }
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

    public void GoToCamp()
    {
        SceneManager.LoadScene("CampScene");
    }
}
