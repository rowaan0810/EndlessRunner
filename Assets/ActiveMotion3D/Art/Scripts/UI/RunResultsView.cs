using UnityEngine;
using UnityEngine.UI;

namespace ActiveMotion3D.EndlessRunner.UI
{
    [RequireComponent(typeof(Canvas))]
    public sealed class RunResultsView : MonoBehaviour
    {
        [Header("Run summary")]
        public RectTransform frame;
        public Text score;
        public Text best;
        public Text distance;
        public Text coins;
        public Text time;
        public Button playAgain;
        public Button mainMenu;

        [Header("Stand Coach")]
        public Text standName;
        public Text voiceStatus;
        public Text caption;
        public Text captionButtonLabel;
        public Button replay;
        public Button captionButton;
        public RawImage portrait;
        public GameObject d4cModel;
        public GameObject killerQueenModel;
        public Shader portraitShader;
        public Color stageColor = new Color32(41, 37, 55, 255);

        private void OnEnable() => FitFrame();
        private void OnRectTransformDimensionsChange() => FitFrame();

        private void FitFrame()
        {
            if (frame == null) return;
            Rect canvas = ((RectTransform)transform).rect;
            if (canvas.width <= 0 || canvas.height <= 0) return;
            float scale = Mathf.Min(1f, (canvas.width - 32f) / frame.rect.width,
                (canvas.height - 32f) / frame.rect.height);
            frame.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }
    }
}
