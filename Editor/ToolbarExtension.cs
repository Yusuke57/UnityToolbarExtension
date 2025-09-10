using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor
{
    [InitializeOnLoad]
    public static class ToolbarExtension
    {
        private const string ToolbarZoneLeftAlignName = "ToolbarZoneLeftAlign";
        private const string ToolbarZoneRightAlignName = "ToolbarZoneRightAlign";
        private const string ToolbarExtensionLeftContainerName = "ToolbarExtensionLeftContainer";
        private const string ToolbarExtensionRightContainerName = "ToolbarExtensionRightContainer";
        private const string ToolbarExtensionLeftAlignName = "LeftAlign";
        private const string ToolbarExtensionRightAlignName = "RightAlign";

        static ToolbarExtension()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            var toolbar = GetToolbar();
            if (toolbar == null)
            {
                return;
            }

            var toolbarZoneLeftAlign = toolbar.Q(ToolbarZoneLeftAlignName);
            var toolbarZoneRightAlign = toolbar.Q(ToolbarZoneRightAlignName);
            if (toolbarZoneLeftAlign == null || toolbarZoneRightAlign == null)
            {
                return;
            }

            // Retinaディスプレイから外部ディスプレイにウィンドウを移動した際などにリセットされてしまうため、
            // 描画済みかどうかを毎フレーム確認し、描画されていなかったら描画するようにしておく
            var leftContainer = toolbarZoneLeftAlign.Q(ToolbarExtensionLeftContainerName);
            var rightContainer = toolbarZoneRightAlign.Q(ToolbarExtensionRightContainerName);
            if (leftContainer != null && rightContainer != null)
            {
                // 描画済みなので終了
                return;
            }

            if (leftContainer == null)
            {
                leftContainer = CreateContainerElement();
                leftContainer.name = ToolbarExtensionLeftContainerName;
                toolbarZoneLeftAlign.Insert(toolbarZoneLeftAlign.childCount, leftContainer);
            }

            if (rightContainer == null)
            {
                rightContainer = CreateContainerElement();
                rightContainer.name = ToolbarExtensionRightContainerName;
                toolbarZoneRightAlign.Insert(toolbarZoneRightAlign.childCount, rightContainer);
            }

            DrawElements(leftContainer.Q(ToolbarExtensionLeftAlignName), leftContainer.Q(ToolbarExtensionRightAlignName),
                rightContainer.Q(ToolbarExtensionLeftAlignName), rightContainer.Q(ToolbarExtensionRightAlignName));
        }

        private static VisualElement GetToolbar()
        {
            var toolbarType = Type.GetType("UnityEditor.Toolbar,UnityEditor");
            if (toolbarType == null)
            {
                return null;
            }

            var getField = toolbarType.GetField("get", BindingFlags.Static | BindingFlags.Public);
            var getValue = getField?.GetValue(null);
            if (getValue == null)
            {
                return null;
            }

            var windowBackendProperty = toolbarType.GetProperty("windowBackend", BindingFlags.Instance | BindingFlags.NonPublic);
            var windowBackendValue = windowBackendProperty?.GetValue(getValue);
            if (windowBackendValue == null)
            {
                return null;
            }

            var iWindowBackendType = Type.GetType("UnityEditor.IWindowBackend,UnityEditor");
            if (iWindowBackendType == null)
            {
                return null;
            }

            var visualTreeProperty = iWindowBackendType.GetProperty("visualTree", BindingFlags.Instance | BindingFlags.Public);
            var visualTreeValue = visualTreeProperty?.GetValue(windowBackendValue);

            return visualTreeValue as VisualElement;
        }

        private static VisualElement CreateContainerElement()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.FlexStart;
            root.style.flexGrow = 1;

            var leftAlign = new VisualElement();
            leftAlign.name = ToolbarExtensionLeftAlignName;
            leftAlign.style.flexDirection = FlexDirection.Row;
            leftAlign.style.alignItems = Align.Center;
            leftAlign.style.justifyContent = Justify.FlexStart;
            leftAlign.style.flexGrow = 1;
            root.Add(leftAlign);

            var flexSpacer = new VisualElement();
            flexSpacer.style.flexGrow = 1;
            root.Add(flexSpacer);

            var rightAlign = new VisualElement();
            rightAlign.name = ToolbarExtensionRightAlignName;
            rightAlign.style.flexDirection = FlexDirection.Row;
            rightAlign.style.alignItems = Align.Center;
            rightAlign.style.justifyContent = Justify.FlexEnd;
            rightAlign.style.flexGrow = 1;
            root.Add(rightAlign);

            return root;
        }

        private static void DrawElements(VisualElement leftSideLeftAlignRoot, VisualElement leftSideRightAlignRoot,
            VisualElement rightSideLeftAlignRoot, VisualElement rightSideRightAlignRoot)
        {
            // 既存の要素をクリア
            leftSideLeftAlignRoot.Clear();
            leftSideRightAlignRoot.Clear();
            rightSideLeftAlignRoot.Clear();
            rightSideRightAlignRoot.Clear();

            var settings = GetSettings();
            var toolbarElements = GetTypesImplementingInterface<IToolbarElement>();

            // 設定がある場合は設定に従って利用可能な型を更新
            settings?.UpdateElementSettings(toolbarElements.ToList());

            // LayoutType別に要素を配置
            var layoutTypes = (ToolbarElementLayoutType[]) Enum.GetValues(typeof(ToolbarElementLayoutType));

            foreach (var layoutType in layoutTypes)
            {
                var root = layoutType switch
                {
                    ToolbarElementLayoutType.LeftSideLeftAlign => leftSideLeftAlignRoot,
                    ToolbarElementLayoutType.LeftSideRightAlign => leftSideRightAlignRoot,
                    ToolbarElementLayoutType.RightSideLeftAlign => rightSideLeftAlignRoot,
                    ToolbarElementLayoutType.RightSideRightAlign => rightSideRightAlignRoot,
                    _ => throw new ArgumentOutOfRangeException()
                };

                // 設定からこのLayoutTypeの要素を順序付きで取得
                var orderedSettings = settings?.GetSettingsForLayoutType(layoutType) ?? new List<ToolbarElementSetting>();

                foreach (var elementSetting in orderedSettings)
                {
                    // 設定で無効化されている場合はスキップ
                    if (!elementSetting.IsEnabled)
                    {
                        continue;
                    }

                    var elementType = toolbarElements.FirstOrDefault(t => t.FullName == elementSetting.TypeName);
                    if (elementType == null)
                    {
                        continue;
                    }

                    if (Activator.CreateInstance(elementType) is IToolbarElement toolbarElement)
                    {
                        var element = toolbarElement.CreateElement();
                        if (element != null)
                        {
                            root.Add(element);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 特定のインターフェースを実装したすべての型を取得
        /// </summary>
        private static List<Type> GetTypesImplementingInterface<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(GetAssemblyTypes)
                .Where(t => t != null && interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();
        }

        private static IEnumerable<Type> GetAssemblyTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// ToolbarExtensionSettingsを取得
        /// </summary>
        private static ToolbarExtensionSettings GetSettings()
        {
            return ToolbarExtensionSettings.Instance;
        }

        /// <summary>
        /// ツールバーを強制的に再描画
        /// </summary>
        public static void ForceRefresh()
        {
            var toolbar = GetToolbar();
            if (toolbar == null)
            {
                return;
            }

            var leftContainer = toolbar.Q(ToolbarExtensionLeftContainerName);
            var rightContainer = toolbar.Q(ToolbarExtensionRightContainerName);

            if (leftContainer != null && rightContainer != null)
            {
                DrawElements(leftContainer.Q(ToolbarExtensionLeftAlignName), leftContainer.Q(ToolbarExtensionRightAlignName),
                    rightContainer.Q(ToolbarExtensionLeftAlignName), rightContainer.Q(ToolbarExtensionRightAlignName));
            }
        }
    }
}