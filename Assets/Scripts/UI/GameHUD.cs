// GameHUD.cs — In-game heads-up display.
// Shows score, distance, coins, speed, and input mode.

using UnityEngine;
using UnityEngine.UI;
using EndlessRunner.Core;
using EndlessRunner.Input;

namespace EndlessRunner.UI
{
    /// <summary>
    /// Real-time HUD during gameplay. Shows score, distance, coins,
    /// speed bar, and current input mode.
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
        private GameObject hudPanel;

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
                scoreText.text = $"SCORE: {GameManager.Instance.TotalScore}";

            if (distanceText != null)
                distanceText.text = $"{GameManager.Instance.DistanceScore:F0}m";

            if (coinText != null)
                coinText.text = $"x {GameManager.Instance.CoinScore}";

            if (speedText != null && GameSpeed.Instance != null)
                speedText.text = $"SPEED: {GameSpeed.Instance.Current:F0}";

            if (speedBar != null && GameSpeed.Instance != null)
                speedBar.fillAmount = GameSpeed.Instance.NormalizedSpeed;

            if (modeText != null && InputManager.Instance != null)
                modeText.text = InputManager.Instance.GetModeName();

            if (debugText != null)
            {
                var pi = InputManager.Instance?.GetComponent<PoseInput>();
                var emi = InputManager.Instance?.GetComponent<EasyModeInput>();
                var res = new EndlessRunner.Input.PoseDetection.PoseResult();
                bool hasRes = false;

                if (pi != null && pi.isActiveAndEnabled && pi.IsAvailable) { res = pi.GetLastResult(); hasRes = true; }
                else if (emi != null && emi.isActiveAndEnabled && emi.IsAvailable) { res = emi.GetLastResult(); hasRes = true; }

                if (hasRes && res.tracking)
                {
                    debugText.text = $"Ready: {res.ready}\n" +
                                     $"HipLift: {res.hipLift:F2} / {res.jumpThresh:F2}\n" +
                                     $"HipVel: {res.hipVel:F2}\n" +
                                     $"NoseDrop: {res.noseDrop:F2} / {res.duckThresh:F2}\n" +
                                     $"Ankles: {(res.anklesOk ? "OK" : "HIDDEN")}";
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

            // HUD Panel
            hudPanel = CreatePanel(canvas.transform, "HUDPanel");

            // Score — top center
            scoreText = CreateText(hudPanel.transform, "ScoreText",
                "SCORE: 0", 36, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -15f), new Vector2(400f, 50f));
            scoreText.color = Color.white;
            AddShadow(scoreText.gameObject);

            // Distance — top left
            distanceText = CreateText(hudPanel.transform, "DistanceText",
                "0m", 28, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -15f), new Vector2(200f, 40f));
            distanceText.color = new Color(0.8f, 0.9f, 1f);
            AddShadow(distanceText.gameObject);

            // Coins — top left below distance
            coinText = CreateText(hudPanel.transform, "CoinText",
                "x 0", 24, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -55f), new Vector2(200f, 35f));
            coinText.color = new Color(1f, 0.85f, 0.2f);
            AddShadow(coinText.gameObject);

            // Coin icon (text-based placeholder)
            Text coinIcon = CreateText(hudPanel.transform, "CoinIcon",
                "●", 28, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(2f, -52f), new Vector2(30f, 35f));
            coinIcon.color = new Color(1f, 0.85f, 0.2f);

            // Speed — top right
            speedText = CreateText(hudPanel.transform, "SpeedText",
                "SPEED: 10", 22, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -15f), new Vector2(200f, 35f));
            speedText.color = new Color(0.7f, 0.9f, 0.7f);
            AddShadow(speedText.gameObject);

            // Speed bar background
            GameObject speedBarBg = CreateImage(hudPanel.transform, "SpeedBarBg",
                new Color(0.2f, 0.2f, 0.2f, 0.6f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -50f), new Vector2(180f, 12f));

            // Speed bar fill
            GameObject speedBarFill = CreateImage(speedBarBg.transform, "SpeedBarFill",
                new Color(0.3f, 0.9f, 0.4f),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(180f, 12f));
            speedBar = speedBarFill.GetComponent<Image>();
            speedBar.type = Image.Type.Filled;
            speedBar.fillMethod = Image.FillMethod.Horizontal;
            speedBar.fillAmount = 0f;

            // Input mode — bottom left
            modeText = CreateText(hudPanel.transform, "ModeText",
                "Keyboard", 18, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 15f), new Vector2(200f, 30f));
            modeText.color = new Color(0.6f, 0.6f, 0.7f);

            // Debug Text — center left
            debugText = CreateText(hudPanel.transform, "DebugText",
                "", 16, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(20f, 0f), new Vector2(300f, 400f));
            debugText.color = new Color(1f, 1f, 1f, 0.8f);
            AddShadow(debugText.gameObject);
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

        private static void AddShadow(GameObject obj)
        {
            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        #endregion
    }
}
