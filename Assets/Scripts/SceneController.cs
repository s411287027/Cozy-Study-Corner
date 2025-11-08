using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void GoToHome()
    {
        // 載入 SampleScene（Additive）
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Additive);

        // 設定 SampleScene 為 Active Scene
        Scene sampleScene = SceneManager.GetSceneByName("SampleScene");
        if (sampleScene.IsValid())
        {
            SceneManager.SetActiveScene(sampleScene);
        }

        // 調整 SampleScene Canvas 排序（確保顯示在最前面）
        Canvas canvasB = FindObjectOfType<Canvas>();
        if (canvasB != null)
            canvasB.sortingOrder = 1;
    }

}
