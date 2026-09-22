// GameOverScreen.cs — Game over overlay with score summary and action buttons.

using UnityEngine;
using UnityEngine.UI;
using EndlessRunner.Core;
using EndlessRunner.Player;

namespace EndlessRunner.UI
{
    /// <summary>
    /// Game over screen showing final score, high score, and restart/menu buttons.
    /// Displayed when the player collides with an obstacle.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        private GameObject panel;
        private Text titleText;
        private Text scoreText;
        private Text highScoreText;
        private Text statsText;
        private Button restartButton;
        private Button menuButton;

        private void Start()
        {
            CreateGameOverUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver.AddListener(Show);
                GameManager.Instance.OnGameStart.AddListener(Hide);
            }
        }

        private void Show()
        {
            if (panel == null) return;
            panel.SetActive(true);

            var gm = GameManager.Instance;
            if (gm == null) return;

            if (scoreText != null)
                scoreText.text = $"{gm.TotalScore}";

            bool isNewHigh = gm.TotalScore >= gm.HighScore;
            if (highScoreText != null)
            {
                highScoreText.text = isNewHigh ? "NEW HIGH SCORE!" : $"Best: {gm.HighScore}";
                highScoreText.color = isNewHigh ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
            }

            if (statsText != null)
                statsText.text = $"Distance: {gm.DistanceScore:F0}m  |  Coins: {gm.CoinScore}  |  Time: {gm.TimeSurvived:F1}s";
        }

        private void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void DoRestart()
        {
            // Reset player
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.ResetPlayer();

            // Reset collision state
            var collision = FindAnyObjectByType<PlayerCollision>();
            if (collision != null) collision.ResetCollision();

            GameManager.Instance?.RestartGame();
        }

        private void DoMenu()
        {
            Hide();
            GameManager.Instance?.ReturnToMenu();
            // Show main menu (if it exists)
            var mainMenu = FindAnyObjectByType<MainMenu>();
            if (mainMenu != null) mainMenu.ShowMenu();
        }

        private void CreateGameOverUI()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Semi-transparent overlay panel
            panel = new GameObject("GameOverPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image overlay = panel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.7f);

            // Content container (centered box)
            GameObject content = CreateCenteredBox(panel.transform, "Content", 450f, 380f,
                new Color(0.12f, 0.12f, 0.18f, 0.95f));

            // Title
            titleText = CreateText(content.transform, "GAME OVER", 48,
                new Vector2(0f, 100f), new Color(0.95f, 0.3f, 0.3f));

            // Score
            scoreText = CreateText(content.transform, "0", 64,
                new Vector2(0f, 40f), Color.white);

            // High score
            highScoreText = CreateText(content.transform, "Best: 0", 22,
                new Vector2(0f, -10f), new Color(0.7f, 0.7f, 0.7f));

            // Stats
            statsText = CreateText(content.transform, "Distance: 0m  |  Coins: 0  |  Time: 0s", 18,
                new Vector2(0f, -50f), new Color(0.6f, 0.6f, 0.6f));

            // Restart button
            restartButton = CreateButton(content.transform, "PLAY AGAIN", 28,
                new Vector2(0f, -110f), new Vector2(280f, 55f),
                new Color(0.2f, 0.75f, 0.4f), Color.white);
            restartButton.onClick.AddListener(DoRestart);

            // Menu button
            menuButton = CreateButton(content.transform, "MAIN MENU", 22,
                new Vector2(0f, -170f), new Vector2(200f, 42f),
                new Color(0.35f, 0.35f, 0.45f), new Color(0.8f, 0.8f, 0.8f));
            menuButton.onClick.AddListener(DoMenu);

            // Also restart on R key
            panel.SetActive(false);
        }

        private void Update()
        {
            if (panel != null && panel.activeInHierarchy)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.R) || UnityEngine.Input.GetKeyDown(KeyCode.Return))
                {
                    DoRestart();
                }
            }
        }

        #region UI Helpers

        private static GameObject CreateCenteredBox(Transform parent, string name,
            float width, float height, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = color;

            return obj;
        }

        private static Text CreateText(Transform parent, string content, int fontSize,
            Vector2 position, Color color)
        {
            GameObject obj = new GameObject("Text_" + content.Substring(0, Mathf.Min(10, content.Length)),
                typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(420f, fontSize + 20);

            Text text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
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

            // Button text
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
