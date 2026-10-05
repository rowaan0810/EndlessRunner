// WebcamHUD.cs — Displays webcam feed with hand skeleton tracking overlay.
// Draws the 21 hand landmarks as dots and connections on top of the camera feed.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EndlessRunner.Input;
using EndlessRunner.Input.PoseDetection;

namespace EndlessRunner.UI
{
    public class WebcamHUD : MonoBehaviour
    {
        private RawImage webcamImage;
        private RectTransform webcamRect;
        private Text gestureLabel;
        private Text fingerCountText;
        private Image gestureIndicator;
        private GameObject hudPanel;

        // Skeleton drawing
        private List<Image> landmarkDots = new List<Image>();
        private List<Image> boneLine = new List<Image>();
        private const int MAX_LANDMARKS = 21;

        // Hand skeleton connections (pairs of landmark indices)
        private static readonly int[,] BONES = new int[,] {
            {0,1},{1,2},{2,3},{3,4},       // Thumb
            {0,5},{5,6},{6,7},{7,8},       // Index
            {0,9},{9,10},{10,11},{11,12},   // Middle
            {0,13},{13,14},{14,15},{15,16}, // Ring
            {0,17},{17,18},{18,19},{19,20}, // Pinky
            {5,9},{9,13},{13,17}            // Palm
        };

        // Colors
        private static readonly Color JumpGreen = new Color(0.1f, 1f, 0.3f, 1f);
        private static readonly Color DuckOrange = new Color(1f, 0.6f, 0.1f, 1f);
        private static readonly Color ThumbYellow = new Color(1f, 1f, 0.2f, 1f);
        private static readonly Color NeutralGrey = new Color(0.4f, 0.4f, 0.4f, 0.7f);
        private static readonly Color NeonCyan = new Color(0f, 0.95f, 1f, 1f);
        private static readonly Color DotColor = new Color(0f, 1f, 0.5f, 1f);
        private static readonly Color BoneColor = new Color(0f, 0.8f, 1f, 0.7f);

        private void Start()
        {
            BuildHUD();
        }

        private void Update()
        {
            if (hudPanel == null) return;

            var hgm = HandGestureManager.Instance;
            bool active = hgm != null && hgm.IsRunning && hgm.WebcamTexture != null;

            hudPanel.SetActive(active);
            if (!active) return;

            // Update webcam feed
            if (webcamImage != null && hgm.WebcamTexture != null)
                webcamImage.texture = hgm.WebcamTexture;

            // Draw hand skeleton
            DrawSkeleton(hgm);

            // Get gesture info from PoseInput
            var pi = FindAnyObjectByType<PoseInput>();
            HandGesture gesture = HandGesture.None;
            if (pi != null)
            {
                gesture = pi.CurrentGesture;
            }

            // Gesture indicator bar
            if (gestureIndicator != null)
            {
                gestureIndicator.color = gesture switch
                {
                    HandGesture.ThumbsUp => JumpGreen,
                    HandGesture.OpenPalm => DuckOrange,
                    HandGesture.Fist => NeutralGrey,
                    _ => NeutralGrey,
                };
            }

            // Gesture text
            if (gestureLabel != null)
            {
                (gestureLabel.text, gestureLabel.color) = gesture switch
                {
                    HandGesture.ThumbsUp => ("👍 THUMBS UP → JUMP", JumpGreen),
                    HandGesture.OpenPalm => ("✋ OPEN PALM → DUCK", DuckOrange),
                    HandGesture.Fist => ("✊ FIST", NeutralGrey),
                    _ => (hgm.HandDetected ? "✋ HAND DETECTED" : "NO HAND", hgm.HandDetected ? NeonCyan : NeutralGrey),
                };
            }

            // Finger count
            if (fingerCountText != null)
            {
                fingerCountText.text = ""; // Disabled as we use native gestures now
            }
        }

        private void DrawSkeleton(HandGestureManager hgm)
        {
            // Hide all dots and bones first
            foreach (var dot in landmarkDots) dot.gameObject.SetActive(false);
            foreach (var bone in boneLine) bone.gameObject.SetActive(false);

            if (hgm.RawLandmarks == null || hgm.RawLandmarks.Count == 0) return;

            var landmarks = hgm.RawLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count < 21) return;

            float w = webcamRect.rect.width;
            float h = webcamRect.rect.height;

            // Draw landmark dots
            for (int i = 0; i < Mathf.Min(landmarks.Count, MAX_LANDMARKS); i++)
            {
                if (i >= landmarkDots.Count) break;

                var dot = landmarkDots[i];
                dot.gameObject.SetActive(true);

                // Convert normalized coords to local rect coords
                // Mirror X to match the mirrored webcam display
                float lx = (1f - landmarks[i].x) * w - w / 2f;
                float ly = -(landmarks[i].y * h - h / 2f);

                dot.rectTransform.anchoredPosition = new Vector2(lx, ly);

                // Color fingertips differently
                bool isTip = (i == 4 || i == 8 || i == 12 || i == 16 || i == 20);
                dot.color = isTip ? ThumbYellow : DotColor;
            }

