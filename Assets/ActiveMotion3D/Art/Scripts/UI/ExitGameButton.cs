using UnityEngine;
using UnityEngine.UI;

namespace ActiveMotion3D.EndlessRunner.UI
{
    /// <summary>Exits the ActiveMotion3D game from a UI button.</summary>
    [RequireComponent(typeof(Button))]
    public class ExitGameButton : MonoBehaviour
    {
        private Button exitButton;

        private void Awake()
        {
            exitButton = GetComponent<Button>();
            exitButton.onClick.AddListener(QuitGame);
        }

        private void OnDestroy()
        {
            if (exitButton != null)
                exitButton.onClick.RemoveListener(QuitGame);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
