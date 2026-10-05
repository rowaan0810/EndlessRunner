// GameOverScreen.cs — Cyberpunk-styled game over overlay with score summary.

using UnityEngine;
using UnityEngine.UI;
using EndlessRunner.Core;
using EndlessRunner.Player;

namespace EndlessRunner.UI
{
    /// <summary>
    /// Cyberpunk-themed game over screen with neon aesthetics,
    /// score summary, high score detection, and restart/menu buttons.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        private GameObject panel;
        private Text titleText;
        private Text scoreLabel;
        private Text scoreText;
        private Text highScoreText;
        private Text statsText;
        private Button restartButton;
        private Button menuButton;

        // Cyberpunk palette
        private static readonly Color NeonCyan = new Color(0f, 0.95f, 1f);
        private static readonly Color NeonMagenta = new Color(1f, 0.1f, 0.7f);
        private static readonly Color NeonGreen = new Color(0.1f, 1f, 0.5f);
        private static readonly Color NeonRed = new Color(1f, 0.2f, 0.3f);
        private static readonly Color GoldCoin = new Color(1f, 0.85f, 0.15f);
        private static readonly Color DarkBg = new Color(0.02f, 0.02f, 0.05f, 0.85f);
        private static readonly Color PanelBg = new Color(0.06f, 0.06f, 0.12f, 0.95f);
        private static readonly Color DimText = new Color(0.4f, 0.4f, 0.55f);

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
                if (isNewHigh)
                {
                    highScoreText.text = "★ NEW HIGH SCORE ★";
                    highScoreText.color = GoldCoin;
                }
                else
                {
                    highScoreText.text = $"BEST: {gm.HighScore}";
                    highScoreText.color = DimText;
                }
            }

            if (statsText != null)
            {
                statsText.text = $"DISTANCE  {gm.DistanceScore:F0}m   ·   " +
                                 $"COINS  {gm.CoinScore}   ·   " +
                                 $"TIME  {gm.TimeSurvived:F1}s";
            }
        }

        private void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void DoRestart()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.ResetPlayer();

            var collision = FindAnyObjectByType<PlayerCollision>();
            if (collision != null) collision.ResetCollision();

            GameManager.Instance?.RestartGame();
        }

        private void DoMenu()
        {
            Hide();
            GameManager.Instance?.ReturnToMenu();
            var mainMenu = FindAnyObjectByType<MainMenu>();
            if (mainMenu != null) mainMenu.ShowMenu();
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

        private void CreateGameOverUI()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Full-screen dark overlay
            panel = new GameObject("GameOverPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image overlay = panel.AddComponent<Image>();
            overlay.color = DarkBg;

            // Content card
            GameObject card = CreateCard(panel.transform, "GameOverCard", 480f, 440f, PanelBg);

            // Top accent line (red — danger!)
            CreateAccentLine(card.transform, new Vector2(0f, 200f), 440f, 3f, NeonRed);

            // "GAME OVER" title
            titleText = CreateText(card.transform, "GAME OVER", 48,
                new Vector2(0f, 155f), NeonRed, FontStyle.Bold);
            AddGlow(titleText.gameObject, NeonRed);

            // Separator
            CreateAccentLine(card.transform, new Vector2(0f, 120f), 350f, 1f, DimText);

            // Score label
            scoreLabel = CreateText(card.transform, "FINAL SCORE", 14,
                new Vector2(0f, 95f), DimText, FontStyle.Normal);

            // Score value — big and bright
            scoreText = CreateText(card.transform, "0", 64,
                new Vector2(0f, 50f), NeonCyan, FontStyle.Bold);
            AddGlow(scoreText.gameObject, NeonCyan);

            // High score
            highScoreText = CreateText(card.transform, "BEST: 0", 20,
                new Vector2(0f, -5f), DimText, FontStyle.Normal);

            // Stats line
            statsText = CreateText(card.transform, "DISTANCE  0m   ·   COINS  0   ·   TIME  0s", 15,
                new Vector2(0f, -40f), new Color(0.5f, 0.5f, 0.6f), FontStyle.Normal);

            // Separator
            CreateAccentLine(card.transform, new Vector2(0f, -70f), 350f, 1f, DimText);

            // Restart button
            restartButton = CreateNeonButton(card.transform, "► PLAY AGAIN", 26,
                new Vector2(0f, -110f), new Vector2(300f, 58f),
                NeonGreen);
            restartButton.onClick.AddListener(DoRestart);

            // Menu button
            menuButton = CreateNeonButton(card.transform, "MAIN MENU", 18,
                new Vector2(0f, -170f), new Vector2(200f, 40f),
                DimText);
            menuButton.onClick.AddListener(DoMenu);

            // Bottom accent line
            CreateAccentLine(card.transform, new Vector2(0f, -200f), 440f, 3f, NeonMagenta);

            // Hint text
            CreateText(card.transform, "PRESS  [R]  OR  [ENTER]  TO  RESTART", 12,
                new Vector2(0f, -215f), new Color(0.3f, 0.3f, 0.4f), FontStyle.Normal);

            panel.SetActive(false);
        }

        #region UI Helpers

        private static GameObject CreateCard(Transform parent, string name,
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

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.2f);
            outline.effectDistance = new Vector2(2f, -2f);

            return obj;
        }

        private static void CreateAccentLine(Transform parent, Vector2 position, float width, float height, Color color)
        {
            GameObject obj = new GameObject("AccentLine", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(width, height);

            Image img = obj.AddComponent<Image>();
            img.color = color;
        }

        private static Text CreateText(Transform parent, string content, int fontSize,
            Vector2 position, Color color, FontStyle style = FontStyle.Normal)
        {
            string safeName = content.Length > 12 ? content.Substring(0, 12) : content;
            GameObject obj = new GameObject("Text_" + safeName, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(450f, fontSize + 20);

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
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(2f, -2f);

            return text;
        }

        private static Button CreateNeonButton(Transform parent, string label, int fontSize,
            Vector2 position, Vector2 size, Color accentColor)
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
            img.color = new Color(0.08f, 0.08f, 0.14f, 0.95f);

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = obj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.4f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
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
            text.color = accentColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return btn;
        }

        private static void AddGlow(GameObject obj, Color color)
        {
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.4f);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        #endregion
    }
}
