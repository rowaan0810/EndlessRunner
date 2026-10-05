// GameHUD.cs — Cyberpunk-styled in-game heads-up display.
// Shows score, distance, coins, speed, and input mode with neon aesthetics.

using UnityEngine;
using UnityEngine.UI;
using EndlessRunner.Core;
using EndlessRunner.Input;

namespace EndlessRunner.UI
{
    /// <summary>
    /// Cyberpunk-themed HUD during gameplay. Neon-styled score, distance,
    /// coins, speed indicator, and debug overlay for pose tracking.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        private Text scoreText;
        private Text distanceText;
        private Text coinText;
        private Text speedText;
        private Text modeText;
        private Text debugText;
        private Image speedBar;
        private Image speedBarBorder;
        private GameObject hudPanel;

        // Cyberpunk palette
        private static readonly Color NeonCyan = new Color(0f, 0.95f, 1f);
        private static readonly Color NeonMagenta = new Color(1f, 0.1f, 0.7f);
        private static readonly Color NeonGreen = new Color(0.1f, 1f, 0.5f);
        private static readonly Color GoldCoin = new Color(1f, 0.85f, 0.15f);
        private static readonly Color DimText = new Color(0.4f, 0.4f, 0.55f);
        private static readonly Color PanelBg = new Color(0.04f, 0.04f, 0.08f, 0.5f);

        private void Start()
        {
            CreateHUD();
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            bool visible = GameManager.Instance.State == GameState.Playing;
            if (hudPanel != null) hudPanel.SetActive(visible);
            if (!visible) return;

            // Update text
            if (scoreText != null)
                scoreText.text = $"{GameManager.Instance.TotalScore}";

            if (distanceText != null)
                distanceText.text = $"{GameManager.Instance.DistanceScore:F0}m";

            if (coinText != null)
                coinText.text = $"× {GameManager.Instance.CoinScore}";

            if (speedText != null && GameSpeed.Instance != null)
            {
                float spd = GameSpeed.Instance.Current;
                speedText.text = $"{spd:F0}";
                // Color shifts from green to cyan to magenta as speed increases
                float t = GameSpeed.Instance.NormalizedSpeed;
                speedText.color = Color.Lerp(NeonGreen, NeonMagenta, t);
            }

            if (speedBar != null && GameSpeed.Instance != null)
            {
                float t = GameSpeed.Instance.NormalizedSpeed;
                speedBar.fillAmount = t;
                speedBar.color = Color.Lerp(NeonGreen, NeonMagenta, t);
            }

            if (modeText != null && InputManager.Instance != null)
                modeText.text = $"[ {InputManager.Instance.GetModeName().ToUpper()} ]";

            if (debugText != null)
            {
                var pi = InputManager.Instance?.GetComponent<PoseInput>();

                if (pi != null && pi.isActiveAndEnabled && pi.IsAvailable)
                {
                    debugText.text = $"Gesture: {pi.CurrentGesture}\n" +
                                     $"Lane: {pi.DesiredLane}";
                }
                else
                {
                    debugText.text = "Tracking: OFF";
                }
            }
        }

        private void CreateHUD()
        {
            // Find or create Canvas
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

            // HUD Panel (full screen, no background)
            hudPanel = CreatePanel(canvas.transform, "HUDPanel");

            // ═══════════════════════════════════
            // TOP-CENTER: Score display
            // ═══════════════════════════════════
            GameObject scorePanel = CreateHUDPanel(hudPanel.transform, "ScorePanel",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(300f, 70f));

            Text scoreLabel = CreateText(scorePanel.transform, "ScoreLabel",
                "SCORE", 14, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -5f), new Vector2(200f, 20f));
            scoreLabel.color = DimText;

