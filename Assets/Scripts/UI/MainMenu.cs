// MainMenu.cs — Main menu with start button and input mode selector.

using UnityEngine;
using UnityEngine.UI;
using EndlessRunner.Core;
using EndlessRunner.Input;
using EndlessRunner.Player;

namespace EndlessRunner.UI
{
    /// <summary>
    /// Main menu screen with Start Game button, input mode selector, and title.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private GameObject panel;
        private Text modeLabel;
        private InputMode selectedMode = InputMode.Keyboard;

        private void Start()
        {
            CreateMenuUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart.AddListener(HideMenu);
            }

            // Show menu on start (after a tiny delay to let other systems init)
            Invoke(nameof(ShowMenu), 0.1f);
        }

        public void ShowMenu()
        {
            if (panel != null) panel.SetActive(true);

            // Pause the game (ensure we're in Menu state)
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Menu)
            {
                GameManager.Instance.ReturnToMenu();
            }
        }

        private void HideMenu()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void DoStartGame()
        {
            // Set the selected input mode
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetMode(selectedMode);
            }

            // Reset player
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.ResetPlayer();

            var collision = FindAnyObjectByType<PlayerCollision>();
            if (collision != null) collision.ResetCollision();

            // Start
            GameManager.Instance?.StartGame();
        }

        private void CycleMode()
        {
            selectedMode = selectedMode switch
            {
                InputMode.Keyboard => InputMode.WebcamPose,
                InputMode.WebcamPose => InputMode.EasyMode,
                InputMode.EasyMode => InputMode.Keyboard,
                _ => InputMode.Keyboard
            };

            UpdateModeLabel();
        }

        private void UpdateModeLabel()
        {
            if (modeLabel == null) return;

            string modeName = selectedMode switch
            {
                InputMode.Keyboard => "⌨  Keyboard",
                InputMode.WebcamPose => "📷  Webcam Pose",
                InputMode.EasyMode => "👴  Easy Mode",
                _ => "Keyboard"
            };

            string status = selectedMode switch
            {
                InputMode.Keyboard => "",
                InputMode.WebcamPose => " (Week 3)",
                InputMode.EasyMode => " (Week 3)",
                _ => ""
            };

            modeLabel.text = $"Mode: {modeName}{status}";
        }

        private void DoQuit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void Update()
        {
            if (panel != null && panel.activeInHierarchy)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
                {
                    DoStartGame();
                }
            }
        }

        private void CreateMenuUI()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("UICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Full screen panel with dark background
            panel = new GameObject("MainMenuPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image overlay = panel.AddComponent<Image>();
            overlay.color = new Color(0.06f, 0.06f, 0.12f, 0.92f);

            // Title
            CreateText(panel.transform, "ENDLESS RUNNER", 56,
                new Vector2(0f, 160f), new Color(0.3f, 0.85f, 0.5f), FontStyle.Bold);

            // Subtitle
            CreateText(panel.transform, "A Simplified Subway Surfers Experience", 22,
                new Vector2(0f, 100f), new Color(0.5f, 0.5f, 0.6f), FontStyle.Normal);

            // Start button
            Button startBtn = CreateButton(panel.transform, "START GAME", 32,
                new Vector2(0f, 10f), new Vector2(320f, 65f),
                new Color(0.2f, 0.75f, 0.4f), Color.white);
            startBtn.onClick.AddListener(DoStartGame);

            // Mode selector
            modeLabel = CreateText(panel.transform, "", 22,
                new Vector2(0f, -60f), new Color(0.7f, 0.7f, 0.8f), FontStyle.Normal);
            UpdateModeLabel();

            Button modeBtn = CreateButton(panel.transform, "CHANGE MODE", 18,
                new Vector2(0f, -100f), new Vector2(220f, 40f),
                new Color(0.3f, 0.3f, 0.4f), new Color(0.7f, 0.7f, 0.8f));
            modeBtn.onClick.AddListener(CycleMode);

            // Controls info
            CreateText(panel.transform, "Controls: Arrow Keys / WASD  |  Space / W: Jump  |  S / ↓: Duck", 18,
                new Vector2(0f, -170f), new Color(0.45f, 0.45f, 0.5f), FontStyle.Normal);

            CreateText(panel.transform, "Press ENTER or SPACE to start", 20,
                new Vector2(0f, -210f), new Color(0.5f, 0.5f, 0.55f), FontStyle.Italic);

            // Quit button
            Button quitBtn = CreateButton(panel.transform, "QUIT", 18,
                new Vector2(0f, -270f), new Vector2(140f, 38f),
                new Color(0.4f, 0.2f, 0.2f), new Color(0.8f, 0.6f, 0.6f));
            quitBtn.onClick.AddListener(DoQuit);

            panel.SetActive(false); // Start hidden; shown by Invoke in Start()
        }

        #region UI Helpers

        private static Text CreateText(Transform parent, string content, int fontSize,
            Vector2 position, Color color, FontStyle style = FontStyle.Normal)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(800f, fontSize + 20);

            Text text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);

            return text;
        }

        private static Button CreateButton(Transform parent, string label, int fontSize,
            Vector2 position, Vector2 size, Color bgColor, Color textColor)
        {
            GameObject obj = new GameObject("Btn_" + label, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = obj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return btn;
        }

        #endregion
    }
}
