using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor
{
    [InitializeOnLoad]
    public static class ToolbarExtension
    {
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
            var (toolbarZoneLeftAlign, toolbarZoneRightAlign) = GetToolbarZones();
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

        /// <summary>
        /// ツールバーの左右ゾーンを取得する。
        /// Unity 6000.3+: MainToolbarWindow の overlay-toolbar__top 内の ContainerSection を使用。
        /// Unity 6000.1: Toolbar (HostView) の m_Root 内の ToolbarZone を使用。
        /// </summary>
        private static (VisualElement left, VisualElement right) GetToolbarZones()
        {
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;

#if UNITY_6000_3_OR_NEWER
            // Unity 6000.3+: MainToolbarWindow (EditorWindow) の rootVisualElement から取得
            var mainToolbarWindowType = editorAssembly.GetType("UnityEditor.MainToolbarWindow");
            if (mainToolbarWindowType != null)
            {
                var instances = Resources.FindObjectsOfTypeAll(mainToolbarWindowType);
                if (instances.Length > 0 && instances[0] is EditorWindow toolbarWindow)
                {
                    var overlayContainer = toolbarWindow.rootVisualElement.Q("overlay-toolbar__top");
                    if (overlayContainer != null)
                    {
                        // ContainerSection の順序: [0]=左, [1]=中央(PlayMode), [2]=右
                        var sections = overlayContainer.Children().ToList();
                        if (sections.Count >= 3)
                        {
                            return (sections[0], sections[2]);
                        }
                    }
                }
            }
#else
            // Unity 6000.1: Toolbar (HostView) の m_Root から取得
            var toolbarType = editorAssembly.GetType("UnityEditor.Toolbar");
            if (toolbarType != null)
            {
                var instances = Resources.FindObjectsOfTypeAll(toolbarType);
                if (instances.Length > 0)
                {
                    var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (rootField?.GetValue(instances[0]) is VisualElement root)
                    {
                        var left = root.Q("ToolbarZoneLeftAlign");
                        var right = root.Q("ToolbarZoneRightAlign");
                        return (left, right);
                    }
                }
            }
#endif
            return (null, null);
        }

        private static VisualElement CreateContainerElement()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.FlexStart;
#if UNITY_6000_3_OR_NEWER
            root.style.alignSelf = Align.Center;
            root.style.flexShrink = 0;
#else
            root.style.flexGrow = 1;
#endif

            var leftAlign = new VisualElement();
            leftAlign.name = ToolbarExtensionLeftAlignName;
            leftAlign.style.flexDirection = FlexDirection.Row;
            leftAlign.style.alignItems = Align.Center;
            leftAlign.style.justifyContent = Justify.FlexStart;
#if !UNITY_6000_3_OR_NEWER
            leftAlign.style.flexGrow = 1;
#endif
            root.Add(leftAlign);

            var flexSpacer = new VisualElement();
#if !UNITY_6000_3_OR_NEWER
            flexSpacer.style.flexGrow = 1;
#endif
            root.Add(flexSpacer);

            var rightAlign = new VisualElement();
            rightAlign.name = ToolbarExtensionRightAlignName;
            rightAlign.style.flexDirection = FlexDirection.Row;
            rightAlign.style.alignItems = Align.Center;
            rightAlign.style.justifyContent = Justify.FlexEnd;
#if !UNITY_6000_3_OR_NEWER
            rightAlign.style.flexGrow = 1;
#endif
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
                            ApplyToolbarOverlayStyle(element);
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
            var (leftZone, rightZone) = GetToolbarZones();
            if (leftZone == null || rightZone == null) return;

            var leftContainer = leftZone.Q(ToolbarExtensionLeftContainerName);
            var rightContainer = rightZone.Q(ToolbarExtensionRightContainerName);

            if (leftContainer != null && rightContainer != null)
            {
                DrawElements(leftContainer.Q(ToolbarExtensionLeftAlignName), leftContainer.Q(ToolbarExtensionRightAlignName),
                    rightContainer.Q(ToolbarExtensionLeftAlignName), rightContainer.Q(ToolbarExtensionRightAlignName));
            }
        }

#if UNITY_6000_3_OR_NEWER
        /// <summary>
        /// ツールバー要素にOverlayToolbar相当のスタイルを適用する
        /// </summary>
        private static void ApplyToolbarOverlayStyle(VisualElement element)
        {
            if (element is EditorToolbarButton or EditorToolbarDropdown)
            {
                element.style.flexDirection = FlexDirection.Row;
                element.style.alignItems = Align.Center;
            }

            element.Query(className: "unity-editor-toolbar-element__icon").ForEach(icon =>
            {
                icon.style.width = 16;
                icon.style.height = 16;
            });
        }
#else
        private static void ApplyToolbarOverlayStyle(VisualElement element) { }
#endif
    }
}