            scoreText = CreateText(scorePanel.transform, "ScoreText",
                "0", 38, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -22f), new Vector2(300f, 45f));
            scoreText.color = NeonCyan;
            scoreText.fontStyle = FontStyle.Bold;
            AddGlow(scoreText.gameObject, NeonCyan);

            // ═══════════════════════════════════
            // TOP-LEFT: Distance + Coins
            // ═══════════════════════════════════
            GameObject leftPanel = CreateHUDPanel(hudPanel.transform, "LeftPanel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(15f, -10f), new Vector2(200f, 80f));

            distanceText = CreateText(leftPanel.transform, "DistanceText",
                "0m", 28, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -8f), new Vector2(180f, 35f));
            distanceText.color = new Color(0.7f, 0.85f, 1f);
            distanceText.fontStyle = FontStyle.Bold;
            AddGlow(distanceText.gameObject, new Color(0.3f, 0.5f, 0.8f));

            // Coin icon + text
            Text coinIcon = CreateText(leftPanel.transform, "CoinIcon",
                "◆", 22, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, -42f), new Vector2(30f, 30f));
            coinIcon.color = GoldCoin;

            coinText = CreateText(leftPanel.transform, "CoinText",
                "× 0", 22, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(32f, -44f), new Vector2(150f, 30f));
            coinText.color = GoldCoin;
            coinText.fontStyle = FontStyle.Bold;

            // ═══════════════════════════════════
            // TOP-RIGHT: Speed indicator
            // ═══════════════════════════════════
            GameObject rightPanel = CreateHUDPanel(hudPanel.transform, "RightPanel",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-15f, -10f), new Vector2(180f, 75f));

            Text speedLabel = CreateText(rightPanel.transform, "SpeedLabel",
                "SPD", 12, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -8f), new Vector2(60f, 18f));
            speedLabel.color = DimText;

            speedText = CreateText(rightPanel.transform, "SpeedText",
                "10", 30, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -22f), new Vector2(80f, 35f));
            speedText.color = NeonGreen;
            speedText.fontStyle = FontStyle.Bold;

            // Speed bar background
            GameObject speedBarBg = CreateImage(rightPanel.transform, "SpeedBarBg",
                new Color(0.15f, 0.15f, 0.2f, 0.8f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -60f), new Vector2(156f, 8f));

            // Speed bar border
            Outline barOutline = speedBarBg.AddComponent<Outline>();
            barOutline.effectColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            barOutline.effectDistance = new Vector2(1f, -1f);

            // Speed bar fill
            GameObject speedBarFill = CreateImage(speedBarBg.transform, "SpeedBarFill",
                NeonGreen,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(156f, 8f));
            speedBar = speedBarFill.GetComponent<Image>();
            speedBar.type = Image.Type.Filled;
            speedBar.fillMethod = Image.FillMethod.Horizontal;
            speedBar.fillAmount = 0f;

            // ═══════════════════════════════════
            // BOTTOM-LEFT: Input mode
            // ═══════════════════════════════════
            modeText = CreateText(hudPanel.transform, "ModeText",
                "[ KEYBOARD ]", 14, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 15f), new Vector2(250f, 24f));
            modeText.color = DimText;

            // ═══════════════════════════════════
            // CENTER-LEFT: Debug text (pose tracking)
            // ═══════════════════════════════════
            debugText = CreateText(hudPanel.transform, "DebugText",
                "", 14, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(20f, 0f), new Vector2(300f, 400f));
            debugText.color = new Color(0.7f, 0.7f, 0.8f, 0.7f);
            AddGlow(debugText.gameObject, new Color(0f, 0f, 0f, 0.3f));
        }

        #region UI Helpers

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return obj;
        }

        private static GameObject CreateHUDPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            // Semi-transparent dark background
            Image img = obj.AddComponent<Image>();
            img.color = PanelBg;

            return obj;
        }

        private static Text CreateText(Transform parent, string name, string content,
            int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Text text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static GameObject CreateImage(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static void AddGlow(GameObject obj, Color color)
        {
            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(color.r, color.g, color.b, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        #endregion
    }
}
