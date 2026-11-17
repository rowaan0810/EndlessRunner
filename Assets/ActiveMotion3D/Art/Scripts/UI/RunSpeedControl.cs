using ActiveMotion3D.EndlessRunner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ActiveMotion3D.EndlessRunner.UI
{
    internal sealed class RunSpeedControl : MonoBehaviour
    {
        private Slider slider;
        private Text label;
        private bool hasUnsavedChanges;

        internal static void Create(Transform parent, Vector2 anchor, Vector2 position, float width,
            Font font, Color surface, Color accent)
        {
            var root = new GameObject("RunSpeedControl", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 56f);
            var background = root.GetComponent<Image>();
            background.color = surface;
            background.raycastTarget = false;

            var label = DefaultControls.CreateText(default).GetComponent<Text>();
            var labelRect = label.rectTransform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -15f);
            labelRect.sizeDelta = new Vector2(width - 24f, 26f);
            label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            var slider = DefaultControls.CreateSlider(default).GetComponent<Slider>();
            var sliderRect = (RectTransform)slider.transform;
            sliderRect.SetParent(rect, false);
            sliderRect.anchoredPosition = new Vector2(0f, -13f);
            sliderRect.sizeDelta = new Vector2(width - 24f, 24f);
            slider.fillRect.GetComponent<Image>().color = accent;
            slider.targetGraphic.color = Color.white;
            slider.transform.Find("Background").GetComponent<Image>().color = new Color32(79, 72, 93, 255);
            slider.wholeNumbers = true;
            slider.maxValue = GameSpeed.HighestFixedSpeed / GameSpeed.FixedSpeedStep;
            var control = root.AddComponent<RunSpeedControl>();
            control.slider = slider;
            control.label = label;
            slider.onValueChanged.AddListener(control.SelectSpeed);
            control.Refresh(GameSpeed.SelectedFixedSpeed);
        }

        private void OnEnable()
        {
            GameSpeed.FixedSpeedChanged += Refresh;
            if (slider != null) Refresh(GameSpeed.SelectedFixedSpeed);
        }

        private void OnDisable()
        {
            GameSpeed.FixedSpeedChanged -= Refresh;
            if (!hasUnsavedChanges) return;
            PlayerPrefs.Save();
            hasUnsavedChanges = false;
        }

        private void SelectSpeed(float step)
        {
            hasUnsavedChanges = true;
            GameSpeed.SelectedFixedSpeed = (int)step * GameSpeed.FixedSpeedStep;
        }

        private void Refresh(int fixedSpeed)
        {
            slider.SetValueWithoutNotify(fixedSpeed / GameSpeed.FixedSpeedStep);
            label.text = fixedSpeed == 0 ? "Run speed  ·  Auto" : $"Run speed  ·  {fixedSpeed} m/s";
        }
    }
}
