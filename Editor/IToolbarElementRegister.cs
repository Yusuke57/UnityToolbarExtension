using UnityEngine.UIElements;

namespace Yusuke57.UnityToolbarExtension.Editor
{
    public interface IToolbarElementRegister
    {
        ToolbarElementLayoutType LayoutType { get; }
        VisualElement CreateElement();
    }

    public enum ToolbarElementLayoutType
    {
        LeftSideLeftAlign,
        LeftSideRightAlign,
        RightSideLeftAlign,
        RightSideRightAlign
    }
}