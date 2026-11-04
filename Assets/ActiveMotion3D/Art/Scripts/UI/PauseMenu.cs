using System.Collections;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Input.PoseDetection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ActiveMotion3D.EndlessRunner.UI
{
    /// <summary>Pause controls for the authored ActiveMotion3D gameplay scene.</summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Font font;

        [Header("Controls")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private string mainMenuScene = "AM3D_MainMenu";
        [SerializeField] private string pauseLabel = "PAUSE";
        [SerializeField] private string title = "PAUSED";
        [SerializeField] private string resumeLabel = "RESUME";
        [SerializeField] private string restartLabel = "RESTART";
        [SerializeField] private string menuLabel = "MAIN MENU";
        [SerializeField] private string quitLabel = "QUIT";

        [Header("Layout")]
        [SerializeField] private Vector2 pauseButtonPosition = new Vector2(-25f, -92f);
        [SerializeField] private Vector2 pauseButtonPivot = new Vector2(1f, 1f);
        [SerializeField] private Vector2 pauseButtonSize = new Vector2(150f, 44f);
        [SerializeField] private Vector2 cardSize = new Vector2(420f, 440f);
        [SerializeField] private Vector2 actionButtonSize = new Vector2(320f, 56f);
        [SerializeField] private float titleYOffset = 155f;
        [SerializeField] private float titleHeight = 60f;
        [SerializeField] private float titleSidePadding = 24f;
        [SerializeField] private float firstActionYOffset = 70f;
        [SerializeField] private float actionSpacing = 68f;
        [SerializeField, Min(1)] private int titleFontSize = 36;
        [SerializeField, Min(1)] private int buttonFontSize = 22;

        [Header("Colors")]
        [SerializeField] private Color overlayColor = new Color(0.01f, 0.01f, 0.03f, 0.84f);
        [SerializeField] private Color cardColor = new Color(0.07f, 0.07f, 0.14f, 0.98f);
        [SerializeField] private Color buttonColor = new Color(0.12f, 0.12f, 0.21f, 1f);
        [SerializeField] private Color accentColor = new Color(0f, 0.95f, 1f, 1f);

        private GameObject overlay;
        private Button pauseButton;
        private bool leavingScene;

        private void Start()
        {
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("PauseMenu needs a Canvas reference.", this);
                enabled = false;
                return;
            }

            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            pauseButton = CreateButton(canvas.transform, "PauseButton", pauseLabel,
                new Vector2(1f, 1f), pauseButtonPosition, pauseButtonSize, TogglePause);
            pauseButton.GetComponent<RectTransform>().pivot = pauseButtonPivot;
            pauseButton.transform.SetAsLastSibling();

            overlay = CreateUIObject(canvas.transform, "PauseOverlay", Vector2.zero,
                Vector2.zero, Vector2.one);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.AddComponent<Image>().color = overlayColor;

            var card = CreateUIObject(overlay.transform, "PauseCard", new Vector2(0.5f, 0.5f),
                Vector2.zero, cardSize);
            card.AddComponent<Image>().color = cardColor;
            CreateText(card.transform, "Title", title, new Vector2(0f, titleYOffset),
                new Vector2(cardSize.x - titleSidePadding, titleHeight), titleFontSize);
            CreateButton(card.transform, "ResumeButton", resumeLabel, new Vector2(0.5f, 0.5f),
                new Vector2(0f, firstActionYOffset), actionButtonSize, Resume);
            CreateButton(card.transform, "RestartButton", restartLabel, new Vector2(0.5f, 0.5f),
                new Vector2(0f, firstActionYOffset - actionSpacing), actionButtonSize, Restart);
            CreateButton(card.transform, "MainMenuButton", menuLabel, new Vector2(0.5f, 0.5f),
                new Vector2(0f, firstActionYOffset - 2f * actionSpacing), actionButtonSize, ReturnToMenu);
            CreateButton(card.transform, "QuitButton", quitLabel, new Vector2(0.5f, 0.5f),
                new Vector2(0f, firstActionYOffset - 3f * actionSpacing), actionButtonSize, Quit);
            overlay.SetActive(false);
        }

        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null || leavingScene) return;

            if (UnityEngine.Input.GetKeyDown(pauseKey)) TogglePause();
            if (pauseButton != null)
                pauseButton.gameObject.SetActive(game.State == GameState.Playing);
        }

        public void TogglePause()
        {
            var game = GameManager.Instance;
            if (game == null || leavingScene) return;
            if (game.State == GameState.Playing)
            {
                game.PauseGame();
                overlay?.SetActive(true);
                overlay?.transform.SetAsLastSibling();
            }
            else if (game.State == GameState.Paused) Resume();
        }

        public void Resume()
        {
            GameManager.Instance?.ResumeGame();
            if (overlay != null) overlay.SetActive(false);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            if (overlay != null) overlay.SetActive(false);
            var mainMenu = FindAnyObjectByType<MainMenu>();
            if (mainMenu != null) mainMenu.RestartGameWithCountdown();
            else GameManager.Instance?.RestartGame();
        }

        public void ReturnToMenu()
        {
            if (!leavingScene) StartCoroutine(ReturnToMenuRoutine());
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            leavingScene = true;
            Time.timeScale = 1f;
            GameManager.Instance?.ReturnToMenu();
            if (GlobalGestureController.Instance == null)
            {
                MediaPipeManager.Instance?.StopDetection();
                HandGestureManager.Instance?.StopDetection();
            }
            yield return new WaitForEndOfFrame();
            yield return null;
            SceneManager.LoadScene(mainMenuScene);
        }

        public void Quit()
        {
            Time.timeScale = 1f;
            MediaPipeManager.Instance?.StopDetection();
            HandGestureManager.Instance?.StopDetection();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static GameObject CreateUIObject(Transform parent, string objectName,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(objectName, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return obj;
        }

        private void CreateText(Transform parent, string objectName, string label,
            Vector2 position, Vector2 size, int sizeInPoints)
        {
            var obj = CreateUIObject(parent, objectName, new Vector2(0.5f, 0.5f), position, size);
            var text = obj.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = sizeInPoints;
            text.fontStyle = FontStyle.Bold;
            text.color = accentColor;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private Button CreateButton(Transform parent, string objectName, string label,
            Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var obj = CreateUIObject(parent, objectName, anchor, position, size);
            obj.AddComponent<Image>().color = buttonColor;
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);
            var textObj = CreateUIObject(obj.transform, "Label", new Vector2(0.5f, 0.5f),
                Vector2.zero, size);
            var text = textObj.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = buttonFontSize;
            text.color = accentColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return button;
        }
    }
}
