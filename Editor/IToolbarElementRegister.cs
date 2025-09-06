using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor
{
    public interface IToolbarElementRegister
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