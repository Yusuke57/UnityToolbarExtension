using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yusuke57.UnityToolbarExtension.Editor.Sample
{
    public class ToolbarExtensionTimeScaleSlider : IToolbarElementRegister
    {
        private string TimeScaleValueText => $"×{Time.timeScale:F1}";
        public ToolbarElementLayoutType LayoutType => ToolbarElementLayoutType.RightSideLeftAlign;

        private const float MaxTimeScale = 10f;
        private const float DefaultTimeScaleRange = 0.1f;

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

            var slider = new Slider(-1f, 1f);
            slider.style.width = 70;
            slider.style.height = 18;

            var centerLine = new VisualElement();
            centerLine.style.position = Position.Absolute;
            centerLine.style.left = slider.style.width.value.value / 2f + 3f;
            centerLine.style.top = 2;
            centerLine.style.bottom = 0;
            centerLine.style.width = 1;
            centerLine.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            centerLine.style.marginLeft = -0.5f;

            sliderContainer.Add(centerLine);
            sliderContainer.Add(slider);

            var valueLabel = new Label(TimeScaleValueText);
            valueLabel.style.width = 16;
            valueLabel.style.height = 18;
            valueLabel.style.marginLeft = 4;
            valueLabel.style.fontSize = 11;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.color = new Color(0.8f, 0.8f, 0.8f);

            var resetButton = new EditorToolbarButton(
                (Texture2D) EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image,
                () => slider.value = 0);
            resetButton.style.width = 18;
            resetButton.style.height = 18;
            resetButton.style.paddingTop = 0;
            resetButton.style.paddingBottom = 0;
            resetButton.style.paddingLeft = 0;
            resetButton.style.paddingRight = 0;
            resetButton.style.minWidth = 18;

            slider.RegisterValueChangedCallback(evt => 
            {
                Time.timeScale = ConvertSliderValueToTimeScale(evt.newValue);
                valueLabel.text = TimeScaleValueText;
            });
            slider.value = 0;

            container.Add(resetButton);
            container.Add(sliderContainer);
            container.Add(valueLabel);

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
    }
}