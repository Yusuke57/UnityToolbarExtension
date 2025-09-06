using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor
{
    public interface IToolbarElement
    {
        ToolbarElementLayoutType DefaultLayoutType { get; }
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