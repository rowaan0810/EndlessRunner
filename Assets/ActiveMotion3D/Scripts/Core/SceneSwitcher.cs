using UnityEngine;
using UnityEngine.SceneManagement;

namespace ActiveMotion3D
{
    public class SceneSwitcher : MonoBehaviour
{
    // 在 Button 的 On Click() 選擇這個方法，並直接輸入目標場景名稱。
    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("SceneSwitcher: Scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneSwitcher: Scene '{sceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
}
