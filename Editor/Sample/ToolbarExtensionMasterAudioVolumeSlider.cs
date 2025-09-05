using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yusuke57.UnityToolbarExtension.Editor.Sample
{
    public class ToolbarExtensionMasterAudioVolumeSlider : IToolbarElementRegister
    {
        private string MasterAudioVolumeValueText => $"{AudioListener.volume * 100:0}";
        public ToolbarElementLayoutType LayoutType => ToolbarElementLayoutType.LeftSideLeftAlign;

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

            var slider = new Slider(0f, 1f);
            slider.style.width = 70;
            slider.style.height = 18;
            sliderContainer.Add(slider);

            var valueLabel = new Label(MasterAudioVolumeValueText);
            valueLabel.style.width = 16;
            valueLabel.style.height = 18;
            valueLabel.style.marginLeft = 4;
            valueLabel.style.fontSize = 11;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.color = new Color(0.8f, 0.8f, 0.8f);

            var resetButton = new EditorToolbarButton(
                (Texture2D) EditorGUIUtility.IconContent("d_Profiler.Audio").image,
                () => slider.value = 1);
            resetButton.style.width = 18;
            resetButton.style.height = 18;
            resetButton.style.paddingTop = 0;
            resetButton.style.paddingBottom = 0;
            resetButton.style.paddingLeft = 0;
            resetButton.style.paddingRight = 0;
            resetButton.style.minWidth = 18;

            slider.RegisterValueChangedCallback(evt => 
            {
                AudioListener.volume = evt.newValue;
                valueLabel.text = MasterAudioVolumeValueText;
            });
            slider.value = 1;

            container.Add(resetButton);
            container.Add(sliderContainer);
            container.Add(valueLabel);

            return container;
        }
    }
}