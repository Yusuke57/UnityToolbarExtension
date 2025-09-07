using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor.Register
{
    public class ToolbarExtensionTimeScaleSlider : IToolbarElement
    {
        private static Slider _slider;
        private static Label _currentValueLabel;
        private static float _lastTimeScale = 1f;

        private static string TimeScaleValueText => $"×{Time.timeScale:F1}";
        public ToolbarElementLayoutType DefaultLayoutType => ToolbarElementLayoutType.RightSideLeftAlign;

        private const float MaxTimeScale = 10f;
        private const float DefaultTimeScaleRange = 0.13f;

        static ToolbarExtensionTimeScaleSlider()
        {
            EditorApplication.update -= UpdateTimeScaleDisplay;
            EditorApplication.update += UpdateTimeScaleDisplay;
        }

        private static void UpdateTimeScaleDisplay()
        {
            if (_slider == null || _currentValueLabel == null)
            {
                return;
            }

            if (Mathf.Abs(Time.timeScale - _lastTimeScale) > 0.001f)
            {
                _lastTimeScale = Time.timeScale;
                _slider.SetValueWithoutNotify(ConvertTimeScaleToSliderValue(_lastTimeScale));
                _currentValueLabel.text = TimeScaleValueText;
            }
        }

        public VisualElement CreateElement()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginLeft = 10;
            container.style.marginRight = 10;
            container.style.alignSelf = Align.Center;
            container.style.height = 18;

            var sliderContainer = new VisualElement();
            sliderContainer.style.position = Position.Relative;
            sliderContainer.style.flexGrow = 1;
            sliderContainer.style.height = 18;

            _slider = new Slider(-1f, 1f);
            _slider.style.width = 70;
            _slider.style.height = 18;

            var centerLine = new VisualElement();
            centerLine.style.position = Position.Absolute;
            centerLine.style.left = _slider.style.width.value.value / 2f + 3f;
            centerLine.style.top = 2;
            centerLine.style.bottom = 0;
            centerLine.style.width = 1;
            centerLine.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            centerLine.style.marginLeft = -0.5f;

            sliderContainer.Add(centerLine);
            sliderContainer.Add(_slider);

            _currentValueLabel = new Label(TimeScaleValueText);
            _currentValueLabel.style.width = 16;
            _currentValueLabel.style.height = 18;
            _currentValueLabel.style.marginLeft = 4;
            _currentValueLabel.style.fontSize = 11;
            _currentValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _currentValueLabel.style.color = new Color(0.8f, 0.8f, 0.8f);

            var resetButton = new EditorToolbarButton(
                (Texture2D) EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image,
                () => _slider.value = 0);
            resetButton.tooltip = "Reset time scale";
            resetButton.style.width = 18;
            resetButton.style.height = 18;
            resetButton.style.paddingTop = 0;
            resetButton.style.paddingBottom = 0;
            resetButton.style.paddingLeft = 0;
            resetButton.style.paddingRight = 0;
            resetButton.style.minWidth = 18;

            _slider.RegisterValueChangedCallback(evt =>
            {
                _lastTimeScale = ConvertSliderValueToTimeScale(evt.newValue);
                Time.timeScale = _lastTimeScale;
                _currentValueLabel.text = TimeScaleValueText;
            });
            _slider.value = ConvertTimeScaleToSliderValue(Time.timeScale);

            container.Add(resetButton);
            container.Add(sliderContainer);
            container.Add(_currentValueLabel);

            return container;
        }

        private static float ConvertSliderValueToTimeScale(float sliderValue)
        {
            if (sliderValue > 0)
            {
                if (sliderValue <= DefaultTimeScaleRange)
                {
                    return 1f;
                }

                var value = (sliderValue - DefaultTimeScaleRange) / (1f - DefaultTimeScaleRange);
                value = Mathf.Pow(value, 2); // 二次関数で増やす
                return 1f + value * (MaxTimeScale - 1f); // 中央より右側では1からMaxTimeScaleまでの範囲
            }
            else
            {
                if (sliderValue >= -DefaultTimeScaleRange)
                {
                    return 1f;
                }

                var value = (sliderValue + DefaultTimeScaleRange) / (1f - DefaultTimeScaleRange);
                return 1f + value; // 中央より左側では0から1までの範囲
            }
        }

        private static float ConvertTimeScaleToSliderValue(float timeScale)
        {
            if (Mathf.Approximately(timeScale, 1f))
            {
                return 0f;
            }

            if (timeScale > 1f)
            {
                // 1からMaxTimeScaleへの逆変換
                var normalizedValue = (timeScale - 1f) / (MaxTimeScale - 1f);
                var value = Mathf.Sqrt(normalizedValue); // 二次関数の逆変換
                return DefaultTimeScaleRange + value * (1f - DefaultTimeScaleRange);
            }
            else
            {
                // 0から1への逆変換
                var value = timeScale - 1f;
                return value * (1f - DefaultTimeScaleRange) - DefaultTimeScaleRange;
            }
        }
    }
}