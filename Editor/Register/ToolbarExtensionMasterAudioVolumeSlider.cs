using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor.Register
{
    public class ToolbarExtensionMasterAudioVolumeSlider : IToolbarElement
    {
        private static Slider _slider;
        private static Label _currentValueLabel;
        private static float _lastAudioVolume = 1f;

        private static string MasterAudioVolumeValueText => $"{AudioListener.volume * 100:0}";
        public ToolbarElementLayoutType DefaultLayoutType => ToolbarElementLayoutType.LeftSideLeftAlign;

        static ToolbarExtensionMasterAudioVolumeSlider()
        {
            EditorApplication.update -= UpdateAudioVolumeDisplay;
            EditorApplication.update += UpdateAudioVolumeDisplay;
        }

        private static void UpdateAudioVolumeDisplay()
        {
            if (_slider == null || _currentValueLabel == null)
            {
                return;
            }

            if (Mathf.Abs(AudioListener.volume - _lastAudioVolume) > 0.001f)
            {
                _lastAudioVolume = AudioListener.volume;
                _slider.SetValueWithoutNotify(_lastAudioVolume);
                _currentValueLabel.text = MasterAudioVolumeValueText;
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

            _slider = new Slider(0f, 1f);
            _slider.style.width = 70;
            _slider.style.height = 18;
            sliderContainer.Add(_slider);

            _currentValueLabel = new Label(MasterAudioVolumeValueText);
            _currentValueLabel.style.width = 16;
            _currentValueLabel.style.height = 18;
            _currentValueLabel.style.marginLeft = 4;
            _currentValueLabel.style.fontSize = 11;
            _currentValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _currentValueLabel.style.color = new Color(0.8f, 0.8f, 0.8f);

            var resetButton = new EditorToolbarButton(
                (Texture2D) EditorGUIUtility.IconContent("d_Profiler.Audio").image,
                () => _slider.value = 1);
            resetButton.tooltip = "Reset audio volume";
            resetButton.style.width = 18;
            resetButton.style.height = 18;
            resetButton.style.paddingTop = 0;
            resetButton.style.paddingBottom = 0;
            resetButton.style.paddingLeft = 0;
            resetButton.style.paddingRight = 0;
            resetButton.style.minWidth = 18;

            _slider.RegisterValueChangedCallback(evt =>
            {
                AudioListener.volume = evt.newValue;
                _lastAudioVolume = evt.newValue;
                _currentValueLabel.text = MasterAudioVolumeValueText;
            });
            _slider.value = AudioListener.volume;

            container.Add(resetButton);
            container.Add(sliderContainer);
            container.Add(_currentValueLabel);

            return container;
        }
    }
}