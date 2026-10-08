// MainMenu.cs — Cyberpunk-styled main menu with neon aesthetics.
// Shows title, input mode selector with descriptions, and start/quit buttons.

using UnityEngine;
using UnityEngine.UI;
using EndlessRunner.Core;
using EndlessRunner.Input;
using EndlessRunner.Player;

namespace EndlessRunner.UI
{
    /// <summary>
    /// Cyberpunk-themed main menu with neon glowing UI elements.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private GameObject panel;
        private Text modeLabel;
        private Text modeDescription;
        private Text characterLabel;
        private InputMode selectedMode = InputMode.Keyboard;

        // Cyberpunk palette
        private static readonly Color NeonCyan = new Color(0f, 0.95f, 1f);
        private static readonly Color NeonMagenta = new Color(1f, 0.1f, 0.7f);
        private static readonly Color NeonGreen = new Color(0.1f, 1f, 0.5f);
        private static readonly Color DarkBg = new Color(0.04f, 0.04f, 0.08f, 0.96f);
        private static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.14f, 0.9f);
        private static readonly Color DimText = new Color(0.4f, 0.4f, 0.55f);
        private static readonly Color BrightText = new Color(0.9f, 0.9f, 0.95f);

        private void Start()
        {
            CreateMenuUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart.AddListener(HideMenu);
            }

            Invoke(nameof(ShowMenu), 0.1f);
        }

        public void ShowMenu()
        {
            if (panel != null) panel.SetActive(true);

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
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetMode(selectedMode);
            }

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.ResetPlayer();

            var collision = FindAnyObjectByType<PlayerCollision>();
            if (collision != null) collision.ResetCollision();

            GameManager.Instance?.StartGame();
        }

        private void CycleMode()
        {
            selectedMode = selectedMode switch
            {
                InputMode.Keyboard => InputMode.Webcam,
                InputMode.Webcam => InputMode.Keyboard,
                _ => InputMode.Keyboard
            };

            UpdateModeLabel();
        }

        private void UpdateModeLabel()
        {
            if (modeLabel == null) return;

            string modeName = selectedMode switch
            {
                InputMode.Keyboard => "KEYBOARD",
                InputMode.Webcam => "WEBCAM",
                _ => "KEYBOARD"
            };

            modeLabel.text = modeName;
            modeLabel.color = selectedMode switch
            {
                InputMode.Keyboard => NeonCyan,
                InputMode.Webcam => NeonMagenta,
                _ => NeonCyan
            };

            if (modeDescription != null)
            {
                modeDescription.text = selectedMode switch
                {
                    InputMode.Keyboard => "Arrow Keys / WASD to move  |  Space: Jump  |  S: Duck",
                    InputMode.Webcam => "☝️ Right Index Finger: Jump  |  ✋ Open Palm: Duck  |  Lean: Lanes",
                    _ => ""
                };
            }
        }

        private void CycleCharacter()
        {
            var charSelector = FindAnyObjectByType<CharacterSelector>();
            if (charSelector != null)
            {
                charSelector.CycleCharacter();
                UpdateCharacterLabel(charSelector);
            }
        }

        private void UpdateCharacterLabel(CharacterSelector selector = null)
        {
            if (characterLabel == null) return;
            
            if (selector == null) selector = FindAnyObjectByType<CharacterSelector>();
            
            if (selector != null)
            {
                characterLabel.text = selector.GetCurrentCharacterName();
                characterLabel.color = NeonCyan;
            }
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

            // Full-screen background image
            panel = new GameObject("MainMenuPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image overlay = panel.AddComponent<Image>();
            
            // Try to load the Mega-City background image
            Texture2D bgTex = Resources.Load<Texture2D>("UI/MenuBackground");
            if (bgTex != null)
            {
                overlay.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
                overlay.color = new Color(0.6f, 0.6f, 0.7f, 1f); // Slightly dim it so UI pops
            }
            else
            {
                overlay.color = DarkBg; // Fallback
            }

            // === Centered content card with subtle border ===
            GameObject card = CreateCard(panel.transform, "MenuCard", 520f, 520f, PanelBg);

            // Accent line at top of card
            CreateAccentLine(card.transform, new Vector2(0f, 240f), 480f, 3f, NeonCyan);

            // Title — "ENDLESS RUNNER" with glow
            Text title = CreateText(card.transform, "ENDLESS RUNNER", 52,
                new Vector2(0f, 190f), NeonCyan, FontStyle.Bold);
            AddOutline(title.gameObject, NeonCyan * 0.3f);

            // Subtitle
            CreateText(card.transform, "// CYBERPUNK EDITION", 18,
                new Vector2(0f, 145f), DimText, FontStyle.Italic);

            // Accent line separator
            CreateAccentLine(card.transform, new Vector2(0f, 120f), 380f, 1f, DimText);

            // === START BUTTON ===
            Button startBtn = CreateNeonButton(card.transform, "► START GAME", 28,
                new Vector2(0f, 65f), new Vector2(340f, 60f),
                NeonGreen, DarkBg);
            startBtn.onClick.AddListener(DoStartGame);

            // ==========================================
            // LAYOUT: Two columns for Settings
            // Left Column: Input Mode
            // Right Column: Character Model
            // ==========================================

            // LEFT COLUMN (Input)
            CreateText(card.transform, "INPUT MODE", 14,
                new Vector2(-120f, 10f), DimText, FontStyle.Normal);

            modeLabel = CreateText(card.transform, "", 22,
                new Vector2(-120f, -20f), NeonCyan, FontStyle.Bold);

            Button modeBtn = CreateNeonButton(card.transform, "◄ CHANGE ►", 14,
                new Vector2(-120f, -60f), new Vector2(160f, 34f),
                NeonMagenta, DarkBg);
            modeBtn.onClick.AddListener(CycleMode);

            // RIGHT COLUMN (Character)
            CreateText(card.transform, "CHARACTER", 14,
                new Vector2(120f, 10f), DimText, FontStyle.Normal);

            characterLabel = CreateText(card.transform, "", 18,
                new Vector2(120f, -20f), NeonCyan, FontStyle.Bold);

            Button charBtn = CreateNeonButton(card.transform, "◄ CHANGE ►", 14,
                new Vector2(120f, -60f), new Vector2(160f, 34f),
                new Color(1f, 0.85f, 0.2f), DarkBg); // Gold button for chars
            charBtn.onClick.AddListener(CycleCharacter);

            // Initialize both labels
            UpdateModeLabel();
            UpdateCharacterLabel();

            // Mode description (centered below columns)
            modeDescription = CreateText(card.transform, "", 14,
                new Vector2(0f, -100f), new Color(0.55f, 0.55f, 0.65f), FontStyle.Normal);
            UpdateModeLabel(); // Call again to set description text correctly

            // Accent line separator
            CreateAccentLine(card.transform, new Vector2(0f, -135f), 380f, 1f, DimText);

            // Quick start hint
            CreateText(card.transform, "PRESS  [ENTER]  OR  [SPACE]  TO  START", 14,
                new Vector2(0f, -160f), DimText, FontStyle.Normal);

            // === QUIT BUTTON ===
            Button quitBtn = CreateNeonButton(card.transform, "QUIT", 16,
                new Vector2(0f, -200f), new Vector2(140f, 36f),
                new Color(0.6f, 0.2f, 0.2f), DarkBg);
            quitBtn.onClick.AddListener(DoQuit);

            // Accent line at bottom of card
            CreateAccentLine(card.transform, new Vector2(0f, -240f), 480f, 3f, NeonMagenta);

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

            // Subtle outline
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.15f);
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
            GameObject obj = new GameObject("Text", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(480f, fontSize + 20);

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
            Vector2 position, Vector2 size, Color accentColor, Color bgColor)
        {
            GameObject obj = new GameObject("Btn_" + label, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            // Dark background with colored border
            Image img = obj.AddComponent<Image>();
            img.color = new Color(bgColor.r + 0.05f, bgColor.g + 0.05f, bgColor.b + 0.1f, 0.95f);

            // Neon outline
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = obj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.3f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
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
            text.color = accentColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return btn;
        }

        private static void AddOutline(GameObject obj, Color color)
        {
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        #endregion
    }
}
