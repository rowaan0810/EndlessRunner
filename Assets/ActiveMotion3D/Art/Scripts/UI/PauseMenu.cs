using System.Collections;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Input;
using ActiveMotion3D.EndlessRunner.Input.PoseDetection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ActiveMotion3D.EndlessRunner.UI
{
    /// <summary>Pause controls for the authored ActiveMotion3D gameplay scene.</summary>
    [DefaultExecutionOrder(-50)]
    public class PauseMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Font font;
        [SerializeField] private Font titleFont;

        [Header("Controls")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private string mainMenuScene = "AM3D_MainMenu";
        [SerializeField] private string pauseLabel = "PAUSE";
        [SerializeField] private string title = "Paused";
        [SerializeField] private string resumeLabel = "Resume";
        [SerializeField] private string restartLabel = "Restart";
        [SerializeField] private string menuLabel = "Main menu";
        [SerializeField] private string quitLabel = "Quit";
        [SerializeField] private bool showRunSpeedControl;

        [Header("Layout")]
        [SerializeField] private Vector2 pauseButtonPosition = new Vector2(-25f, -92f);
        [SerializeField] private Vector2 pauseButtonPivot = new Vector2(1f, 1f);
        [SerializeField] private Vector2 pauseButtonSize = new Vector2(150f, 44f);
        [SerializeField] private Vector2 cardSize = new Vector2(590f, 544f);
        [SerializeField] private Vector2 actionButtonSize = new Vector2(496f, 68f);
        [SerializeField] private float titleYOffset = 184f;
        [SerializeField] private float titleHeight = 90f;
        [SerializeField] private float firstActionYOffset = 73f;
        [SerializeField] private float actionSpacing = 82f;
        [SerializeField, Min(1)] private int titleFontSize = 68;
        [SerializeField, Min(1)] private int buttonFontSize = 22;

        [Header("Colors")]
        [SerializeField] private Color overlayColor = new Color(0.047058824f, 0.03529412f, 0.078431375f, 0.5f);
        [SerializeField] private Color cardColor = new Color32(22, 19, 31, 245);
        [SerializeField] private Color buttonColor = new Color(0.12f, 0.12f, 0.21f, 1f);
        [SerializeField] private Color accentColor = new Color(0f, 0.95f, 1f, 1f);

        // Palette used by the RunResults prefab.
        private static readonly Color Pink = new Color32(244, 158, 207, 255);
        private static readonly Color Plum = new Color32(33, 29, 45, 255);
        private static readonly Color Border = new Color32(77, 65, 93, 255);
        private static readonly Color TextColor = new Color32(248, 239, 250, 255);
        private static readonly Color MutedTextColor = new Color32(199, 184, 210, 255);
        private static readonly Color PrimaryTextColor = new Color32(22, 19, 31, 255);

        private GameObject overlay;
        private Text gestureHint;
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
                new Vector2(1f, 1f), pauseButtonPosition, pauseButtonSize, TogglePause,
                buttonColor, accentColor, buttonFontSize, FontStyle.Normal);
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
            var cardImage = card.AddComponent<Image>();
            cardImage.color = cardColor;
            cardImage.raycastTarget = false;
            var frame = card.AddComponent<Outline>();
            frame.effectColor = Border;
            frame.effectDistance = new Vector2(1f, -1f);

            float contentWidth = actionButtonSize.x;
            const float headingInset = 24f;
            CreateDecoration(card.transform, "HeaderAccent",
                new Vector2(-contentWidth / 2f + 3f, titleYOffset), new Vector2(6f, 62f), Pink);
            var heading = CreateText(card.transform, "Title", title,
                new Vector2(headingInset / 2f, titleYOffset),
                new Vector2(contentWidth - headingInset, titleHeight), titleFontSize);
            heading.font = titleFont != null ? titleFont : font;
            heading.color = TextColor;
            heading.alignment = TextAnchor.MiddleLeft;
            CreateDecoration(card.transform, "HeaderSeparator",
                new Vector2(0f, titleYOffset - titleHeight / 2f - 10f),
                new Vector2(contentWidth, 1f), Border);

            if (showRunSpeedControl)
            {
                float speedControlY = (titleYOffset - titleHeight / 2f - 10f
                    + firstActionYOffset + actionButtonSize.y / 2f) / 2f;
                RunSpeedControl.Create(card.transform, new Vector2(0.5f, 0.5f),
                    new Vector2(0f, speedControlY), contentWidth, font, Plum, Pink);
            }

            CreateActionButton(card.transform, "ResumeButton", resumeLabel, 0, Resume, primary: true);
            CreateActionButton(card.transform, "RestartButton", restartLabel, 1, Restart);
            CreateActionButton(card.transform, "MainMenuButton", menuLabel, 2, ReturnToMenu);
            CreateActionButton(card.transform, "QuitButton", quitLabel, 3, Quit);
            gestureHint = CreateText(card.transform, "GestureHint", "V sign to resume  •  Hold thumbs down to quit",
                new Vector2(0f, firstActionYOffset - 3f * actionSpacing - actionButtonSize.y / 2f - 22f),
                new Vector2(contentWidth, 32f), 20);
            gestureHint.color = MutedTextColor;
            overlay.SetActive(false);
        }

        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null || leavingScene) return;

            if (UnityEngine.Input.GetKeyDown(pauseKey)) TogglePause();
            else if (InputManager.Instance?.CurrentInput is PoseInput poseInput)
            {
                switch (poseInput.ConfirmedGesture)
                {
                    case HandGesture.BothPalms when game.State == GameState.Playing:
                        TogglePause();
                        break;
                    case HandGesture.Victory when game.State == GameState.Paused:
                        Resume();
                        break;
                    case HandGesture.ThumbsDown when game.State == GameState.Playing || game.State == GameState.Paused:
                        Quit();
                        return;
                }
            }

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
                if (gestureHint != null)
                    gestureHint.gameObject.SetActive(InputManager.Instance?.CurrentMode == InputMode.Webcam);
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
            MediaPipeManager.Instance?.StopDetection();
            HandGestureManager.Instance?.StopDetection();
            yield return new WaitForEndOfFrame();
            yield return null;
            SceneManager.LoadScene(mainMenuScene);
        }

        public void Quit()
        {
            if (leavingScene) return;
            leavingScene = true;
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

        private static void CreateDecoration(Transform parent, string objectName, Vector2 position,
            Vector2 size, Color color)
        {
            var obj = CreateUIObject(parent, objectName, new Vector2(0.5f, 0.5f), position, size);
            var image = obj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private Text CreateText(Transform parent, string objectName, string label,
            Vector2 position, Vector2 size, int sizeInPoints)
        {
            var obj = CreateUIObject(parent, objectName, new Vector2(0.5f, 0.5f), position, size);
            var text = obj.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = sizeInPoints;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private void CreateActionButton(Transform parent, string objectName, string label, int row,
            UnityEngine.Events.UnityAction action, bool primary = false)
        {
            var button = CreateButton(parent, objectName, label, new Vector2(0.5f, 0.5f),
                new Vector2(0f, firstActionYOffset - row * actionSpacing), actionButtonSize, action,
                primary ? Pink : Plum, primary ? PrimaryTextColor : TextColor,
                primary ? 32 : 28, FontStyle.Bold);
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.9f, 0.97f);
            colors.pressedColor = new Color(0.84f, 0.73f, 0.92f);
            button.colors = colors;
        }

        private Button CreateButton(Transform parent, string objectName, string label,
            Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action,
            Color backgroundColor, Color labelColor, int labelSize, FontStyle labelStyle)
        {
            var obj = CreateUIObject(parent, objectName, anchor, position, size);
            obj.AddComponent<Image>().color = backgroundColor;
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);
            var text = CreateText(obj.transform, "Label", label, Vector2.zero, size, labelSize);
            text.color = labelColor;
            text.fontStyle = labelStyle;
            return button;
        }
    }
}
