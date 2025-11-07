using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    // 切換到 ShopScene
    public void GoToClassroom()
    {
        SceneManager.LoadScene("classroom");
    }

    // 如果要切回 MainScene
    public void BackToMain()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void GoToWorld()
    {
        SceneManager.LoadScene("world");
    }


}
