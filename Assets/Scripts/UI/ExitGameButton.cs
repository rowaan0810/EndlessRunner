using UnityEngine;
using UnityEngine.UI;

namespace EndlessRunner.UI
{
    /// <summary>
    /// Attach this component to a Unity UI Button to quit the game when it is clicked.
    /// </summary>
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

        /// <summary>Stops Play Mode in the Editor and exits a standalone build.</summary>
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