            // Draw bones (connections between landmarks)
            int boneCount = BONES.GetLength(0);
            for (int i = 0; i < boneCount && i < boneLine.Count; i++)
            {
                int a = BONES[i, 0], b = BONES[i, 1];
                if (a >= landmarks.Count || b >= landmarks.Count) continue;

                var bone = boneLine[i];
                bone.gameObject.SetActive(true);

                float ax = (1f - landmarks[a].x) * w - w / 2f;
                float ay = -(landmarks[a].y * h - h / 2f);
                float bx = (1f - landmarks[b].x) * w - w / 2f;
                float by = -(landmarks[b].y * h - h / 2f);

                // Position at midpoint, rotate to connect the two points
                float mx = (ax + bx) / 2f;
                float my = (ay + by) / 2f;
                float dx = bx - ax;
                float dy = by - ay;
                float len = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                bone.rectTransform.anchoredPosition = new Vector2(mx, my);
                bone.rectTransform.sizeDelta = new Vector2(len, 2f);
                bone.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void BuildHUD()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // === Main Panel (bottom-left corner) ===
            hudPanel = new GameObject("WebcamHUD", typeof(RectTransform));
            hudPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRT = hudPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0f, 0f);
            panelRT.anchorMax = new Vector2(0f, 0f);
            panelRT.pivot = new Vector2(0f, 0f);
            panelRT.anchoredPosition = new Vector2(10f, 10f);
            panelRT.sizeDelta = new Vector2(280f, 250f);

            Image bg = hudPanel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.06f, 0.9f);

            // === Webcam feed (mirrored) ===
            GameObject webcamObj = new GameObject("WebcamFeed", typeof(RectTransform));
            webcamObj.transform.SetParent(hudPanel.transform, false);
            webcamRect = webcamObj.GetComponent<RectTransform>();
            webcamRect.anchorMin = new Vector2(0.5f, 0.5f);
            webcamRect.anchorMax = new Vector2(0.5f, 0.5f);
            webcamRect.pivot = new Vector2(0.5f, 0.5f);
            webcamRect.anchoredPosition = new Vector2(0f, 20f);
            webcamRect.sizeDelta = new Vector2(260f, 165f);

            webcamImage = webcamObj.AddComponent<RawImage>();
            webcamImage.color = Color.white;
            webcamImage.uvRect = new Rect(1, 0, -1, 1); // Mirror

            // === Skeleton overlay (parented to webcam so coords align) ===
            GameObject skeletonParent = new GameObject("Skeleton", typeof(RectTransform));
            skeletonParent.transform.SetParent(webcamObj.transform, false);
            RectTransform skelRT = skeletonParent.GetComponent<RectTransform>();
            skelRT.anchorMin = Vector2.zero;
            skelRT.anchorMax = Vector2.one;
            skelRT.offsetMin = Vector2.zero;
            skelRT.offsetMax = Vector2.zero;

            // Create bone lines (must be behind dots)
            int boneCount = BONES.GetLength(0);
            for (int i = 0; i < boneCount; i++)
            {
                GameObject boneObj = new GameObject($"Bone_{i}", typeof(RectTransform));
                boneObj.transform.SetParent(skeletonParent.transform, false);
                RectTransform boneRT = boneObj.GetComponent<RectTransform>();
                boneRT.anchorMin = new Vector2(0.5f, 0.5f);
                boneRT.anchorMax = new Vector2(0.5f, 0.5f);
                boneRT.pivot = new Vector2(0.5f, 0.5f);
                boneRT.sizeDelta = new Vector2(20f, 2f);

                Image boneImg = boneObj.AddComponent<Image>();
                boneImg.color = BoneColor;
                boneImg.raycastTarget = false;
                boneObj.SetActive(false);
                boneLine.Add(boneImg);
            }

            // Create landmark dots
            for (int i = 0; i < MAX_LANDMARKS; i++)
            {
                GameObject dotObj = new GameObject($"Dot_{i}", typeof(RectTransform));
                dotObj.transform.SetParent(skeletonParent.transform, false);
                RectTransform dotRT = dotObj.GetComponent<RectTransform>();
                dotRT.anchorMin = new Vector2(0.5f, 0.5f);
                dotRT.anchorMax = new Vector2(0.5f, 0.5f);
                dotRT.pivot = new Vector2(0.5f, 0.5f);
                dotRT.sizeDelta = new Vector2(6f, 6f);

                Image dotImg = dotObj.AddComponent<Image>();
                dotImg.color = DotColor;
                dotImg.raycastTarget = false;
                dotObj.SetActive(false);
                landmarkDots.Add(dotImg);
            }

            // === Gesture indicator bar ===
            GameObject indObj = new GameObject("GestureBar", typeof(RectTransform));
            indObj.transform.SetParent(hudPanel.transform, false);
            RectTransform indRT = indObj.GetComponent<RectTransform>();
            indRT.anchorMin = new Vector2(0.5f, 0f);
            indRT.anchorMax = new Vector2(0.5f, 0f);
            indRT.pivot = new Vector2(0.5f, 0f);
            indRT.anchoredPosition = new Vector2(0f, 48f);
            indRT.sizeDelta = new Vector2(260f, 6f);

            gestureIndicator = indObj.AddComponent<Image>();
            gestureIndicator.color = NeutralGrey;

            // === Gesture label ===
            gestureLabel = CreateText(hudPanel, "GestureText", 14, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(260f, 22f));

            // === Finger count ===
            fingerCountText = CreateText(hudPanel, "FingerCount", 11, FontStyle.Normal, NeonCyan,
                new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(260f, 18f));

            // === Title ===
            var title = CreateText(hudPanel, "Title", 11, FontStyle.Bold, NeonCyan,
                new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(260f, 18f));
            title.text = "🎥 HAND TRACKING";

            hudPanel.SetActive(false);
        }

        private Text CreateText(GameObject parent, string name, int size, FontStyle style, Color color,
            Vector2 anchor, Vector2 pos, Vector2 sizeDelta)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, anchor.y);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;

            Text t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }
    }
}
