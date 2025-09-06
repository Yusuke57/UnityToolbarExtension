using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor.Example
{
    public class ToolbarExtensionExampleA : IToolbarElementRegister
    {
        public ToolbarElementLayoutType LayoutType => ToolbarElementLayoutType.LeftSideLeftAlign;

        public VisualElement CreateElement()
        {
            var toggle = new EditorToolbarToggle();
            toggle.text = "A";
            toggle.style.marginLeft = 5;
            toggle.style.marginRight = 5;
            
            return toggle;
        }
    }
}